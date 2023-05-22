using Hansero;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
using Utill;

namespace VisionStartChecker
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private Hansero.LogManager logManager;
        private IniFile iniConfig = new IniFile(AppDomain.CurrentDomain.BaseDirectory + "\\Config.ini");


        public MainWindow()
        {
            InitializeComponent();

            logManager = new Hansero.LogManager(true, true);

            int checkInterval = iniConfig.GetInt32("Info", "ProcessCheckInterval", 5000);

            new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    try
                    {
                        string current = System.Diagnostics.Process.GetCurrentProcess().ProcessName.ToUpper();

                        //이미 실행 중인 프로그램이 있을경우
                        System.Diagnostics.Process[] processes = null;
                        string strCurrentProcess = "Espotec";
                        processes = System.Diagnostics.Process.GetProcessesByName(strCurrentProcess);
                        if (processes.Length == 0)
                        {
                            // logManager.Fatal("비전 프로그램 종료 확인");
                            Process.Start(AppDomain.CurrentDomain.BaseDirectory + "\\" + "Espotec.exe");
                            // logManager.Fatal("비전 프로그램 시작");
                        }
                    }
                    catch
                    {

                    }
                    Thread.Sleep(checkInterval);
                }
            })).Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
             
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
