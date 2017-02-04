namespace Microsoft.Samples.Kinect.ColorBasics
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Windows.Threading;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Reader for color frames
        /// </summary>
        private ColorFrameReader colorFrameReader = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap colorBitmap = null;

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;


        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string savePathImages = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\GitHub\\Tesi\\OGGETTI";

        /// <summary>
        /// Draw update timer
        /// </summary>
        protected DispatcherTimer drawUpdateTimer;

        /// <summary>
        /// Class describe rect state
        /// </summary>
        protected class DragBox
        {
            public enum State : byte
            {
                START,
                STOP
            }

            public DragBox(Rect start)
            {
                box = start;
            }

            public void SetBoxFromMouse(Point point)
            {
                switch(state)
                {
                    case State.START:
                        double min_x = Math.Min(point.X, box.X);
                        double min_y = Math.Min(point.Y, box.Y);
                        double max_x = Math.Max(point.X, box.X + box.Width);
                        double max_y = Math.Max(point.Y, box.Y + box.Height);
                        box = new Rect(min_x, min_y, max_x - min_x, max_y - min_y);
                    break;
                    case State.STOP:  box = new Rect(point.X, point.Y, 1, 1); break;
                }
                state = State.START;
            }

            public bool IsNotComplete()
            {
                return state == State.START;
            }

            public void BoxIsComplete()
            {
                state = State.STOP;
            }

            public Rect Box
            {
                get { return box; }
            }

            public Int32Rect Int32Box
            {
                get { return new Int32Rect((int)box.X, (int)box.Y, (int)box.Width, (int)box.Height); }
            }

            public System.Drawing.Rectangle RectangleBox
            {
                get { return new System.Drawing.Rectangle((int)box.X, (int)box.Y, (int)box.Width, (int)box.Height); }
            }

            protected State state = State.STOP;
            protected Rect  box;

        };

        /// <summary>
        /// Rect area to take image
        /// </summary>
        DragBox boxToTakeImage = null;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // get the kinectSensor object
            this.kinectSensor = KinectSensor.GetDefault();

            // open the reader for the color frames
            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();

            // wire handler for frame arrival
            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;

            // create the colorFrameDescription from the ColorFrameSource using Bgra format
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            // create the bitmap to display
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            //Init drag box
            boxToTakeImage = new DragBox(new Rect(0, 0, colorFrameDescription.Width, colorFrameDescription.Height));

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            //update screen
            drawUpdateTimer = new DispatcherTimer();
            drawUpdateTimer.Tick += this.DrawUpdate;
            drawUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, 33);
            drawUpdateTimer.Start();

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();


        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

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
        /// Draw scene
        /// </summary>
        void DrawUpdate(object sender, System.EventArgs e)
        {
            //draw object
            using (DrawingContext dc = this.drawingGroup.Open())
            {
                if (this.colorBitmap != null)
                {
                    // Draw background (Kineckt video)
                    dc.DrawImage(colorBitmap, new Rect(0, 0, colorBitmap.Width, colorBitmap.Height));
                }

                if(boxToTakeImage != null)
                {
                    dc.DrawRectangle(null, new Pen(Brushes.Orange, 8), boxToTakeImage.Box);
                }
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        private void mainViewbox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.colorBitmap != null)
            {
                double value_x_ratio = 1.0 / (this.MainViewbox.ActualWidth / ImageSource.Width);
                double value_y_ratio = 1.0 / (this.MainViewbox.ActualHeight / ImageSource.Height);
                boxToTakeImage.SetBoxFromMouse(new Point(e.GetPosition(this.MainViewbox).X * value_x_ratio,
                                                         e.GetPosition(this.MainViewbox).Y * value_y_ratio));
            }
        }
        private void mainViewbox_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (this.colorBitmap != null && boxToTakeImage.IsNotComplete())
            {
                double value_x_ratio = 1.0 / (this.MainViewbox.ActualWidth / ImageSource.Width);
                double value_y_ratio = 1.0 / (this.MainViewbox.ActualHeight / ImageSource.Height);
                boxToTakeImage.SetBoxFromMouse(new Point(e.GetPosition(this.MainViewbox).X * value_x_ratio,
                                                         e.GetPosition(this.MainViewbox).Y * value_y_ratio));
            }
        }
        private void mainViewbox_MouseUp(object sender, System.Windows.Input.MouseEventArgs e)
        {
           boxToTakeImage.BoxIsComplete();
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.colorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }


        /// <summary>
        /// Convert BitmapSource in Bitmap (on the fly)
        /// </summary>
        protected System.Drawing.Bitmap GetBitmap(BitmapSource source)
        {
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(
                  source.PixelWidth,
                  source.PixelHeight,
                  System.Drawing.Imaging.PixelFormat.Format32bppPArgb
            );
            var data = bmp.LockBits(new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp.Size),
                                                                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                                                                System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            source.CopyPixels(
              Int32Rect.Empty,
              data.Scan0,
              data.Height * data.Stride,
              data.Stride
            );
            bmp.UnlockBits(data);
            return bmp;
        }

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        /// 
        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.colorBitmap != null)
            {
                // create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder encoder = new PngBitmapEncoder();

                // Image to bitmap
                System.Drawing.Bitmap staticColorBitmap = GetBitmap(this.colorBitmap);

                // create frame from the writable bitmap and add to encoder
                System.Drawing.Bitmap staticOutImage =  staticColorBitmap.Clone(boxToTakeImage.RectangleBox, staticColorBitmap.PixelFormat);
                
                //out time name
                string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

                //output name
                string path = Path.Combine(savePathImages, "KinectScreenshot-Color-" + time + ".png");

                // write the new file to disk
                try
                {
                    // write image
                    staticOutImage.Save(path);
                    //update status text field
                    this.StatusText = string.Format(Properties.Resources.SavedScreenshotStatusTextFormat, path);
                }
                catch (IOException)
                {
                    this.StatusText = string.Format(Properties.Resources.FailedScreenshotStatusTextFormat, path);
                }
            }
        }

        private byte[] BitmapSourceToArray(BitmapSource bitmapSource)
        {
            // Stride = (width) x (bytes per pixel)
            int stride = (int)bitmapSource.PixelWidth * (bitmapSource.Format.BitsPerPixel / 8);
            byte[] pixels = new byte[(int)bitmapSource.PixelHeight * stride];

            bitmapSource.CopyPixels(pixels, stride, 0);

            return pixels;
        }
        /// <summary>
        /// Handles the user clicking on the select path where you will be save the images
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SelectImagePath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = savePathImages;
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                savePathImages = dialog.SelectedPath;
            }
        }

        /// <summary>
        /// Handles the color frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;
                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }

                        this.colorBitmap.Unlock();
                    }
                }
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
#if false
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
#endif
         if (StatusText != null)
            {
                // on failure, set the status text
                this.StatusText = Properties.Resources.RunningStatusText;
                                                            
            }
            else {
                this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                         : Properties.Resources.SensorNotAvailableStatusText;
            }
}

private void DialogCreateSamples_Click(object sender, RoutedEventArgs e)
        {
            DialogCreateSamples dialog = new DialogCreateSamples(savePathImages);
            dialog.ShowDialog();
        }

#if false
        private void daUnoaDue_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            ToolsGenHaarCascade.MainWindow apri = new ToolsGenHaarCascade.MainWindow();
            apri.ShowDialog();
        }

#endif
    }
}
