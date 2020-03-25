using ExoUtil;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace AulProjectCollector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private delegate void ProcessFinishedHandler();
        private event ProcessFinishedHandler ProcessFinished;

        private ProcessUtil ProcessUnit { get; set; }
        private bool IsProcessing { get; set; }
        private Thread ProcessThread { get; set; }
        private Thread ProgressUpdateThread { get; set; }
        

        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
            this.Loaded += async delegate (object sender, RoutedEventArgs e)
            {
                await Task.Delay(500);
                AProgressBar.Value = 0;
                await Task.Delay(500);
                AProgressBar.Value = -1;
            };
            Console.WriteLine("[{0}] [Info] Initialized", GetType().Name);

            new Task(() =>
            {
                ProcessUnit = new ProcessUtil();

                Dispatcher.Invoke(() =>
                {
                    Storyboard loadedStoryboard = ((Storyboard)Resources["LoadedStoryboard"]);
                    loadedStoryboard.Completed += LoadedStoryboard_Completed;
                    loadedStoryboard.Begin();
                });
            }).Start();
            
        }

        public MainWindow(string[] files)
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
            this.ProcessFinished += MainWindow_ProcessFinished;
            Console.WriteLine("[{0}] [Info] Initialized", GetType().Name);

            new Task(() =>
            {
                ProcessUnit = new ProcessUtil();
                
                Dispatcher.Invoke(() =>
                {
                    Storyboard loadedStoryboard = ((Storyboard)Resources["LoadedStoryboard"]);
                    loadedStoryboard.Completed += LoadedStoryboard_Completed;
                    loadedStoryboard.Begin();
                    StartProcess(files);
                });
            }).Start();
        }

        private void LoadedStoryboard_Completed(object sender, EventArgs e)
        {
            LoadingMask.Visibility = Visibility.Collapsed;
        }

        private void MainWindow_ProcessFinished()
        {
            this.Close();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ProcessThread != null)
                ProcessThread.Abort();
            if (ProgressUpdateThread != null)
                ProgressUpdateThread.Abort();
            Console.WriteLine("[{0}] [Info] Closing", GetType().Name);
        }

        private void DropGrid_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            StartProcess(files);
        }

        private void StartProcess(string[] files)
        {
            AProgressBar.Value = 0;
            DropGrid.AllowDrop = false;
            ((Storyboard)Resources["StartStoryboard"]).Begin();

            ProcessThread = new Thread(() => {
                IsProcessing = true;
                ProgressUpdateThread = new Thread(ProgressUpdateLoop);
                ProgressUpdateThread.Start();

                string message = ProcessUnit.Process(files);

                IsProcessing = false;

                Dispatcher.Invoke(() =>
                {
                    AProgressBar.Value = -1;
                    DropGrid.AllowDrop = true;
                    ((Storyboard)Resources["EndStoryboard"]).Begin();
                    MessageBox.Show(this, message);
                    ProcessFinished?.Invoke();
                });
            });
            ProcessThread.Start();
        }

        private void ProgressUpdateLoop()
        {
            while (IsProcessing)
            {
                Dispatcher.Invoke(() =>
                {
                    AProgressBar.Value = ProcessUnit.CurrentProgress;
                });
                Thread.Sleep(500);
            }
        }
    }


    public class ProgressTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double progress = (double)value;
            if (progress == -1)
                return "100.0%";
            return progress.ToString("P1");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
