using System;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Threading;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Windows;

namespace Microsoft.Samples.Kinect.BodyBasics.source
{
    class CNNThread
    {
        private Thread _thread;
        private Mutex _mutex;
        //CASCADE CLASSIFIER
        public ClassifiedObject[]  currentObjectClassified;
        protected BitmapSource lastImagePrev = null;
        protected Image<Bgra, Byte> lastImage = null;
        protected Image<Bgra, Byte> nextImage = null;
        protected double lastDepthMin = 0.0;
        protected double lastDepthMax = 0.0;
        protected double lastDepthPosFactorX = 0.0;
        protected double lastDepthPosFactorY = 0.0;
        protected int    lastDepthWidth = 0;
        protected ushort[] lastDepthImage = null;
        protected double nextDepthMin = 0.0;
        protected double nextDepthMax = 0.0;
        protected double nextDepthPosFactorX = 0.0;
        protected double nextDepthPosFactorY = 0.0;
        protected int    nextDepthWidth = 0;
        protected ushort[] nextDepthImage = null;
        protected int nextScale = 2;
        protected int nextContrast = 0;
        protected int nextAdj = 0;
        protected bool loop = false;

        public CNNThread(ClassifiedObject[] objects)
        {
            currentObjectClassified = objects;
            _thread = new Thread(new ThreadStart(this.RunThread));
            _mutex = new Mutex();
        }

        ~CNNThread()
        {
            if(loop) Join();
        }

        //get mutex
        public Mutex mutex
        {
            get
            {
                return _mutex;
            }
        }

        public BitmapSource imagePreview
        {
            get
            {
                return lastImagePrev;
            }
        }

        // Thread methods / properties
        public void Start() { loop = true;  _thread.Start(); }
        public void Join() { loop = false;  _thread.Join(); }
        public bool IsAlive { get { return _thread.IsAlive; } }

        public void AddAImage(
            Image<Bgra, Byte> new_image,
            int scale,
            int contrast,
            int adj,
            ushort[] depthMap,
            int depthWidth,
            double depthPosFactorX,
            double depthPosFactorY,
            double depthMin,
            double depthMax
        )
        {
            lock(_mutex)
            {
                nextImage = new_image;
                nextScale = scale;
                nextContrast = contrast;
                nextAdj = adj;
                nextDepthImage = depthMap;
                nextDepthWidth = depthWidth;
                nextDepthPosFactorX = depthPosFactorX;
                nextDepthPosFactorY = depthPosFactorY;
                nextDepthMin = depthMin;
                nextDepthMax = depthMax;
            }
        }

        // Override in base class
        public void RunThread()
        {
            while(loop)
            {
                Image<Bgra, Byte> image = null;
                int scale = 2;
                int contrast = 0;
                int adj = 0;
                bool compute = false;

                lock (_mutex)
                {
                    if (nextImage != null && lastImage != nextImage)
                    {
                        compute = true;
                        image = nextImage;
                        scale = nextScale;
                        contrast = nextContrast;
                        adj = nextAdj;
                        lastImage = image;
                        lastDepthImage = nextDepthImage;
                        lastDepthWidth = nextDepthWidth;
                        lastDepthPosFactorX = nextDepthPosFactorX;
                        lastDepthPosFactorY = nextDepthPosFactorY;
                        lastDepthMin = nextDepthMin;
                        lastDepthMax = nextDepthMax;
                    }
                }

                if (compute)
                {
                    Execute_CNN_and_Compute_Contrast(image, scale, contrast, adj);
                }
            }
          
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


            return new Emgu.CV.Image<Bgr, Byte>(resultBitmap);
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
            NewGraphics.DrawImage(TempBitmap, new System.Drawing.Rectangle(0, 0, TempBitmap.Width,
                TempBitmap.Height), 0, 0, TempBitmap.Width, TempBitmap.Height, System.Drawing.GraphicsUnit.Pixel, Attributes);

            Attributes.Dispose();
            NewGraphics.Dispose();

            return new Emgu.CV.Image<Bgr, Byte>(NewBitmap);
        }

        private void Execute_CNN_and_Compute_Contrast(Emgu.CV.Image<Bgra, Byte> frameImg, int scale, int contrast, int adjust)
        {
            //to cv
            frameImg = frameImg.Resize(frameImg.Width / scale, frameImg.Height / scale, Emgu.CV.CvEnum.Inter.Linear);
            //choose contrast 
            Image<Bgr, Byte>
            ca_frameImg = Contrast(frameImg.ToBitmap(), (int)contrast);
            ca_frameImg = AdjustBrightness(ca_frameImg.ToBitmap(), (int)adjust);
            //save prev
            lock (_mutex)
            {
                lastImagePrev = BitmapToImageSource(ca_frameImg.ToBitmap());
            }
            //CNN
            Execute_CNN(ca_frameImg, scale);
        }

        private void Execute_CNN(Image<Bgr, Byte> frameImg, int scaleFactor)
        {
            
            //Flip image (?)
            //frameImg = frameImg.Flip(Emgu.CV.CvEnum.FlipType.Horizontal);
            //Image to gray scale
            Image<Gray, Byte> grayframe = frameImg.Convert<Gray, byte>();
            ///////////////////////////////////////////////////////////////////
            for(int i = 0; i != currentObjectClassified.Length; ++i)
            if (currentObjectClassified[i].classifier != null)
            {
                System.Drawing.Rectangle[] gettedObjects = currentObjectClassified[i].classifier.DetectMultiScale(grayframe, 1.05, 3);
                currentObjectClassified[i].feature = "nodraw";
                foreach (var rectObj in gettedObjects)
                {
                    lock(_mutex)
                    {
                        currentObjectClassified[i].rectangle = rectObj;
                        currentObjectClassified[i].ScaleRectangle(scaleFactor);
                        currentObjectClassified[i].feature = "draw";
                        if (lastDepthImage != null)
                        {
                            currentObjectClassified[i].depth = GetZPosFromDepthFrame
                            (
                                lastDepthImage,
                                lastDepthWidth,
                                lastDepthMin,
                                lastDepthMax,
                                (int)(currentObjectClassified[i].center.X * (1.0 / lastDepthPosFactorX)),
                                (int)(currentObjectClassified[i].center.Y * (1.0 / lastDepthPosFactorY))
                            );
                        }
                    }
                }
            }
        }
        //delete
        [DllImport("gdi32")]
        static extern int DeleteObject(IntPtr o);
        //convert  to bitmapSource from bitmap
        BitmapSource BitmapToImageSource(System.Drawing.Bitmap source)
        {
            BitmapSource bitSrc = null;
            var hBitmap = source.GetHbitmap();
            try
            {
                bitSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap
                (
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions()
                );
            }
            catch (Win32Exception)
            {
                bitSrc = null;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
            //do not modify
            bitSrc.Freeze();
            //return
            return bitSrc;
        }

        //cet zvalue
        static protected double GetZPosFromDepthFrame
        (
            ushort[] depthData,
            int depthWidth,
            double min,
            double max,
            int pos_x,
            int pos_y
        )
        {
            //to do: if u_at_pos is 0, try to get depth from near points
            ushort u_at_pos = depthData[pos_x + pos_y * depthWidth];
            double norm_depth = u_at_pos > min ? ((double)(u_at_pos) - min) / (max - min) : 0.0;
            return norm_depth;
        }



    }

}
