using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video.DirectShow;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Dnn;
using Emgu.CV.CvEnum;
using System.Runtime.InteropServices;
using Point = System.Drawing.Point;

using DlibDotNet;
using Dlib = DlibDotNet.Dlib;
using DlibDotNet.Extensions;

using Numpy;

namespace Desktop_navigation
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        Utilities util = new Utilities();  

        Net _faceNet;
        ShapePredictor sp;

        FilterInfoCollection filter;
        VideoCapture capture;
        VideoCapture captureStrm;

        private Image<Bgr, byte> currentFrame = null;
        private Image<Bgr, byte> newImage = null;
        Mat frame = new Mat();
        Mat frameStrm = new Mat();
        public Bitmap frameToBeShown;
        //private Image<Bgr, byte> resizedFrame = null;
        private Array2D<RgbPixel> dlibimg = new Array2D<RgbPixel>();
        private FullObjectDetection shape;

        Mat blob, detection;
        float confidence;
        Size size = new Size(300, 200); // For the blob image size
        MCvScalar meanForBlob = new MCvScalar(104, 177, 123);
        int rows, cols;
        float[] temp;
        int x1, y1, x2, y2;

        DlibDotNet.Rectangle faceCordinates;
        List<Point> listOfPoints = new List<Point>();
        List<Point> listOfMouthPoints = new List<Point>();

        bool blinked = false;
        bool caliberationFinished = false;
        bool formClosed = false;
        bool frameReceived;
        bool detectButtonClicked = false;
        bool checkFps = false;

        bool leftEyeMsg = true, bothEyeMsg = true, rightEyeMsg = true, laughMsg = true;

        int count = 0, avg = 0;
        double fps = 0;
        DateTime prFps = DateTime.Now;
        String msg, title;

        int frameHeight, frameWidth;

        // Left and right eye srating points
        private readonly short leftEyeStart = 42;
        private readonly short rightEyeStart = 36;

        Point[] leftEyeCoord;
        Point[] rightEyeCoord;
        Point[] mouthCoord;

        private double leftEyeEAR;
        private double rightEyeEAR;
        private double mouthEAR;

        DateTime caliberTime = DateTime.Now;
        DateTime curBlnk = DateTime.Now;
        DateTime prBlnk = DateTime.Now;

        int skipFrame = 0;
        private void Form1_Load(object sender, EventArgs e)
        {

            const string configFile = "deploy.prototxt.txt";
            const string faceModel = "res10_300x300_ssd_iter_140000_fp16.caffemodel";
            _faceNet = DnnInvoke.ReadNetFromCaffe(configFile, faceModel);
            sp = ShapePredictor.Deserialize("shape_predictor.dat");

            filter = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo item in filter)
                comboBox1.Items.Add(item.Name);
            comboBox1.SelectedIndex = 0;
        }

        bool caliberationButtonClicked = false;
        /* List<double> lclick = new List<double>();
         List<double> rclick = new List<double>();
         List<double> mouthScroll = new List<double>();
         List<double> lclickArea = new List<double>();
         List<double> rclickArea = new List<double>();*/

        NDarray lclick = np.array<double>();
        NDarray rclick = np.array<double>();
        NDarray mouthScroll = np.array<double>();
        NDarray lclickArea = np.array<double>();
        NDarray rclickArea = np.array<double>();

        double EARDiff, rightEyeArea, leftEyeArea;
        private void CaliberBtn_Click(object sender, EventArgs e)
        {
            if (!caliberationButtonClicked)
            {
                caliberationButtonClicked = true;
                var index = comboBox1.SelectedIndex;
                VideoCapture.API captureApi = VideoCapture.API.DShow;
                capture = new VideoCapture(index, captureApi)
                {
                    FlipHorizontal = true
                };
                capture.ImageGrabbed += streamingForCaliberation;
                capture.Start();
                
                caliberTime = DateTime.Now;
            }
        }

        private void streamingForCaliberation(object sender, EventArgs e)
        {
            frameReceived = capture.Retrieve(frame); 
            if (!frameReceived)
            {
                msg = "Cam not working, please check";
                title = "Error";
                DialogResult result = MessageBox.Show(msg, title);
                if (result == DialogResult.OK)
                    this.Close();
            }

            else
            {
                currentFrame = frame.ToImage<Bgr, byte>().Resize(300, 200, Inter.Cubic);
                frameToBeShown = currentFrame.ToBitmap();

                newImage = currentFrame.Clone();

                frameHeight = newImage.Rows;
                frameWidth = newImage.Cols;

                using (blob = DnnInvoke.BlobFromImage(newImage, 1.0, size,
                                                         meanForBlob, false, false))
                    _faceNet.SetInput(blob, "data");

                using (detection = _faceNet.Forward("detection_out"))
                {
                    rows = detection.SizeOfDimension[2];
                    cols = detection.SizeOfDimension[3];

                    temp = new float[rows * cols];
                    Marshal.Copy(detection.DataPointer, temp, 0, temp.Length);
                }

                if (listOfPoints != null)
                    listOfPoints.Clear();
                for (int i = 0; i < 1; i++)
                {
                    confidence = temp[i * cols + 2];

                    if (confidence > 0.7)
                    {
                        x1 = (int)(temp[i * cols + 3] * frameWidth);
                        y1 = (int)(temp[i * cols + 4] * frameHeight);
                        x2 = (int)(temp[i * cols + 5] * frameWidth);
                        y2 = (int)(temp[i * cols + 6] * frameHeight);

                        using (dlibimg = frameToBeShown.ToArray2D<RgbPixel>())
                        {
                            //preparing coordinates of face
                            faceCordinates = new DlibDotNet.Rectangle(x1, y1, x2, y2);

                            // find the landmark points of the face
                            using (shape = sp.Detect(dlibimg, faceCordinates))
                            {

                                // Get the landmark points on the image
                                for (var j = 0; j < shape.Parts; j++)
                                {
                                    var point = shape.GetPart((uint)j);
                                    Point xys = new Point(point.X, point.Y);
                                    listOfPoints.Add(xys);
                                }
                            }
                        }
                        Utilities.drawLandmarks(listOfPoints, ref frameToBeShown);

                        rightEyeCoord = listOfPoints.Skip(leftEyeStart).Take(6).ToArray();
                        leftEyeCoord = listOfPoints.Skip(rightEyeStart).Take(6).ToArray();

                        //The Aspect ratio of eyes to know the blinks of eyes 
                        rightEyeEAR = Utilities.eyeAspectRation(rightEyeCoord);
                        leftEyeEAR = Utilities.eyeAspectRation(leftEyeCoord);

                        //The Aspect ratio of mouth to know the laugh to activate scroll
                        mouthCoord = listOfPoints.Skip(48).Take(13).ToArray();
                        mouthEAR = Utilities.mouthAspectRatio(mouthCoord);

                        EARDiff = (leftEyeEAR - rightEyeEAR) * 100;
                        
                        
                        Emgu.CV.Util.VectorOfPoint vre = new Emgu.CV.Util.VectorOfPoint();
                        vre.Push(rightEyeCoord);
                        rightEyeArea = CvInvoke.ContourArea(vre);

                        Emgu.CV.Util.VectorOfPoint vle = new Emgu.CV.Util.VectorOfPoint();
                        vle.Push(leftEyeCoord);
                        leftEyeArea = CvInvoke.ContourArea(vle);

                        //Console.WriteLine($"EARDiff: {EARDiff} & RA: {rightEyeArea} & LA: {leftEyeArea}");

                        Emgu.CV.Util.VectorOfPoint vm = new Emgu.CV.Util.VectorOfPoint();
                        vm.Push(mouthCoord);
                        var mouthArea = CvInvoke.ContourArea(vm);

                        DateTime curr = DateTime.Now;
                        TimeSpan ts = curr.Subtract(caliberTime);

                        if(ts.TotalSeconds < 5)
                        {
                            if (ts.TotalSeconds < 2)
                            {
                                Utilities.writeText("both", ref frameToBeShown);
                                bothEyeMsg = false;
                            }
                        }
                        else if (ts.TotalSeconds > 5 && ts.TotalSeconds < 10)
                        {
                            if (ts.TotalSeconds < 7)
                            {
                                Utilities.writeText("left", ref frameToBeShown);
                                leftEyeMsg = false;
                            }
                            else
                            {
                                //Console.WriteLine($"Time: {ts.TotalSeconds}");
                                //lclick.Add(EARDiff);
                                //lclickArea.Add(leftEyeArea);
                                lclick = np.append(lclick, (NDarray)EARDiff);
                                lclickArea = np.append(lclickArea, (NDarray)leftEyeArea);
                                Console.WriteLine($"lclick; {EARDiff} & leftEyeArea: {leftEyeArea}");
                            }
                        }
                        else if (ts.TotalSeconds > 12 && ts.TotalSeconds < 17)
                        {
                            if (ts.TotalSeconds < 14)
                            {
                                Utilities.writeText("right", ref frameToBeShown);
                                rightEyeMsg = false;
                            }
                            else
                            {
                                //Console.WriteLine($"Time: {ts.TotalSeconds}");
                                //rclick.Add(EARDiff);
                                //rclickArea.Add(rightEyeArea);
                                rclick = np.append(rclick, (NDarray)EARDiff);
                                rclickArea = np.append(rclickArea, (NDarray)rightEyeArea);
                                Console.WriteLine($"rclick; {EARDiff} & rightEyeArea: {rightEyeArea}");
                            }
                        }

                        else if (ts.TotalSeconds > 19 && ts.TotalSeconds < 24)
                        {
                            if (ts.TotalSeconds < 21)
                            {
                                Utilities.writeText("laugh", ref frameToBeShown);
                                laughMsg = false;
                            }
                            //mouthScroll.Add(mouthEAR);
                            else
                            {
                                mouthScroll = np.append(mouthScroll, (NDarray)mouthEAR);
                            }
                        }

                        else if (ts.TotalSeconds > 25)
                        {
                            MessageBoxButtons buttons = MessageBoxButtons.OK;
                            DialogResult res = MessageBox.Show("Caliberation Completed", "Calibration", buttons);
                            if (res == DialogResult.OK)
                            {
                                caliberationFinished = true;
                                if (capture != null && capture.IsOpened)
                                    capture.Stop();
                                capture.ImageGrabbed -= streamingForCaliberation;

                            }
                        }
                    }
                }

                //Disposing the frames 
                currentFrame.Dispose();

                pictureBox1.Image = frameToBeShown;
                if (caliberationFinished)
                    pictureBox1.Image = null;
            }
        }


        double lclickMean, rclickMean, mouthScrollMean, lclickAreaMean, rclickAreaMean;
        private void StartBtn_Click(object sender, EventArgs e)
        {
            if (!caliberationFinished)
                MessageBox.Show("Please complete caliberation first", "Caution");
            if (!detectButtonClicked && caliberationFinished)
            {
                detectButtonClicked = true;
                var index = comboBox1.SelectedIndex;
                VideoCapture.API captureApi = VideoCapture.API.DShow;
                captureStrm = new VideoCapture(index, captureApi)
                {
                    FlipHorizontal = true
                };

                //sorting and finding the mean values
                /*lclick.Sort(); rclick.Sort(); mouthScroll.Sort(); lclickArea.Sort(); rclickArea.Sort();
                lclickMean = Utilities.findMedian(lclick) - 1;
                rclickMean = Utilities.findMedian(rclick) + 1;
                mouthScrollMean = Utilities.findMedian(mouthScroll);
                lclickAreaMean = Utilities.findMedian(lclickArea);
                rclickAreaMean = Utilities.findMedian(rclickArea);*/

                Console.WriteLine($"len of lclick: {lclick.shape}");
                Console.WriteLine($"len of rclick: {rclick.shape}");
                Console.WriteLine($"len of lclickArea: {lclickArea.shape}");
                Console.WriteLine($"len of rclickArea: {rclickArea.shape}");

                lclickMean = np.median(np.sort(lclick)) ;
                rclickMean = np.median(np.sort(rclick)) ;
                mouthScrollMean = np.median(np.sort(mouthScroll));
                lclickAreaMean = np.median(np.sort(lclickArea));
                rclickAreaMean = np.median(np.sort(rclickArea));

                Console.WriteLine($"lclickMean: {lclickMean}");
                Console.WriteLine($"rclickMean: {rclickMean}");
                Console.WriteLine($"lclickAreaMean: {lclickAreaMean}");
                Console.WriteLine($"rclickAreaMean: {rclickAreaMean}");

                captureStrm.ImageGrabbed += streaming;
                captureStrm.Start();

                checkFps = true;
            }
        }

        private void streaming(object sender, EventArgs e)
        {
            frameReceived = captureStrm.Retrieve(frameStrm);

            if (checkFps)
            {
                DateTime cuFps = DateTime.Now;
                fps = Utilities.findFps(cuFps, ref prFps, ref avg, ref count);
            }

            if (!frameReceived)
            {
                msg = "Cam not working, please check";
                title = "Error";
                DialogResult result = MessageBox.Show(msg, title);
                if (result == DialogResult.OK)
                {
                    pictureBox1.Image = null;
                    this.Close();
                }
            }
            else
            {
                if (skipFrame % 2 == 0)
                {
                    curBlnk = DateTime.Now;
                    currentFrame = frameStrm.ToImage<Bgr, byte>().Resize(300, 200, Inter.Cubic);
                    frameToBeShown = currentFrame.ToBitmap();

                    newImage = currentFrame.Clone();

                    frameHeight = newImage.Rows;
                    frameWidth = newImage.Cols;

                    using (blob = DnnInvoke.BlobFromImage(newImage, 1.0, size,
                                                             meanForBlob, false, false))
                        _faceNet.SetInput(blob, "data");

                    using (detection = _faceNet.Forward("detection_out"))
                    {
                        rows = detection.SizeOfDimension[2];
                        cols = detection.SizeOfDimension[3];

                        temp = new float[rows * cols];
                        Marshal.Copy(detection.DataPointer, temp, 0, temp.Length);
                    }

                    if (listOfPoints != null)
                        listOfPoints.Clear();
                    for (int i = 0; i < rows; i++)
                    {
                        confidence = temp[i * cols + 2];

                        if (confidence > 0.7)
                        {
                            x1 = (int)(temp[i * cols + 3] * frameWidth);
                            y1 = (int)(temp[i * cols + 4] * frameHeight);
                            x2 = (int)(temp[i * cols + 5] * frameWidth);
                            y2 = (int)(temp[i * cols + 6] * frameHeight);

                            using (dlibimg = frameToBeShown.ToArray2D<RgbPixel>())
                            {
                                //preparing coordinates of face
                                faceCordinates = new DlibDotNet.Rectangle(x1, y1, x2, y2);

                                // find the landmark points of the face
                                using (shape = sp.Detect(dlibimg, faceCordinates))
                                {

                                    // Get the landmark points on the image
                                    for (var j = 0; j < shape.Parts; j++)
                                    {
                                        var point = shape.GetPart((uint)j);
                                        System.Drawing.Point xys = new System.Drawing.Point(point.X, point.Y);
                                        listOfPoints.Add(xys);
                                    }
                                }
                            }
                            Utilities.drawLandmarks(listOfPoints, ref frameToBeShown);

                            rightEyeCoord = listOfPoints.Skip(leftEyeStart).Take(6).ToArray();
                            leftEyeCoord = listOfPoints.Skip(rightEyeStart).Take(6).ToArray();

                            //The Aspect ratio of eyes to know the blinks of eyes 
                            rightEyeEAR = Utilities.eyeAspectRation(rightEyeCoord);
                            leftEyeEAR = Utilities.eyeAspectRation(leftEyeCoord);

                            //The Aspect ratio of mouth to know the laugh to activate scroll
                            mouthCoord = listOfPoints.Skip(48).Take(13).ToArray();
                            mouthEAR = Utilities.mouthAspectRatio(mouthCoord);

                            EARDiff = (leftEyeEAR - rightEyeEAR) * 100;


                            Emgu.CV.Util.VectorOfPoint vre = new Emgu.CV.Util.VectorOfPoint();
                            vre.Push(rightEyeCoord);
                            rightEyeArea = CvInvoke.ContourArea(vre);

                            Emgu.CV.Util.VectorOfPoint vle = new Emgu.CV.Util.VectorOfPoint();
                            vle.Push(leftEyeCoord);
                            leftEyeArea = CvInvoke.ContourArea(vle);


                            if (!blinked)
                            {
                                if (EARDiff < lclickMean && leftEyeArea < lclickAreaMean)
                                {
                                    Console.WriteLine($"Left Click, EARDiff: {EARDiff}, leftEyeArea: {leftEyeArea}");
                                    Clicking.leftClick(new Point(Cursor.Position.X, Cursor.Position.Y));
                                   
                                }
                                else if (EARDiff > rclickMean && rightEyeArea < rclickAreaMean)
                                {
                                    Console.WriteLine($"Right Click, EARDiff: {EARDiff}, rightEyeArea: {rightEyeArea}");
                                    Clicking.rightClick(new Point(Cursor.Position.X, Cursor.Position.Y));
                                    
                                }
                                prBlnk = DateTime.Now;
                                blinked = true;
                            }
                            else if (blinked)
                            {
                                // To have a time span of round 1 second from every click
                                TimeSpan ts = curBlnk.Subtract(prBlnk);
                                if (ts.TotalMilliseconds >= 800)
                                    blinked = false;
                            }

                        }
                    }

                    //Disposing the frames //end of else
                    currentFrame.Dispose();
                    if (checkFps)
                        Utilities.drawFps(fps, ref frameToBeShown);
                    pictureBox1.Image = frameToBeShown;
                }
                skipFrame++;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!formClosed)
            {
                if (checkFps)
                {
                    Console.WriteLine($"AvgFps: {avg / count} Total Frames: {count}");
                }
                if (captureStrm != null && captureStrm.IsOpened)
                    captureStrm.Stop();

                if (capture != null && capture.IsOpened)
                    capture.Stop();

                this.formClosed = true;
                this.Close();
            }
        }


    }
}
