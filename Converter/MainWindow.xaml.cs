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
        public MainWindow()
        {
            InitializeComponent();

            _serviceMongo = new SefariaMongoDBService();
            
        }

        private void Convert_OnClick(object sender, RoutedEventArgs e)
        {
            
            var count = _serviceMongo.TextsCount();
            Log("text count: " + count);
            for (int i = 0; i < count; i++)
            {
                var text = _serviceMongo.GetTextAt(i);
                Log("text at: " + i + " count: " + text.Elements.ToList().Count);
                for (int j = 0; j < text.Elements.ToList().Count; j++)
                {
                    Log(" element: name: " + text.GetElement(j).Name + " value: " + text.GetElement(j).Value);
                }
                
                
            }
            
        }

        private void Log(string msg)
        {
            Trace.WriteLine(msg);
            logTxt.Text += msg + "<LineBreak/>";
        }
    }
}
