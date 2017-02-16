using System.Drawing;
using Emgu.CV;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    public class ClassifiedObject
    {

        private Rectangle mRect;
        private double mZ = 0;
        private double mRot = 0;
        private string mFeature = "";
        private string mName = "";
        private CascadeClassifier mClassifier;
        private Point mCenter = new Point();

        public ClassifiedObject(){ }

        public CascadeClassifier classifier
        {
            set
            {
                mClassifier = value;
            }
            get
            {
                return mClassifier;
            }
        }

        public Point center
        {
            get
            {
                return mCenter;
            }
        }

        //Object Rectangle
        public Rectangle rectangle
        {
            set
            {
                mRect = value;
                mCenter.X = mRect.X + (mRect.Width / 2);
                mCenter.Y = mRect.Y + (mRect.Height / 2);
            }
            get
            {
                return mRect;
            }
        }

        //Rescale rectangle
        public void ScaleRectangle(int scale)
        {
            rectangle = new Rectangle(
                rectangle.X * scale,
                rectangle.Y * scale,
                rectangle.Width * scale,
                rectangle.Height * scale
            );
        }

        //Object Z axis (depth)
        public double z
        {
            set
            {
                mZ = value;
            }
            get
            {
                return mZ;
            }
        }

        //Object rotation (on screen)
        public double rotation
        {
            set
            {
                mRot = value;
            }
            get
            {
                return mRot;
            }
        }

        //Info about object
        public string feature
        {
            set
            {
                mFeature = value;
            }
            get
            {
                return mFeature;
            }
        }

        //Nome of object
        public string name
        {
            set
            {
                mName = value;
            }
            get
            {
                return mName;
            }
        }


        // rectangle,z and rotation to 0
        public void SetAllToZero()
        {
            this.mRect = new Rectangle();
            this.mZ = 0;
            this.mRot = 0;
        }
    }
}
