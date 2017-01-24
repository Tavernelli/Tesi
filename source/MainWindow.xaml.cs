namespace Microsoft.Samples.Kinect.BodyBasics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing.Imaging;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Runtime.InteropServices;
    using System.Windows.Threading;
    using System.Windows.Forms;
    using System.IO;
    using System.Globalization;
    using System.Net.Sockets;
    using System.Net;
    

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
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
        ClassifiedObject currentObjectClassified = new ClassifiedObject();

        //CASCADE CLASSIFIER
        CascadeClassifier cClassifierCurrent = new CascadeClassifier(@"C:\Users\tavea\Documents\GitHub\Tesi\DATA\cascade.xml"); 
      
        //Upload model Button
        private void UploadModel_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string pathDir  = dialog.SelectedPath;
                string pathFile = pathDir + "\\cascade.xml";
                if (File.Exists(pathFile))
                {
                    //open model
                    sSelectedFile = pathFile;
                    cClassifierCurrent = new CascadeClassifier(sSelectedFile);
                    //get name of object
                    string[] pathsplit = pathDir.Split('@');
                    //if splitted:
                    if(pathsplit.Length > 1)
                    {
                        currentObjectClassified.name = pathsplit[1];
                    }
                    else
                    {
                        currentObjectClassified.name = "";
                    }
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Not found cascade.xml in:\n" + pathDir, "Error to load model", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

        }

        //Image scene
        private WriteableBitmap colorImageToDraw = null;

        //draw update timer
        protected DispatcherTimer drawUpdateTimer;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
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

            if (this.colorFrameReader != null)
            {
                this.colorFrameReader.FrameArrived += this.Color_FrameArrived;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
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
                // Draw a transparent background to set the render size
                if (currentObjectClassified.feature.Equals( "draw" ))
                {
                    Rect rect = new Rect(currentObjectClassified.rectangle.X,
                                         currentObjectClassified.rectangle.Y,
                                         currentObjectClassified.rectangle.Width,
                                         currentObjectClassified.rectangle.Height);
                    dc.DrawRectangle(null, new Pen(Brushes.Orange, 8), rect);
                    //name?
                    if(currentObjectClassified.name.Length > 0)
                    {
                        FormattedText formattedTest = new FormattedText
                        (
                            currentObjectClassified.name,
                            CultureInfo.GetCultureInfo("en-us"),
                            System.Windows.FlowDirection.LeftToRight,
                            new Typeface("Arial Bold"),
                            40,
                            Brushes.White
                        );
                        
                        //draw background of name Object.
                        dc.DrawRoundedRectangle(Brushes.Orange, null, 
                        new Rect(currentObjectClassified.rectangle.X - 4, 
                        currentObjectClassified.rectangle.Y-60, 250, 60), 10.0, 10.0);

                        //draw name of Object.
                        dc.DrawText(formattedTest,
                                    new Point(currentObjectClassified.rectangle.X,
                                              currentObjectClassified.rectangle.Y - 60));
                        
                    }
                    
                }
                if(this.bodies != null)
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
                    //about 0.33 secs
                    if (TimeToClassificationColorFrame <= colorFrameAccTime)
                    {
                        //Reset accumulator
                        colorFrameAccTime = 0.0;
                        ///////////////////////////////////////////////////////////////////
                        //Preprocessing
                        int scaleFactor = 2;
                        //K image to Emgu image
                        Image<Bgr, Byte> frameImg = Kimage2CVimg(colorFrame);
                        //choose contrast 
                        frameImg = Contrast(frameImg.ToBitmap(), (int)slider.Value);
                        frameImg = AdjustBrightness(frameImg.ToBitmap(), (int)slider1.Value);
                        label1.Content = (int)slider.Value;
                        label2.Content = (int)slider1.Value;
                        img.Source = BitmapToImageSource(frameImg.ToBitmap()); 

                        //Rescale
                        frameImg = frameImg.Resize(frameImg.Width / scaleFactor, frameImg.Height / scaleFactor, Emgu.CV.CvEnum.Inter.Linear);
                       
                                           
                        //Flip image (?)
                        //frameImg = frameImg.Flip(Emgu.CV.CvEnum.FlipType.Horizontal);
                        //Image to gray scale
                        Image<Gray, Byte> grayframe = frameImg.Convert<Gray, byte>();
                        
                        ///////////////////////////////////////////////////////////////////
                        System.Drawing.Rectangle[] gettedObjects = cClassifierCurrent.DetectMultiScale(grayframe, 1.05, 3);
                        ///////////////////////////////////////////////////////////////////
                        //glasses objects
                        currentObjectClassified.feature = "nodraw";
                        foreach (var rectObj in gettedObjects)
                        {
                            //take center
                            currentObjectClassified.rectangle = rectObj;
                            currentObjectClassified.ScaleRectangle(scaleFactor);
                            currentObjectClassified.z = 0.0; //todo
                            currentObjectClassified.rotation = 0.0; //todo
                            currentObjectClassified.feature = "draw";
                        }
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
        public Emgu.CV.Image<Bgr, Byte> Kimage2CVimg (ColorFrame frame)
          {

              var width = frame.FrameDescription.Width;
              var heigth = frame.FrameDescription.Height;
              var data = new byte[width * heigth * System.Windows.Media.PixelFormats.Bgra32.BitsPerPixel / 8];
              frame.CopyConvertedFrameDataToArray(data, ColorImageFormat.Bgra);

              var bitmap = new System.Drawing.Bitmap (width, heigth, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
              var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),  ImageLockMode.WriteOnly,  bitmap.PixelFormat);
              Marshal.Copy(data, 0, bitmapData.Scan0, data.Length);
              bitmap.UnlockBits(bitmapData);

              return new Emgu.CV.Image <Bgr, Byte> (bitmap);
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
              // on failure, set the status text
              this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                              : Properties.Resources.SensorNotAvailableStatusText;
          }


//-------------- buttun Send -> Cut Depth -> And adjsut brightes and contrast method.
        private TcpListener tcpListener = new TcpListener(IPAddress.Any, 3200);
          
        //send to android client
          private void button1_Click(object sender, RoutedEventArgs e)
          {
                 
                  string name = null;
                  string _height = null;
                  string _Width = null;
                  if (currentObjectClassified.feature.Equals("draw"))
                  {
                      name = currentObjectClassified.name;
                //center of obj
                int CenterX = currentObjectClassified.rectangle.X + (int)(currentObjectClassified.rectangle.Width/2);
                int Centery = currentObjectClassified.rectangle.Y + (int)(currentObjectClassified.rectangle.Height / 2);
                _height = ((currentObjectClassified.rectangle.Height/100)*2.54).ToString();
                      _Width = ((currentObjectClassified.rectangle.Width /100)*2.54).ToString();
                
                  }

                Server TCPServer = new Server(name, tcpListener, _height, _Width);
            

        }

        //open new window (cut depth)
        private void button2_Click(object sender, EventArgs e)
        {
            Windowdepth win2 = new Windowdepth(cClassifierCurrent);
            win2.Show();

        }


        // Adjust Contrast Method
        public static Emgu.CV.Image<Bgr, Byte> Contrast(System.Drawing.Bitmap sourceBitmap, int threshold)
          {
              BitmapData sourceData = sourceBitmap.LockBits(new System.Drawing.Rectangle(0, 0,
                                          sourceBitmap.Width, sourceBitmap.Height),
                                          ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            
              byte[] pixelBuffer = new byte[sourceData.Stride * sourceData.Height];


              Marshal.Copy(sourceData.Scan0, pixelBuffer, 0, pixelBuffer.Length);


              sourceBitmap.UnlockBits(sourceData);


              double contrastLevel = Math.Pow((100.0 + threshold) / 100.0, 2);


              double blue = 0;
              double green = 0;
              double red = 0;


              for (int k = 0; k + 4 < pixelBuffer.Length; k += 4)
              {
                  blue = ((((pixelBuffer[k] / 255.0) - 0.5) *
                              contrastLevel) + 0.5) * 255.0;


                  green = ((((pixelBuffer[k + 1] / 255.0) - 0.5) *
                              contrastLevel) + 0.5) * 255.0;


                  red = ((((pixelBuffer[k + 2] / 255.0) - 0.5) *
                              contrastLevel) + 0.5) * 255.0;


                  if (blue > 255)
                  { blue = 255; }
                  else if (blue < 0)
                  { blue = 0; }


                  if (green > 255)
                  { green = 255; }
                  else if (green < 0)
                  { green = 0; }


                  if (red > 255)
                  { red = 255; }
                  else if (red < 0)
                  { red = 0; }


                  pixelBuffer[k] = (byte)blue;
                  pixelBuffer[k + 1] = (byte)green;
                  pixelBuffer[k + 2] = (byte)red;
              }


              System.Drawing.Bitmap resultBitmap = new System.Drawing.Bitmap(sourceBitmap.Width, sourceBitmap.Height);


              BitmapData resultData = resultBitmap.LockBits(new System.Drawing.Rectangle(0, 0,
                                          resultBitmap.Width, resultBitmap.Height),
                                          ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);



              Marshal.Copy(pixelBuffer, 0, resultData.Scan0, pixelBuffer.Length);
              resultBitmap.UnlockBits(resultData);


              return new Emgu.CV.Image<Bgr, Byte> (resultBitmap);
          }

            // Adjust Brightess Method
            public static Emgu.CV.Image<Bgr, Byte> AdjustBrightness(System.Drawing.Bitmap Image, int value)
            {
            System.Drawing.Bitmap TempBitmap = Image;
            float FinalValue = (float)value / 255.0f;
            System.Drawing.Bitmap NewBitmap = new System.Drawing.Bitmap(TempBitmap.Width, TempBitmap.Height);
            System.Drawing.Graphics NewGraphics = System.Drawing.Graphics.FromImage(NewBitmap);
            float[][] FloatColorMatrix =
            {
                new float [] {1,0,0,0,0},
                new float [] {0,1,0,0,0},
                new float [] {0,0,1,0,0},
                new float [] {0,0,0,1,0},
                new float [] {FinalValue, FinalValue, FinalValue, 1, 1}

            };
            ColorMatrix NewColorMatrix = new ColorMatrix(FloatColorMatrix);
            ImageAttributes Attributes = new ImageAttributes();
            Attributes.SetColorMatrix(NewColorMatrix);
            NewGraphics.DrawImage(TempBitmap, new System.Drawing.Rectangle(0,0, TempBitmap.Width, 
                TempBitmap.Height), 0, 0, TempBitmap.Width, TempBitmap.Height, System.Drawing.GraphicsUnit.Pixel, Attributes);

            Attributes.Dispose();
            NewGraphics.Dispose();

            return new Emgu.CV.Image<Bgr, Byte> (NewBitmap);
            }

   
        //convert  to bitmapSource from bitmap
        BitmapImage BitmapToImageSource(System.Drawing.Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        private void EventClosed(object sender, EventArgs e)
        {
            if (colorFrameReader != null)
            {
                colorFrameReader.Dispose();

            }
            if (bodyFrameReader != null)
            {
                bodyFrameReader.Dispose();

            }

            if (kinectSensor != null)
            {
                kinectSensor.Close();
            }

            this.Close();
            


        }
    }
  }
