using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Desktop_navigation
{
    internal class Utilities
    {
        static Font font = new Font("Arial", 15);
        static SolidBrush brush = new SolidBrush(Color.Red);

        static StringFormat format = new StringFormat()
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        public static void drawLineBasepointToCurrentNosepoint(ref Bitmap frameToBeShown, System.Drawing.Point p)
        {
            using (Graphics graphics = Graphics.FromImage(frameToBeShown))
            {
                using (Pen pen = new Pen(Color.Green))
                {
                    graphics.DrawLine(pen, 150, 100, p.X, p.Y);
                }
            }
        }

        public static void drawCircleAroundBasePoint(ref Bitmap frameToBeShown)
        {
            using (Graphics graphics = Graphics.FromImage(frameToBeShown))
            {
                using (Pen pen = new Pen(Color.Green))
                {
                    graphics.DrawEllipse(pen, 150, 100, 1, 1);
                    graphics.DrawEllipse(pen, 150 - 15, 100 - 15, 30, 30);

                }
            }

        }

        public static void writeText(String s, ref Bitmap frameToBeShown)
        {
            Rectangle rect = new Rectangle(0, 0, 280, 30);
            String text = "";
            if (s == "both")
                text = "Keep both eyes open";
            else if (s == "left")
                text = "Close Left eye";
            else if (s == "right")
                text = "Close Right eye";
            else if (s == "laugh")
                text = "Give us a perfect laugh";

            using (Graphics graphics = Graphics.FromImage(frameToBeShown))
            {
                graphics.DrawString(text, font, brush, rect, format);
            }
        }
        public static double findMedian(List<double> ls)
        {
            
            if(ls.Count % 2 != 0)
            {
                return ls[ls.Count / 2];
            }
            else
            {
                return ls[ls.Count / 2] + ls[(ls.Count / 2) - 1] / 2;
            }
        }

        public static void drawLandmarks(List<Point> ls, ref Bitmap bitmapFrame)
        {
            using (Graphics graphics = Graphics.FromImage(bitmapFrame))
            {
                using (Pen pen = new Pen(Color.Green, 2))
                {
                    for (int i = 0; i < ls.Count; i++)
                    {
                        graphics.DrawEllipse(pen, ls[i].X, ls[i].Y, 1, 1);
                    }
                }
            }
        }

        public static void drawRectangle(ref Bitmap frameToBeShown, Rectangle rect)
        {
            using (Graphics gs = Graphics.FromImage(frameToBeShown))
            {
                using (Pen pen = new Pen(Color.Red))
                {
                    gs.DrawRectangle(pen, rect);
                }
            }
        }


        public static double findFps(DateTime cuFps, ref DateTime prFps, ref int avg, ref int count)
        {
            TimeSpan ts1 = cuFps.Subtract(prFps);
            var fps = 1 / (ts1.TotalSeconds);
            fps = Math.Round(fps);
            count += 1;
            avg += (int)fps;
            prFps = cuFps;
            return fps;
        }

        public static void drawFps(double fps, ref Bitmap bitmapFrame)
        {
            using (Graphics gs = Graphics.FromImage(bitmapFrame))
            {
                using (Pen pen = new Pen(Color.Red))
                {
                    gs.DrawString($"{fps}", font, brush, 5, 0);
                }
            }
        }

        public static double euclideanDist(Point p1, Point p2)
        {
            float x1 = p1.X, y1 = p1.Y;
            float x2 = p2.X, y2 = p2.Y;
            var result = Math.Sqrt((Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2)));
            return result;

            /* int x1 = p1.X, y1 = p1.Y;
             int x2 = p2.X, y2 = p2.Y;
             int x = x2 - x1, y = y2 - y1;
             var result = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
             return result;*/
        }

        public static double eyeAspectRation(Point[] eye)
        {
            var A = euclideanDist(eye[1], eye[5]);

            var B = euclideanDist(eye[2], eye[4]);
            //compute the euclidean distance between the horizontal
            //eye landmark (x, y)-coordinates
            var C = euclideanDist(eye[0], eye[3]);
            //compute the eye aspect ratio
            var ear = (A + B) / (2 * C) * 1.0;
            return ear;


            /*var A = euclideanDist(eye[1], eye[5]);
            var B = euclideanDist(eye[2], eye[4]);

            var verticalAvg = A + B / 2;

            var horizontalAvg = euclideanDist(eye[0], eye[3]);

            var dist = verticalAvg / horizontalAvg;
            dist = Math.Round(dist, 2);
            return dist;*/
        }

        public static double mouthAspectRatio(Point[] mouth)
        {
            var A = euclideanDist(mouth[2], mouth[10]);
            var B = euclideanDist(mouth[4], mouth[8]);

            var C = euclideanDist(mouth[0], mouth[6]);

            var ear = (A + B) / (2.0 * C);

            return Math.Round(ear, 2);

            /* var A = euclideanDist(mouth[2], mouth[10]);
             var B = euclideanDist(mouth[4], mouth[8]);

             var verticalAvg = A + B / 2;
             var horizontalAvg = euclideanDist(mouth[0], mouth[6]);

             var dist = verticalAvg / horizontalAvg;
             dist = Math.Round(dist, 2);
             return dist;*/
        }
    }
}
