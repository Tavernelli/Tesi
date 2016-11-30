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
            startInfo.FileName = Directory.GetCurrentDirectory() + "\\" + appName;
        
            startInfo.WorkingDirectory = workingDir;
            startInfo.Arguments = args;
            process.StartInfo = startInfo;
            process.Start();
           // process.WaitForExit(5000);
        }


        //metods that block string input only num!! 
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


        static private bool GetValueFromTextBox(System.Windows.Controls.TextBox tbox,String errorTitle,String errorDesc, ref int outvalue)
        {
            try
            {
                outvalue  = Int32.Parse(tbox.GetLineText(0));
                return true;
            }
            catch (Exception)
            {
                System.Windows.Forms.MessageBox.Show(
                    errorDesc,
                    errorTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

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
                string outputPath = System.IO.Path.GetDirectoryName(sSelectedFile);

                // directory name
                string dirName = "CASCADE@" + System.IO.Path.GetFileNameWithoutExtension( sSelectedFile );

                // Specify the directory you want to manipulate.
                string directorypath = outputPath + "\\" + dirName;

                // Determine whether the directory exists.
                if (Directory.Exists(directorypath) && Delete.IsChecked == true)
                {
                    // Delete old dir
                    Directory.Delete(directorypath, true);
                }
                // Get the directory info
                DirectoryInfo di = null;
                
                // Create the directory if necessary
                if (Directory.Exists(directorypath))
                    di = new DirectoryInfo(directorypath);
                else
                    di = Directory.CreateDirectory(directorypath);

                //image size
                int npos = 0;
                int nneg = 0;
                int nstages = 0;
                int w = 0;
                int h = 0;

                //try to read values
                if (!GetValueFromTextBox(NumPos, "Error input value", "Wrong number of positive", ref npos)) return;
                if (!GetValueFromTextBox(NumNeg, "Error input value", "Wrong number of negative", ref nneg)) return;
                if (!GetValueFromTextBox(NumStage, "Error input value", "Wrong number of stages", ref nstages)) return;
                if (!GetValueFromTextBox(textBoxWidth, "Error input value", "Wrong width value", ref w)) return;
                if (!GetValueFromTextBox(textBoxHeight, "Error input value", "Wrong height value", ref h)) return;


                OpenApplicationWithArguments(
                    outputPath,
                    "\\Tools\\opencv_traincascade.exe",
                        " -data " + dirName
                    + " -vec " + sSelectedFile
                    + " -bg bg.txt -numPos " + NumPos.GetLineText(0)
                    + " -numNeg " + NumNeg.GetLineText(0)
                    + " -numStages " + NumStage.GetLineText(0)
                    + " -w " + w
                    + " -h " + h
                    + " -mode ALL -numThreads 4"

                );

            }
            else
                sSelectedFile = string.Empty;
        }

    }
}
