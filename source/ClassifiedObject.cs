using System.Drawing;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    class ClassifiedObject
    {

        private Rectangle mRect;
        private double mZ = 0;
        private double mRot = 0;
        private string mFeature = "";

        public ClassifiedObject(){ }

        //Object Rectangle
        public Rectangle rectangle
        {
            set
            {
                mRect = value;
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

        // rectangle,z and rotation to 0
        public void SetAllToZero()
        {
            this.mRect = new Rectangle();
            this.mZ = 0;
            this.mRot = 0;
        }
    }
}
