using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;
using System.Drawing;

namespace Microsoft.Samples.Kinect.ColorBasics
{
  
    /// <summary>
    /// Field of a Item in list Items
    /// </summary>
    public class FieldList
    {
        public string PathImage { get; set; }
        public string PathToShow { get; set; }
    }
    /// <summary>
    /// Logica di interazione per DialogCreateSamples.xaml
    /// </summary>
    public partial class DialogCreateSamples : Window
    {
        //Variables
        private String          savePathImages;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string SamplesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        //Attribute
        private List<FieldList> DataFieldList { get; set; }

        protected void FillListBox()
        {
            DataFieldList = new List<FieldList>();
            //Add all data to list
            foreach (string pngPathImages in Directory.GetFiles(savePathImages, "*.png"))
            {
                var field = new FieldList();
                field.PathImage = pngPathImages;
                field.PathToShow = System.IO.Path.GetFileName(pngPathImages);
                DataFieldList.Add(field);
            }
            //Add data to View
            ImageListBox.ItemsSource = DataFieldList;
            ImageListBox.DataContext = this;
        }

        public DialogCreateSamples(String path)
        {
            //save path
            savePathImages = path;
            //init ui
            InitializeComponent();
            //Enable multi select
            ImageListBox.SelectionMode = SelectionMode.Extended;
            //Fill box
            FillListBox();
        }

        /// <summary>
        /// Open console application with arguments
        /// </summary>
        void OpenApplicationWithArguments(String workingDir, String appName, String args)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = Directory.GetCurrentDirectory() + "\\" + appName;
            startInfo.WorkingDirectory = workingDir;
            startInfo.Arguments = args;
            process.StartInfo = startInfo;
            process.Start();
           // process.WaitForExit(2000);
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

        private void BottomCreateSamples_Click(object sender, RoutedEventArgs e)
        {
            //list of images
            List<String> namePathImages = new List<string>();
           
            //get paths
            foreach(object field in ImageListBox.SelectedItems)
            {
                var image = System.Drawing.Image.FromFile(((FieldList)field).PathImage);
                String nameFile =  ((FieldList)field).PathToShow + " 1 0 0 "+ image.Width + " "+ image.Height;
                //String OnlynameFile = ((FieldList)field).PathToShow;
                namePathImages.Add(nameFile);
            }
            //get values
            int w = Int32.Parse(textBoxWidth.GetLineText(0));
            int h = Int32.Parse(textBoxHeight.GetLineText(0));
            String Samples = SamplesPath + "\\test.vec";
            //write output
            File.WriteAllText(savePathImages + "\\temp_out_files.info", String.Join("\n", namePathImages.ToArray()));
            if (checkBox.IsChecked == false)
            {
                OpenApplicationWithArguments(
                savePathImages,
                "OpenCV_tools\\opencv_createsamples.exe",
                "-info temp_out_files.info "
                + "-vec " + Samples + " -w " + w + " -h "+ h 
                );
            }
            else
                OpenApplicationWithArguments(
                savePathImages,
                "OpenCV_tools\\opencv_createsamples.exe",
                "-info temp_out_files.info "
               + "-vec " + Samples + " -w " + w + " -h " + h + " "
                + "-show "
                );

        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = SamplesPath;
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                SamplesPath = dialog.SelectedPath;
                StampaSamplesPath.Text = SamplesPath;
            }
        }
        //nel caso dovessi creare il file sample direttamente da foto 
#if false
           
            foreach (object image in namePathImages)
            {

                //write output
                File.WriteAllText(savePathImages + "\\temp_out_files1.info", String.Join("\n", namePathImages.ToArray()));
                OpenApplicationWithArguments(
                    savePathImages,
                    "OpenCV_tools\\opencv_createsamples.exe",
                      "-info temp_out_files1.info "
                    // "-img " + image
                    + " -vec test.vec -w 20 -h 30 "
                    // + "-show "
                    // + "-num 10"  
                    );
            }
#endif

    }
}
