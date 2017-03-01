namespace Microsoft.Samples.Kinect.BodyBasics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Windows.Threading;
    using System.Windows.Forms;
    using System.IO;
    using System.Globalization;
    using Microsoft.Samples.Kinect.BodyBasics.source;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Net;


    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {


#region Client

        // The port number for the remote device.
        private const int port = 42001;

        // ManualResetEvent instances signal completion.
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        private static ManualResetEvent receiveDone = new ManualResetEvent(false);

        // The response from the remote device.
        private static String response = String.Empty;

        //socket che permette il collegamento al Creator
        private Socket client;
        //stringa di testo da inviare al socket client
        private string invioDati;
        //contatore, arrivato a 20 effettua una chiamata di ricezione dei dati per svuotare la memoria
        private int itr = 0;
        //fine serve per chiudere il while se non si connette il socket e si esce dal programma
        private bool fine = false;
        //thread utilizzato per connettersi al socket
        Thread trd;


#endregion

        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// ColorFrame classification update time
        /// </summary>
        private const double TimeToClassificationColorFrame = 0.500;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private DepthFrameReader depthFrameReader = null;

        /// <summary>
        /// The depth values.
        /// </summary>
        private ushort[] depthData = null;
        private int depthWidth;
        private int depthHeight;
        private double depthMin = 0.0;
        private double depthMax = 1.0;

        /// <summary>
        /// Reader for color frames
        /// </summary>
        private ColorFrameReader colorFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        /// <summary>
        /// ColorFrame accumulatore time
        /// </summary>
        private double colorFrameAccTime = 0.0;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string sSelectedFile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\GitHub\\Tesi\\OGGETTI";


        //Last object clissified
        ClassifiedObject[] currentObjectClassified = new ClassifiedObject[2]
        {
            new ClassifiedObject(),
            new ClassifiedObject()
        };

        //cnn thread -> thread che esegue la classificazione
        CNNThread cnnThread;

        //Image scene
        private WriteableBitmap colorImageToDraw = null;

        //draw update timer
        protected DispatcherTimer drawUpdateTimer;
        
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            #region client


            try
            {
                // Establish the remote endpoint for the socket.
                // The name of the 
                // remote device is "host.contoso.com".

                // Create a TCP/IP socket.
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // cerca di connettersi al socket
                trd = new Thread(NewConnectThread);
                trd.Start();
                // Connect to the remote endpoint.
                //client.BeginConnect(remoteEP,new AsyncCallback(ConnectCallback), client);
                //connectDone.WaitOne();
            }
            catch (Exception er)
            {
                //MessageBox.Show("err01 "+ er.ToString(), "ERRORE", MessageBoxButton.OK, MessageBoxImage.Error);
                //Console.WriteLine(er.ToString());
            }

            #endregion

            //Cnn tread
            cnnThread = new CNNThread(currentObjectClassified);

            //start
            cnnThread.Start();

            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();


            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;
            
            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get the color (display) extents
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            // create the bitmap to display
            this.colorImageToDraw = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            

            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();
            
            // open the reader for the color frames
            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();

            this.depthFrameReader = this.kinectSensor.DepthFrameSource.OpenReader();


            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();
            
            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

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



        //Upload model Button
        private void UploadModel_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string pathDir = dialog.SelectedPath;
                string pathFile = pathDir + "\\cascade.xml";

                if (File.Exists(pathFile))
                {
                    //open model
                    sSelectedFile = pathFile;
                    bool doreset = true;

                    for (int i = 0; i != currentObjectClassified.Length; ++i)
                    {
                        if (currentObjectClassified[i].classifier == null) { doreset = false; break; }
                    }

                    if (doreset)
                    {
                        for (int i = 0; i != currentObjectClassified.Length; ++i)
                        {
                            currentObjectClassified[i].classifier = null;
                            currentObjectClassified[i].name = "";
                        }
                    }

                    for (int i = 0; i != currentObjectClassified.Length; ++i)
                    {
                        if (currentObjectClassified[i].classifier == null)
                        {
                            currentObjectClassified[i].classifier = new CascadeClassifier(sSelectedFile);
                            //get name
                            string[] pathsplit = pathDir.Split('@');
                            //if splitted:
                            currentObjectClassified[i].name = pathsplit.Length > 1 ? pathsplit[1] : "";
                            //done
                            break;
                        }
                    }

                    //ui upload
                    if (!currentObjectClassified[0].name.Equals(""))
                    {
                        Nome1.Content = currentObjectClassified[0].name;
                        X.Source = new BitmapImage(new Uri("/BodyBasics-WPF;component/ButtonIcon/check.png", UriKind.RelativeOrAbsolute));
                    }
                    else
                    {
                        Nome1.Content = "";
                        X.Source = new BitmapImage(new Uri("/BodyBasics-WPF;component/ButtonIcon/close.png", UriKind.RelativeOrAbsolute));
                    }
                    //ui
                    if (!currentObjectClassified[1].name.Equals(""))
                    {
                        Nome2.Content = currentObjectClassified[1].name;
                        X1.Source = new BitmapImage(new Uri("/BodyBasics-WPF;component/ButtonIcon/check.png", UriKind.RelativeOrAbsolute));
                    }
                    else
                    {
                        Nome2.Content = "";
                        X1.Source = new BitmapImage(new Uri("/BodyBasics-WPF;component/ButtonIcon/close.png", UriKind.RelativeOrAbsolute));
                    }

                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Not found cascade.xml in:\n" + pathDir, "Error to load model", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

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
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Body_FrameArrived;
            }

            if (this.depthFrameReader != null)
            {
                this.depthFrameReader.FrameArrived += this.Depth_FrameArrived;
            }

            if (this.colorFrameReader != null)
            {
                this.colorFrameReader.FrameArrived += this.Color_FrameArrived;
            }
        }

        private void Depth_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            using (DepthFrame depthframe = e.FrameReference.AcquireFrame())
            {
                if (depthframe != null)
                {
                    depthMin = depthframe.DepthMinReliableDistance;
                    depthMax = depthframe.DepthMaxReliableDistance;
                    depthWidth = depthframe.FrameDescription.Width;
                    depthHeight = depthframe.FrameDescription.Height;
                    depthData = new ushort[depthWidth * depthHeight];
                    depthframe.CopyFrameDataToArray(depthData);
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
            
            fine = true;
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.colorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }

            if (this.depthFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                this.depthFrameReader.Dispose();
                this.depthFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }

            try
            {
                // Release the socket.
                if (client != null)
                {
                    if (client.Connected)
                    {
                        client.Shutdown(SocketShutdown.Both);
                    }
                    client.Close();
                }


            }
            catch (Exception er)
            {
                //MessageBox.Show("err02 " + er.ToString(), "ERRORE", MessageBoxButton.OK, MessageBoxImage.Error);
                //Console.WriteLine(er.ToString());
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

                if ( this.colorImageToDraw != null)
                {
                    // Draw background to set the render size
                    dc.DrawImage(colorImageToDraw, new Rect(0, 0, colorImageToDraw.Width, colorImageToDraw.Height));
                    
                }
                // parametric label (solo una piccola miglioria che permette di rendere tutto parametrico) 
                var xy_labels = new System.Windows.Controls.Label[][]
                {
                    new  System.Windows.Controls.Label[]{ CenterX,CenterY },
                    new  System.Windows.Controls.Label[]{ CenterX2,CenterY2 }
                };

                //Message to Send to Server
                String sendMessage = "sensor-update ";
                for (int i = 0; i != currentObjectClassified.Length && i != xy_labels.Length; ++i)
                {
                    if (currentObjectClassified[i].feature.Equals("draw"))
                    {
                        xy_labels[i][0].Content = currentObjectClassified[i].center.X;
                        xy_labels[i][1].Content = currentObjectClassified[i].center.Y;

                        sendMessage +=  "/ "  + "Oggetto:" + currentObjectClassified[i].name +
                                        "/ "  + "Centro X: " + currentObjectClassified[i].center.X +
                                        "/ " + "Centro Y: " + currentObjectClassified[i].center.Y +
                                        "/ " + "Centro Z: " + currentObjectClassified[i].depth;


                    }
                }

                if (!sendMessage.Equals("sensor-update "))
                {
                    Send(sendMessage);
                }

                for (int i = 0; i < currentObjectClassified.Length; ++i)
                {
                    // Blocco la thread secondoaria fin quando la prima thread non ha fatto il disegno
                    lock(cnnThread.mutex)
                    {
                        if (currentObjectClassified[i].feature.Equals("draw"))
                        {
                            Rect rect = new Rect(currentObjectClassified[i].rectangle.X,
                                                 currentObjectClassified[i].rectangle.Y,
                                                 currentObjectClassified[i].rectangle.Width,
                                                 currentObjectClassified[i].rectangle.Height);
                            dc.DrawRectangle(null, new Pen(Brushes.Orange, 8), rect);
                            //name?
                            if (currentObjectClassified[i].name.Length > 0)
                            {
                                FormattedText formattedTest = new FormattedText
                                (
                                    currentObjectClassified[i].name,
                                    CultureInfo.GetCultureInfo("en-us"),
                                    System.Windows.FlowDirection.LeftToRight,
                                    new Typeface("Arial Bold"),
                                    40,
                                    Brushes.White
                                );

                                //draw background of name Object.
                                dc.DrawRoundedRectangle(Brushes.Orange, null,
                                new Rect(currentObjectClassified[i].rectangle.X - 4,
                                currentObjectClassified[i].rectangle.Y - 60, 250, 60), 10.0, 10.0);

                                //draw name of Object.
                                dc.DrawText(formattedTest,
                                            new Point(currentObjectClassified[i].rectangle.X,
                                                      currentObjectClassified[i].rectangle.Y - 60));

                            }
                        }

                        //sblocco la thread dopo aver disengnato
                    }
             
                }

                if (this.bodies != null)
                {
                    int penIndex = 0;
                    foreach (Body body in this.bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                            this.DrawClippedEdges(body, dc);

                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            this.DrawBody(joints, jointPoints, dc, drawPen);

                            this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                        }
                    }

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, colorImageToDraw.Width, colorImageToDraw.Height));
                }
            }
        }
  
        /// <summary>
        /// Handles the color frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Color_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {

                if (colorFrame != null)
                {
                    // k image to bytes
                    using (Microsoft.Kinect.KinectBuffer colorFrameBuffer = colorFrame.LockRawImageBuffer())
                    {
                        FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                        using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                        {
                            //lock image
                           this.colorImageToDraw.Lock();
                            
                            // verify data and write the new color frame data to the display bitmap
                            if ((colorFrameDescription.Width == this.colorImageToDraw.PixelWidth) &&
                                (colorFrameDescription.Height == this.colorImageToDraw.PixelHeight))
                            {
                                colorFrame.CopyConvertedFrameDataToIntPtr( this.colorImageToDraw.BackBuffer,
                                    (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                    ColorImageFormat.Bgra
                                );

                                this.colorImageToDraw.AddDirtyRect(new Int32Rect(0, 0, this.colorImageToDraw.PixelWidth, this.colorImageToDraw.PixelHeight));
                            }
                            //unlock
                            this.colorImageToDraw.Unlock();
                        }
                    }
                    //compute frame rate
                    colorFrameAccTime += colorFrame.ColorCameraSettings.FrameInterval.TotalSeconds;

                    //preview stessa cosa di sopra blocco la thread secondaria per far si che sia possibile mostrare l'immagine di preview
                    lock (cnnThread.mutex)  {
                        if (cnnThread.imagePreview != null)
                            img.Source = cnnThread.imagePreview;
                    }
                    //about 0.33 secs
                    if (TimeToClassificationColorFrame <= colorFrameAccTime)
                    {
                        colorFrameAccTime = 0.0;
                        //K image to Emgu image
                        int scale = 2;
                        int contrast = (int)slider.Value;
                        int adjust = (int)slider1.Value;
                        var frame = Kimage2CVimg(colorFrame);
                        //push imag chiamo il metodo AddAimage 
                        cnnThread.AddAImage(
                            frame,
                            scale,
                            contrast,
                            adjust,
                            depthData,
                            depthWidth,
                            //
                            Math.Ceiling( (double)colorFrame.FrameDescription.Width / depthWidth),
                            Math.Ceiling((double)colorFrame.FrameDescription.Height / depthHeight),
                            //
                            depthMin,
                            depthMax
                        );
                    }

                }
            }
        }


        // per salvare foto su cartella
