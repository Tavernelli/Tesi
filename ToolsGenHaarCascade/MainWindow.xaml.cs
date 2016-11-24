using System;
using System.Collections.Generic;
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
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using Microsoft.Kinect;
using System.Windows.Threading;


namespace ToolsGenHaarCascade
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string sSelectedFile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\GitHub\\Tesi\\OGGETTI";

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Open console application with arguments
        /// </summary>
        void OpenApplicationWithArguments(String workingDir, String appName, String args)
        {
            
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
           // startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = System.IO.Path.GetDirectoryName(sSelectedFile) + "\\" + appName;
        
            startInfo.WorkingDirectory = workingDir;
            startInfo.Arguments = args;
            process.StartInfo = startInfo;
            process.Start();
           // process.WaitForExit(5000);
        }

        private void textBoxInteger_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !TextBoxTextAllowed(e.Text);
        }

        private void textBoxInteger_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                String Text1 = (String)e.DataObject.GetData(typeof(String));
                if (!TextBoxTextAllowed(Text1)) e.CancelCommand();
            }
            else e.CancelCommand();
        }

        private Boolean TextBoxTextAllowed(String Text2)
        {
            return Array.TrueForAll<Char>(Text2.ToCharArray(),
                delegate (Char c) { return Char.IsDigit(c) || Char.IsControl(c); });
        }


        private void TakeFile_Click(object sender, EventArgs e)
        {

            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "All Files (*.vec)|*.vec";
            dialog.FilterIndex = 1;
            dialog.Multiselect = true;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            { 
                sSelectedFile = dialog.FileName;
                //write output
                string path = System.IO.Path.GetDirectoryName(sSelectedFile);

                // Specify the directory you want to manipulate.
                string Directorypath = System.IO.Path.GetDirectoryName(sSelectedFile) + "\\" + "DATA1";
                
                // Determine whether the directory exists.
                if (Directory.Exists(Directorypath))
                {
                    Directory.Delete(Directorypath, true);
                }

                int w = Int32.Parse(textBoxWidth.GetLineText(0));
                int h = Int32.Parse(textBoxHeight.GetLineText(0));
                // Try to create the directory.
                DirectoryInfo di = Directory.CreateDirectory(Directorypath);
                OpenApplicationWithArguments(
                    System.IO.Path.GetDirectoryName(sSelectedFile),
                    "opencv_traincascade.exe",
                    " -data DATA1"
                    + " -vec " + sSelectedFile
                    + " -bg bg.txt -numPos " + NumPos.GetLineText(0) + " -numNeg " + NumNeg.GetLineText(0) + " -numStages "+ NumStage.GetLineText(0) + " -w " + w + " -h " + h + " -mode ALL -numThreads 4"

                );

            }
            else
                sSelectedFile = string.Empty;
        }

    }
}
