﻿using System.Windows;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Windows.Forms;

namespace FinestraIntro

{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //process relative to exe file
        private Process myProcess;
        bool MyProcessIsRunning;
        Process MyProcess
        {
            get { return myProcess; }
            set
            {
                if (myProcess != null)
                {
                    myProcess.Exited -= MyProcess_Exited;
                }
                myProcess = value;
                if (myProcess != null)
                {
                    myProcess.Exited += MyProcess_Exited;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            // check if sdk is already install!
            if (checkSDK())
            {

                ciao.Source = new BitmapImage(new Uri("ImageIcon/downloadGray.png", UriKind.RelativeOrAbsolute));
                OpenExeInstall.ToolTip = "SDK Already Installed!";
                OpenExeInstall.IsEnabled = false;

            }

          
        }


        //method that verify in the register if sdk is installed 
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
            if (!checkSDK())
            {
                System.Windows.Forms.MessageBox.Show("SDK NOT INSTALL YET!",
                "Warning",
                MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation,
                MessageBoxDefaultButton.Button1);
                return;
            }
            Microsoft.Samples.Kinect.ColorBasics.MainWindow nuovo = new Microsoft.Samples.Kinect.ColorBasics.MainWindow();

            nuovo.ShowDialog();
            nuovo.Close();


        }

        //open ObjectRec
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (!checkSDK())
            {
                System.Windows.Forms.MessageBox.Show("SDK NOT INSTALL YET!",
                "Warning",
                MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation,
                MessageBoxDefaultButton.Button1);
                return;
            }
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
            if (!checkSDK())
            {
                System.Windows.Forms.MessageBox.Show("SDK NOT INSTALL YET!",
                "Warning",
                MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation,
                MessageBoxDefaultButton.Button1);
                    return;
            }
            ToolsGenHaarCascade.MainWindow b = new ToolsGenHaarCascade.MainWindow();
            b.ShowDialog();
        }


        //install sdk
        private void OpenExeInstall_Click(object sender, RoutedEventArgs e)
        {

            MyProcess = Process.Start("ImageIcon\\KinectSDK-v2.0_1409-Setup.exe");

            // enable raising events for the process.
            MyProcess.EnableRaisingEvents = true;

            // set the flag to know whether my process is running
            MyProcessIsRunning = true;
        }


        private void MyProcess_Exited(object sender, EventArgs e)
        {
            // the process has just exited. what do you want to do?
            MyProcessIsRunning = false;
            

            if (checkSDK())
            {
                ciao.Dispatcher.Invoke(new Action(() => { ciao.Source = new BitmapImage(new Uri("ImageIcon/downloadGray.png", UriKind.RelativeOrAbsolute)); }));
                OpenExeInstall.Dispatcher.Invoke(new Action(() => { OpenExeInstall.ToolTip = "SDK Already Installed!"; }));
                OpenExeInstall.Dispatcher.Invoke(new Action(() => { OpenExeInstall.IsEnabled = false; }));

            }

 
        }
    }
}
