using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.Kinect;
using Microsoft.Samples.Kinect.BodyBasics.source;
using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    /// <summary>
    /// Logica di interazione per CutDepth.xaml
    /// </summary>
    public partial class Windowdepth : Window
    {
        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        ClassifiedObject[] cClassifierObj;

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        //draw update timer
        protected DispatcherTimer drawUpdateTimer;

        //image image Filtered by depth
        WriteableBitmap depthImageFiltered = null;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

  
        /// <summary>
        /// ColorFrame classification update time
        /// </summary>
        private const double TimeToClassificationColorFrame = 0.500;

        /// <summary>
        /// ColorFrame accumulatore time
        /// </summary>
        private double colorFrameAccTime = 0.0;

        //cnn thread -> thread che esegue la classificazione
        CNNThread cnnThread;

        //Create a background removal tool.
        BackgroundRemovalTool _backgroundRemovalTool;

        //Classified Objects
        ClassifiedObject[] currentObjectClassified = null;

        public Windowdepth(ClassifiedObject[] cClassifierCurrent)
        {
            //save
            currentObjectClassified = cClassifierCurrent;

            //Cnn tread
            cnnThread = new CNNThread(cClassifierObj);

            //start
            cnnThread.Start();

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // use the window object as the view model in this simple example
            this.DataContext = this;


            //update screen
            drawUpdateTimer = new DispatcherTimer();
            drawUpdateTimer.Tick += this.DrawUpdate;
            drawUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, 33);
            drawUpdateTimer.Start();

            // initialize the components (controls) of the window
            this.InitializeComponent();
        }

        void DrawUpdate(object sender, System.EventArgs e)
        {
            //draw object
            using (DrawingContext dc = this.drawingGroup.Open())
            {

                if (this.depthImageFiltered != null)
                {
                    // Draw background to set the render size
                    dc.DrawImage(depthImageFiltered, new Rect(0, 0, depthImageFiltered.Width, depthImageFiltered.Height));
                }

                for (int i = 0; i < currentObjectClassified.Length; ++i)
                {
                    // Blocco la thread secondoaria fin quando la prima thread non ha fatto il disegno
                    lock (cnnThread.mutex)
                    {
                        if (currentObjectClassified[i].feature.Equals("draw"))
                        {
                            int scalefactorX = 4;
                            double scalefactorY = 2.75;
                            int scalefactorRect = 2;
                            Rect rect = new Rect(currentObjectClassified[i].rectangle.X / scalefactorX,
                                                 currentObjectClassified[i].rectangle.Y / scalefactorY,
                                                 currentObjectClassified[i].rectangle.Width/ scalefactorRect,
                                                 currentObjectClassified[i].rectangle.Height/ scalefactorRect);
                            dc.DrawRectangle(null, new Pen(Brushes.Red, 6), rect);
                            //name?
                            if (currentObjectClassified[i].name.Length > 0)
                            {
                                FormattedText formattedTest = new FormattedText
                                (
                                    currentObjectClassified[i].name,
                                    CultureInfo.GetCultureInfo("en-us"),
                                    System.Windows.FlowDirection.LeftToRight,
                                    new Typeface("Arial Bold"),
                                    10,
                                    Brushes.White
                                );

                                //draw background of name Object.
                                dc.DrawRoundedRectangle(Brushes.Red, null,
                                new Rect(currentObjectClassified[i].rectangle.X / scalefactorX,
                                currentObjectClassified[i].rectangle.Y / scalefactorY,
                                50, 
                                15), 10.0, 10.0);

                                //draw name of Object.
                                dc.DrawText(formattedTest,
                                            new Point(currentObjectClassified[i].rectangle.X / scalefactorX,
                                                      currentObjectClassified[i].rectangle.Y / scalefactorY));

                            }
                        }
                        //sblocco la thread dopo aver disengnato
                    }
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();

                //Initialize the background removal tool.
                _backgroundRemovalTool = new BackgroundRemovalTool(_sensor.CoordinateMapper);

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            }
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            using (var colorFrame = reference.ColorFrameReference.AcquireFrame())
            using (var depthFrame = reference.DepthFrameReference.AcquireFrame())
            using (var bodyIndexFrame = reference.BodyIndexFrameReference.AcquireFrame())
            {
                if (colorFrame != null && depthFrame != null && bodyIndexFrame != null)
                {
                    //Update the image source.
                    depthImageFiltered = _backgroundRemovalTool.GreenScreen(colorFrame, depthFrame, bodyIndexFrame);
                    //about 0.33 secs
                    if (TimeToClassificationColorFrame <= colorFrameAccTime)
                    {
                        colorFrameAccTime = 0.0;
                        //K image to Emgu image
                        int scale = 2;
                        int contrast = (int)0.0;
                        int adjust = (int)0.0;
                        var frame = Kimage2CVimg(BitmapFrame.Create(depthImageFiltered));
                        //push imag chiamo il metodo AddAimage 
                        cnnThread.AddAImage(frame, scale, contrast, adjust);
                    }
                }
            }

        }

        //funzione che trasforma il frame nel formato di Emgu (senza il canale a)
        public Emgu.CV.Image<Bgra, Byte> Kimage2CVimg(BitmapFrame frame)
        {
            MemoryStream outStream = new MemoryStream();
            BitmapEncoder enc = new BmpBitmapEncoder();
            enc.Frames.Add(frame);
            enc.Save(outStream);
            var bmp = new System.Drawing.Bitmap(outStream);

            return new Image<Bgra, Byte>(bmp);
        }
        
        //close window
        private void Window_Closed(object sender, EventArgs e)
        {

            if (this._reader != null)
            {
                this._reader.Dispose();

            }

            if (this._sensor != null)
            {
                this._sensor.Close();
            }

            this.Close();

            MainWindow mw = new MainWindow();

        }


        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this._reader != null)
            {
               
                this._reader.Dispose();
                this._reader = null;
            }

            if (this._sensor != null)
            {
                this._sensor.Close();
                this._sensor = null;
            }
        }

    }
}
