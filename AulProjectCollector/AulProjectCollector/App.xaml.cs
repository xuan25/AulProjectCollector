using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AulProjectCollector
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private StreamWriter LogWriter { get; set; }

        public App()
        {
            this.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(Application_DispatcherUnhandledException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            LogWriter = new StreamWriter("log.txt", false, Encoding.UTF8);
            Console.SetOut(LogWriter);
            Console.WriteLine("[{0}] [Info] Startup at {1}", GetType().Name, DateTime.Now);
            this.Exit += App_Exit;
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            Console.WriteLine("[{0}] [Info] Closed", GetType().Name);
            LogWriter.Close();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            if(e.Args.Length == 0)
            {
                Current.MainWindow = new MainWindow();
                Current.MainWindow.Show();
            }
            else
            {
                Current.MainWindow = new MainWindow(e.Args);
                Current.MainWindow.Show();
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            MessageBox.Show("An unexpected and unrecoverable problem has occourred. Software will now exit.", "Unexpected operation", MessageBoxButton.OK, MessageBoxImage.Error);
            CrashLog("UnhandledException : \r\n\r\n" + string.Format("Captured an unhandled exception：{0}\r\nException Message：{1}\r\nException StackTrace：\r\n{2}", ex.GetType(), ex.Message, ex.StackTrace));
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            MessageBox.Show("An unexpected problem has occourred. Some operation has been terminated.", "Unexpected operation", MessageBoxButton.OK, MessageBoxImage.Information);
            CrashLog("DispatcherUnhandledException : \r\n\r\n" + string.Format("Captured an unhandled exception：{0}\r\nException Message：{1}\r\nException StackTrace：\r\n{2}", ex.GetType(), ex.Message, ex.StackTrace));
            e.Handled = true;
        }

        private void CrashLog(string message)
        {
            string directory = Path.Combine(Environment.CurrentDirectory, "crashlog");
            Directory.CreateDirectory(directory);
            string time = DateTime.Now.ToString();

            int i = 0;
            while (i < 100)
            {
                string filename = MakeValidFileName(time);
                if (i != 0)
                {
                    filename = filename + " (" + i + ")";
                }
                string logPath = Path.Combine(directory, string.Format("{0}.log", filename));
                if (!File.Exists(logPath))
                {
                    try
                    {
                        using (StreamWriter streamWriter = new StreamWriter(logPath, false))
                        {
                            streamWriter.WriteLine(message);
                        }
                        break;
                    }
                    catch { }
                }
                i++;
            }
        }

        public static string MakeValidFileName(string text, string replacement = "_")
        {
            StringBuilder str = new StringBuilder();
            var invalidFileNameChars = Path.GetInvalidFileNameChars();
            foreach (var c in text)
            {
                if (invalidFileNameChars.Contains(c))
                {
                    str.Append(replacement ?? "");
                }
                else
                {
                    str.Append(c);
                }
            }

            return str.ToString();
        }
    }
}
