using Converter.Model.SQLite;
using Converter.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            _serviceMongo = new SefariaMongoDBService();
            _serviceSQLite = new SefariaSQLiteConversionContext(new Microsoft.EntityFrameworkCore.DbContextOptions<SefariaSQLiteConversionContext> { });
            Converter.Model.SQLite.Version defaultVersion = new Converter.Model.SQLite.Version() { Major = 0, Minor = 0, Build = 0 };
            Converter.Model.SQLite.Version loadedVersion = null;
            if (_serviceSQLite.Database.CanConnect())
            {
                loadedVersion = _serviceSQLite.Version.FirstOrDefault();
            }

            if (loadedVersion != null) {
                _serviceSQLite.Database.EnsureDeleted();
                _serviceSQLite.Dispose();
            }
            _serviceSQLite = new SefariaSQLiteConversionContext(new Microsoft.EntityFrameworkCore.DbContextOptions<SefariaSQLiteConversionContext> { });
            _serviceSQLite.Database.EnsureCreated();
            defaultVersion.Build = (loadedVersion!=null?loadedVersion.Build:0)+1;

            _serviceSQLite.Version.Add(defaultVersion);
            _serviceSQLite.Languages.Add(new Language { Value = LanguageTypes.Undefined.ToString() });
            _serviceSQLite.Languages.Add(new Language { Value = LanguageTypes.English.ToString() });
            _serviceSQLite.Languages.Add(new Language { Value = LanguageTypes.Hebrew.ToString() });
            _serviceSQLite.SaveChanges();
        }

        private void Convert_OnClick(object sender, RoutedEventArgs e)
        {

            var totalTexts = _serviceMongo.TextsCount();
            for (int i = 0; i < totalTexts; i++)
            {
                _serviceSQLite.AddAsync(_serviceMongo.GetTextAt(i));
            }
            _serviceSQLite.SaveChanges();
        }

        private void Log(string msg)
        {
            Trace.WriteLine(msg);
            logTxt.Text += msg + "<LineBreak/>";
        }
    }
}
