using System.Windows;


namespace FinestraIntro

{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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
        }

        //GenCascade
        private void button3_Click(object sender, RoutedEventArgs e)
        {
            ToolsGenHaarCascade.MainWindow b = new ToolsGenHaarCascade.MainWindow();
            b.ShowDialog();
        }

       
    }
}
