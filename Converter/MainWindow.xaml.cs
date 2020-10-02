using Converter.Model.SQLite;
using Converter.Service;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Converter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SefariaMongoDBService _serviceMongo;
        private SefariaSQLiteConversionContext _serviceSQLite;
        public MainWindow()
        {
            InitializeComponent();
            
            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(33);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private float TrackingTotalAmount = 0f;
        private long TrackingStartTime = 0;
        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Total != 0)
                {
                    if (TrackingTotalAmount != Total)
                    {
                        TrackingTotalAmount = Total;
                        TrackingStartTime = DateTime.Now.Ticks;
                    }

                    progress.Value = (Complete / Total) * 100f;
                    convert.IsEnabled = !isActive;
                    float percent = MathF.Floor(((Complete + 1) / Total) * 1000) / 10;

                    //float countTime = MathF.Floor((DateTime.Now.Ticks - TrackingStartTime) / 10000000);
                    long countTime = DateTime.Now.Ticks - TrackingStartTime;
                    //long remainTime = Complete!=0?((long)(countTime / (Complete / Total))) - countTime:0;

                    textBlock.Text = $"Processed {Complete + 1}/{Total} ({percent.ToString("0.0")}%), Time Elapsed: {new DateTime(countTime).ToString("H:mm:ss.f")}";

                    if (!string.IsNullOrEmpty(bufferText))
                    {
                        Trace.WriteLine(bufferText);
                        logTxt.Text += bufferText;
                        logTxt.ScrollToEnd();
                        bufferText = "";
                    }
                }
            }));
            
        }

        private static System.Timers.Timer aTimer;

        private Task ConversionTask;
        private readonly object TaskLock = new object();

        private float Complete = 0f;
        private float Total = 0f;
        private bool isActive = false;

        private void ConversionLogic() {

            Log("Inializing");
            _serviceMongo = _serviceMongo ?? new SefariaMongoDBService();
            _serviceSQLite = new SefariaSQLiteConversionContext(new Microsoft.EntityFrameworkCore.DbContextOptions<SefariaSQLiteConversionContext> { });
            Log("Loaded Databases");
            Converter.Model.SQLite.Version defaultVersion = new Converter.Model.SQLite.Version() { Major = 0, Minor = 0, Build = 0 };
            Converter.Model.SQLite.Version loadedVersion = null;
            if (_serviceSQLite.Database.CanConnect())
            {
                loadedVersion = _serviceSQLite.Version.FirstOrDefault();
            }
            Log("Parsing Version Information");

            if (loadedVersion != null)
            {
                _serviceSQLite.Database.EnsureDeleted();
                _serviceSQLite.Dispose();
            }

            Log("Starting to Rebuild Database");

            _serviceSQLite = new SefariaSQLiteConversionContext(new Microsoft.EntityFrameworkCore.DbContextOptions<SefariaSQLiteConversionContext> { });
            _serviceSQLite.Database.EnsureCreated();
            defaultVersion.Build = (loadedVersion != null ? loadedVersion.Build : 0) + 1;

            Log("DB Version: "+ defaultVersion);
            _serviceSQLite.Version.Add(defaultVersion);
            _serviceSQLite.Languages.Add(new Language { Value = LanguageTypes.Undefined.ToString() });
            _serviceSQLite.Languages.Add(new Language { Value = LanguageTypes.English.ToString() });
            _serviceSQLite.Languages.Add(new Language { Value = LanguageTypes.Hebrew.ToString() });
            _serviceSQLite.SaveChanges();

            //Processing Summaries
            var topic = _serviceMongo.GetSummaryTopics();
            _serviceSQLite.AddAsync(topic);
            _serviceSQLite.SaveChanges();

            //Processing Texts
            var totalTexts = Total = _serviceMongo.TextsCount();
            Log($"Processing: Texts #{totalTexts}");
            int textBatchAmount = 390;
            List<BsonDocument> textParseList = new List<BsonDocument>();
            for (int i = 0; i < totalTexts; i++)
            {
                if (textParseList.Count == 0)
                {
                    textParseList = _serviceMongo.GetTexts(i, textBatchAmount);
                }

                var raw = textParseList[0];
                textParseList.RemoveAt(0);

                var txt = _serviceMongo.ParseText(raw, _serviceSQLite);
                //var test = _serviceSQLite.Texts.Local;
                
                _serviceSQLite.Add(_serviceSQLite.Texts,txt);
                //_serviceSQLite.SaveChanges();
                foreach (var chapter in _serviceMongo.ListChapters(txt.Chapter))
                {
                    chapter.TopicText = txt;
                    _serviceSQLite.Add(_serviceSQLite.Chapters, chapter);
                }

                if (i % textBatchAmount == 0)
                {
                    //_serviceSQLite.SaveChanges();
                    //_serviceSQLite.ChangeTracker.DetectChanges();
                    _serviceSQLite.SaveChanges();
                    _serviceSQLite.DisposeAsync();
                    _serviceSQLite = new SefariaSQLiteConversionContext(new Microsoft.EntityFrameworkCore.DbContextOptions<SefariaSQLiteConversionContext> { });
                    //break;//For Testing Links

                }
                Complete = i;
                //Log($"Processing: index {i} / total {totalTexts}");
            }
            Complete++;

            _serviceSQLite.SaveChanges();
            _serviceSQLite.DisposeAsync();
            _serviceSQLite = new SefariaSQLiteConversionContext(new Microsoft.EntityFrameworkCore.DbContextOptions<SefariaSQLiteConversionContext> { });

            const int linkBatchAmount = 52000;
            var totalLinks = Total = _serviceMongo.LinksCount();
            List<BsonDocument> linksList = new List<BsonDocument>();
            Log($"Processing: Links #{totalLinks}");
            for (int i = 0; i < totalLinks; i++)
            {
                if (linksList.Count == 0)
                {
                    linksList = _serviceMongo.GetLinks(i, linkBatchAmount);
                }

                if (linksList.Count > 0)
                {
                    var processing = linksList[0];
                    linksList.RemoveAt(0);

                    //var processing = linksList[i];
                    if (processing != null)
                    {
                        var link = _serviceMongo.ParseLink(processing, _serviceSQLite);
                        if (link != null) _serviceSQLite.Add(link);
                    }
                    //bool hasNew = false;
                    //if (link.LinkGroup.Id == 0) {
                    //    hasNew = true;
                    //}
                    //foreach (var item in link.LinkGroup.LinkedLanguages)
                    //{
                    //    if (item.Id == 0) {
                    //        hasNew = true;
                    //    }
                    //}

                    if (i % linkBatchAmount == 0)
                    {
                        _serviceSQLite.SaveChanges();
                        _serviceSQLite.DisposeAsync();
                        _serviceSQLite = new SefariaSQLiteConversionContext(new Microsoft.EntityFrameworkCore.DbContextOptions<SefariaSQLiteConversionContext> { });
                    }
                }
                Complete = i;
            }
            _serviceSQLite.SaveChanges();
            _serviceSQLite.Dispose();
            _serviceSQLite = new SefariaSQLiteConversionContext(new Microsoft.EntityFrameworkCore.DbContextOptions<SefariaSQLiteConversionContext> { });

            isActive = false;
        }



        private void Convert_OnClick(object sender, RoutedEventArgs e)
        {
            if (!isActive && ConversionTask == null || ConversionTask.IsCompleted)
            {
                convert.IsEnabled = !(isActive = true);

                ConversionTask = new Task(ConversionLogic);
                ConversionTask.Start();
            }
        }

        string bufferText = "";
        private void Log(string msg)
        {
            //Dispatcher.BeginInvoke(new Action(() =>
            //{
                bufferText += msg + "\n";
            //}));
        }
    }
}
