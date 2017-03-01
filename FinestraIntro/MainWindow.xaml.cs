using System.Windows;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Windows.Media.Imaging;


namespace FinestraIntro

{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //process relative to exe file
        Process myProcess;
        public MainWindow()
        {
            InitializeComponent();

            // check if sdk is already install!
            if (checkSDK())
            {
               
                ciao.Source = new BitmapImage(new Uri("ImageIcon/downloadGray.png", UriKind.RelativeOrAbsolute));
                OpenExeInstall.ToolTip = "Sdk Already Installed!";
                OpenExeInstall.IsEnabled = false;

            }

        }

        public bool checkSDK()
        {

            string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            using (Microsoft.Win32.RegistryKey key = Registry.LocalMachine.OpenSubKey(registry_key))
            {
                foreach (string subkey_name in key.GetSubKeyNames())
                {
                    using (Microsoft.Win32.RegistryKey subkey = key.OpenSubKey(subkey_name))
                    {
                        if (!ReferenceEquals(subkey.GetValue("DisplayName"), null))
                        {

                            string SoftNames = Convert.ToString(subkey.GetValue("DisplayName"));

                            if (SoftNames.Contains("Kinect for Windows SDK v2.0"))
                            {

                                return true;
                                
                            }
                          


                        }
                    }
                }

            }
            return false;
        }




        //OpenCreateSamples
        private void button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Samples.Kinect.ColorBasics.MainWindow nuovo = new Microsoft.Samples.Kinect.ColorBasics.MainWindow();
            
            nuovo.ShowDialog();
            nuovo.Close();


        }

        //open ObjectRec
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Samples.Kinect.BodyBasics.MainWindow a = new Microsoft.Samples.Kinect.BodyBasics.MainWindow();
            a.ShowDialog();
        }
        //quit
        private void button2_Click(object sender, RoutedEventArgs e)
        {

            this.Close();
            System.Environment.Exit(1);
        }

        //GenCascade
        private void button3_Click(object sender, RoutedEventArgs e)
        {
            ToolsGenHaarCascade.MainWindow b = new ToolsGenHaarCascade.MainWindow();
            b.ShowDialog();
        }


        private void OpenExeInstall_Click(object sender, RoutedEventArgs e)
        {


            myProcess = Process.Start("ImageIcon\\KinectSDK-v2.0_1409-Setup.exe");

            while (!myProcess.HasExited)
            {
                if (checkSDK())
                {

                    ciao.Source = new BitmapImage(new Uri("ImageIcon/downloadGray.png", UriKind.RelativeOrAbsolute));
                    OpenExeInstall.ToolTip = "Sdk Already Installed!";
                    OpenExeInstall.IsEnabled = false;
                    
                }

            }
        }


            
            
       

        



    }
}
