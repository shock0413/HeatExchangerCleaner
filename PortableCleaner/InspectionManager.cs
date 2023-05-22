using OpenCvSharp;
using OpenCvSharp.Blob;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PortableCleaner
{
    public class InspectionManager
    {
        /// <summary>
        /// filter Min Max = mm
        /// </summary>
        /// <param name="inspectionInfo"></param>
        /// <param name="filterZMin"></param>
        /// <param name="filterZMax"></param>
        public static BitmapSource GetBinaryImage(InspectionInfo inspectionInfo, int filterZMin, int filterZMax)
        {
            double zResoulution = inspectionInfo.zResolution;
            short[] datas = inspectionInfo.data;
            long width = inspectionInfo.width;
            long length = inspectionInfo.length;

            byte[] result = new byte[datas.Length];

            double avg = 0;
            long avgCount = 0;
            for(int i = 0; i < datas.Length; i++)
            {
                if(datas[i] != short.MinValue)
                {
                    avgCount++;
                    avg += datas[i];
                }
            }

            avg /= avgCount;

            //이후 avg는 mm임
            double minZ = avg - filterZMin / zResoulution;
            double maxZ = avg + filterZMax / zResoulution;

            for (int i = 0; i < datas.Length; i++)
            {
                if (datas[i] >= minZ && datas[i] <= maxZ)
                {
                    result[i] = 0;
                }
                else
                {
                    result[i] = 255;
                }
            }

            PixelFormat pf = PixelFormats.Gray8;
            long rawStride = (width * pf.BitsPerPixel + 7) / 8;

            BitmapSource bitmap = BitmapSource.Create((int)width, (int)length,
                96, 96, pf, null,
                result, (int)rawStride);

            bitmap.Freeze();

            return bitmap;
        }

        public static List<StructBlob> GetBlobs(BitmapSource image, int areaMin, int areaMax)
        {
            List<StructBlob> result = new List<StructBlob>();

            Mat mat = BitmapSourceConverter.ToMat(image);
            Mat thresholeMat = new Mat();

            Mat element = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5));
            Cv2.MorphologyEx(mat, mat, MorphTypes.Close, element);

            Cv2.Threshold(mat, thresholeMat, 100, 255, ThresholdTypes.Binary);

            CvBlobs blobTool;
            blobTool = new CvBlobs();

            blobTool.Label(thresholeMat);

            List<KeyValuePair<int, CvBlob>> allBlobList = blobTool.ToList();

            /*
            Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + "\\blobs\\", true);
            if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\blobs\\") == false)
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\blobs\\");
            }
            */

            for (int i = 0; i < allBlobList.Count; i++)
            {

                CvBlob blob = (CvBlob)allBlobList[i].Value;

                if (blob.Area > areaMin && blob.Area < areaMax)
                {
                    Mat cropTest = new Mat(new Size(mat.Cols, mat.Rows), MatType.CV_8UC1);
                    CvContourPolygon polygon = blob.Contour.ConvertToPolygon();
                    Cv2.FillPoly(cropTest, new Point[][] { polygon.ToArray() }, Scalar.White);
                    blob.InternalContours.ForEach(x =>
                    {
                        Cv2.FillPoly(cropTest, new Point[][] { x.ConvertToPolygon().ToArray() }, Scalar.Black);
                    });

                    Mat crop = new Mat(cropTest, blob.Rect);

                    // crop.SaveImage(AppDomain.CurrentDomain.BaseDirectory + "\\blobs\\" + i + ".bmp");

                    BitmapSource source = BitmapSourceConverter.ToBitmapSource(crop.Clone());
                    source.Freeze();

                    StructBlob structBlob = new StructBlob(crop, blob.Rect.X, blob.Rect.Y, blob.Rect.Width, blob.Rect.Height, (int)blob.Rect.Width / 2, (int)blob.Rect.Height / 2);
                    result.Add(structBlob);
                }
            }

            return result;
        }

        public static void CircleCheck(StructBlob blob)
        {
            Mat mat = blob.mat;

            CircleSegment[] circles1 = Cv2.HoughCircles(mat, HoughMethods.Gradient, 1, 100, 100, 5, 15, 30);

            for (int j = 0; j < circles1.Length; j++)
            {
                int ax = (int)Math.Max(0, circles1[j].Center.X - circles1[j].Radius);
                int ay = (int)Math.Max(0, circles1[j].Center.Y - circles1[j].Radius);

                int aw, ah;

                if (ax + circles1[j].Radius * 2 > mat.Width)
                {
                    aw = mat.Width - ax;
                }
                else
                {
                    aw = (int)circles1[j].Radius * 2;
                }

                if (ay + circles1[j].Radius * 2 > mat.Height)
                {
                    ah = mat.Height - ay;
                }
                else
                {
                    ah = (int)circles1[j].Radius * 2;
                }

                double sum = 0;


                #region 버튼이 아닌 원 걸러내기...
                Mat circleMat = new Mat(mat, new OpenCvSharp.Rect(new OpenCvSharp.Point(ax, ay), new OpenCvSharp.Size(aw, ah)));
 
                sum = 0;
                double avg;

                for (int y = 0; y < circleMat.Height; y++)
                {
                    for (int x = 0; x < circleMat.Height; x++)
                    {
                        sum += circleMat.At<Vec3b>(y, x)[0];
                    }
                }

                avg = sum / (circleMat.Width * circleMat.Height);

                if (avg < 100)
                {
                    Console.WriteLine("avg Pass : " + avg);
                    continue;
                }
                #endregion

                OpenCvSharp.Point p = new OpenCvSharp.Point();
                p.X = (int)circles1[j].Center.X;
                p.Y = (int)circles1[j].Center.Y;
                blob.centerResult.Add(p);
            }
        }

        public static BitmapSource DrawResult(BitmapSource image, List<StructBlob> blobs)
        {
            Mat mat = BitmapSourceConverter.ToMat(image);
            mat = mat.CvtColor(ColorConversionCodes.GRAY2RGB);
            for(int i = 0; i < blobs.Count; i++)
            {
                if(blobs[i].IsCircle)
                {
                    Console.WriteLine(i + " center Count : " + blobs[i].centerResult.Count);

                    int x = (int)blobs[i].VisionX;
                    int y = (int)blobs[i].VisionY;
                    
                    mat.DrawMarker(x, y, Scalar.Blue, thickness: 5);
                    mat.PutText(i.ToString(), new Point(x, y), HersheyFonts.HersheyComplex, 1.5, Scalar.Red, thickness:2);
                }
               
            }

            // mat.SaveImage(AppDomain.CurrentDomain.BaseDirectory + "\\drawResult.bmp");
            BitmapSource result = mat.ToBitmapSource();
            if(result.CanFreeze)
            {
                result.Freeze();
            }

            return result;
        }

        public class StructBlob : ViewModel
        {
            public Mat mat;
            public List<Point> centerResult = new List<Point>();
            public int startX;
            public int startY;
            public int width;
            public int height;
            public int centerX;
            public int centerY;

            public int Index { get; set; }
            public double MotorX { get; set; }
            public double MotorZ { get; set; }

            public double VisionX { get { return startX + centerX; } }
            public double VisionY { get { return startY + centerY; } }

            private double motorXResult;
            public double MotorXResult { get { return motorXResult; } set { motorXResult = value; NotifyPropertyChanged("MotorXResult"); } }
            private double motorZResult;
            public double MotorZResult { get { return motorZResult; } set { motorZResult = value; NotifyPropertyChanged("MotorZResult"); } }

            public bool IsCircle { get { return centerResult.Count > 0; } }

            public StructBlob(Mat mat, int startX, int startY, int width, int height, int centerX, int centerY)
            {
                this.mat = mat;
                this.startX = startX;
                this.startY = startY;
                this.width = width;
                this.height = height;
                this.centerX = centerX;
                this.centerY = centerY;
            }

        }
    }
}