#if false
          private void SaveFile(System.Drawing.Bitmap frameImg)
          {
            System.Drawing.Bitmap wmp;
            wmp = frameImg;

            string path = @"C:\Users\tavea\Documents\GitHub\Tesi\img\image" + ".png";
              wmp.Save(path);
            
  
        }

#endif

        //funzione che trasforma il frame nel formato di Emgu
        public Emgu.CV.Image<Bgra, Byte> Kimage2CVimg (ColorFrame frame)
        {
            var width = frame.FrameDescription.Width;
            var heigth = frame.FrameDescription.Height;
            var data = new byte[width * heigth * System.Windows.Media.PixelFormats.Bgra32.BitsPerPixel / 8];
            frame.CopyConvertedFrameDataToArray(data, ColorImageFormat.Bgra);

            var cvimg = new Emgu.CV.Image<Bgra, Byte>(width, heigth);
            cvimg.Bytes = data;

            return cvimg;
        }

        //funzione che trasforma il frame nel formato di Emgu (senza il canale a)
        public Emgu.CV.Image<Bgr, Byte> Kimage2CVimg (BitmapFrame frame)
        {
            MemoryStream outStream = new MemoryStream();
            BitmapEncoder enc = new BmpBitmapEncoder();
            enc.Frames.Add(frame);
            enc.Save(outStream);
            var bmp = new System.Drawing.Bitmap(outStream);

            return new Image<Bgr, Byte>(bmp);
        }


        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Body_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
          {
              bool dataReceived = false;

              using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
              {
                  if (bodyFrame != null)
                  {
                      if (this.bodies == null)
                      {
                          
                          this.bodies = new Body[bodyFrame.BodyCount];
                      }
                      // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                      // As long as those body objects are not disposed and not set to null in the array,
                      // those body objects will be re-used.
                      bodyFrame.GetAndRefreshBodyData(this.bodies);
                      dataReceived = true;
                  }
              }

              if (dataReceived)
              {

              }
          }

          /// <summary>
          /// Draws a body
          /// </summary>
          /// <param name="joints">joints to draw</param>
          /// <param name="jointPoints">translated positions of joints to draw</param>
          /// <param name="drawingContext">drawing context to draw to</param>
          /// <param name="drawingPen">specifies color to draw a specific body</param>
          private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
          {
              // Draw the bones
              foreach (var bone in this.bones)
              {
                  this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
              }

              // Draw the joints
              foreach (JointType jointType in joints.Keys)
              {
                  Brush drawBrush = null;

                  TrackingState trackingState = joints[jointType].TrackingState;

                  if (trackingState == TrackingState.Tracked)
                  {
                      drawBrush = this.trackedJointBrush;
                  }
                  else if (trackingState == TrackingState.Inferred)
                  {
                      drawBrush = this.inferredJointBrush;
                  }

                  if (drawBrush != null)
                  {
                      drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                  }
              }
          }

          /// <summary>
          /// Draws one bone of a body (joint to joint)
          /// </summary>
          /// <param name="joints">joints to draw</param>
          /// <param name="jointPoints">translated positions of joints to draw</param>
          /// <param name="jointType0">first joint of bone to draw</param>
          /// <param name="jointType1">second joint of bone to draw</param>
          /// <param name="drawingContext">drawing context to draw to</param>
          /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
          private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
          {
              Joint joint0 = joints[jointType0];
              Joint joint1 = joints[jointType1];

              // If we can't find either of these joints, exit
              if (joint0.TrackingState == TrackingState.NotTracked ||
                  joint1.TrackingState == TrackingState.NotTracked)
              {
                  return;
              }

              // We assume all drawn bones are inferred unless BOTH joints are tracked
              Pen drawPen = this.inferredBonePen;
              if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
              {
                  drawPen = drawingPen;
              }

              drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
          }

          /// <summary>
          /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
          /// </summary>
          /// <param name="handState">state of the hand</param>
          /// <param name="handPosition">position of the hand</param>
          /// <param name="drawingContext">drawing context to draw to</param>
          private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
          {
              switch (handState)
              {
                  case HandState.Closed:
                      drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                      break;

                  case HandState.Open:
                      drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                      break;

                  case HandState.Lasso:
                      drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                      break;
              }
          }

          /// <summary>
          /// Draws indicators to show which edges are clipping body data
          /// </summary>
          /// <param name="body">body to draw clipping information for</param>
          /// <param name="drawingContext">drawing context to draw to</param>
          private void DrawClippedEdges(Body body, DrawingContext drawingContext)
          {
              FrameEdges clippedEdges = body.ClippedEdges;

              if (clippedEdges.HasFlag(FrameEdges.Bottom))
              {
                  drawingContext.DrawRectangle(
                      Brushes.Red,
                      null,
                      new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
              }

              if (clippedEdges.HasFlag(FrameEdges.Top))
              {
                  drawingContext.DrawRectangle(
                      Brushes.Red,
                      null,
                      new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
              }

              if (clippedEdges.HasFlag(FrameEdges.Left))
              {
                  drawingContext.DrawRectangle(
                      Brushes.Red,
                      null,
                      new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
              }

              if (clippedEdges.HasFlag(FrameEdges.Right))
              {
                  drawingContext.DrawRectangle(
                      Brushes.Red,
                      null,
                      new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
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




        //open new window (cut depth)
        private void button2_Click(object sender, EventArgs e)
        {
            Windowdepth win2 = new Windowdepth(new ClassifiedObject[2] { currentObjectClassified[0], currentObjectClassified[1] });
            win2.Show();
        }


#region Client


        // funzione utilizzata per inviare la stringa al socket
        private void Send(String data)
        {
           
                // se è connesso
                if (client.Connected)
                {
                    // Convert the string data to byte data using ASCII encoding.
                    // converte la stringa in un array di byte
                    byte[] byteData = Encoding.ASCII.GetBytes(data);

                    // converte in byte la lunghezza della stringa data
                    byte[] intBytes = BitConverter.GetBytes(data.Length);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(intBytes);
                    byte[] buffer = intBytes;

                    // crea un nuovo array di byte dove vengono inseriti l'array contente in byte la lungezza di data ed il valore di data in byte
                    int newSize = buffer.Length + byteData.Length;
                    MemoryStream ms = new MemoryStream(new byte[newSize], 0, newSize, true, true);
                    ms.Write(buffer, 0, buffer.Length);
                    ms.Write(byteData, 0, byteData.Length);
                    byte[] merged = ms.GetBuffer();

                    // Begin sending the data to the remote device.
                    try
                    {
                        // avvia l'invio dei dati in modalità asincrona
                        client.BeginSend(merged, 0, merged.Length, 0,
                            new AsyncCallback(SendCallback), client);
                        // se itr = 20 allora avvia NewThread
                        if (itr == 20)
                        {
                            itr = 0;
                            //NewThread richiama la funzione per scaricare i dati ricevuti dal socket
                            Thread t = new Thread(NewThread);
                            t.Start();
                        }
                        else
                        {
                            itr++;
                        }
                    }
                    catch (SocketException ex)
                    {
                        client.Close();
                    }
                }
                else
                {
                    // se si chiude la connessione allora riprova a connetterti
                    if (!trd.IsAlive)
                    {
                        client = new Socket(AddressFamily.InterNetwork,
                            SocketType.Stream, ProtocolType.Tcp);
                        trd = new Thread(NewConnectThread);
                        trd.Start();
                    }
                    Console.WriteLine("");
                }
            }
        
        // invio dati in modo asincrono
        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);

                // Signal that all bytes have been sent.
                sendDone.Set();
            }
            catch (Exception e)
            {
                //MessageBox.Show("err04 " + e.ToString(), "ERRORE", MessageBoxButton.OK, MessageBoxImage.Error);
                //Console.WriteLine(e.ToString());
            }
        }

        // riceve i dati dal socket. serve per svuotare il buffer
        private static void Receive(Socket client)
        {
            try
            {
                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                //MessageBox.Show("err05 " + e.ToString(), "ERRORE", MessageBoxButton.OK, MessageBoxImage.Error);
                //Console.WriteLine(e.ToString());
            }
        }

        //ricezione dei dati in modo asincrono
        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Get the rest of the data.
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                //MessageBox.Show("err06 " + e.ToString(), "ERRORE", MessageBoxButton.OK, MessageBoxImage.Error);
                //Console.WriteLine(e.ToString());
            }
        }

        // funzione del thread che richiama la funzione ricezione dei dati da parte del socket
        void NewThread()
        {
            //code goes here
            Receive(client);
        }

        // funzione del thread che attiva la connessione
        void NewConnectThread()
        {
            //localhost
            IPHostEntry ipHostInfo = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);
            do
            {
                try
                {
                    client.Connect(remoteEP);
                }
                catch (Exception e)
                {
                    // prova a connettersi ogni 1/2 secondo
                    Thread.Sleep(500);
                }
                //client.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), client);

            } while ((!client.Connected) & (!fine));
            // termina quando è connesso oppure quando si chiude la finestra

        }
    

            // State object for receiving data from remote device.
            public class StateObject
            {
                // Client socket.
                public Socket workSocket = null;
                // Size of receive buffer.
                public const int BufferSize = 256;
                // Receive buffer.
                public byte[] buffer = new byte[BufferSize];
                // Received data string.
                public StringBuilder sb = new StringBuilder();
            }

#endregion

    }

}
