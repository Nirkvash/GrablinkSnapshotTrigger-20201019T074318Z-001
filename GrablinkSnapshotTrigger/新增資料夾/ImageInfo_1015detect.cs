using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace GrablinkSnapshotTrigger
{
    public class SystemMgr
    {
        public bool SaveImages;
        public float PixelSize;
        public string RootPath;
        public int NumSaveImageThread;

        public SystemMgr()
        {
            SaveImages = false;
            PixelSize = 0.0f;
            RootPath = "D:\\";
            NumSaveImageThread = 1;//default 1
        }

        public void GetParam(String pathFile)
        {
            try
            {
                using (StreamReader sr = new StreamReader(pathFile))
                {
                    string text = sr.ReadToEnd();

                    string lineDelimitor = "\n";
                    char[] colDelimitor = { ' ', '\r', ';' };

                    string[] lines = text.Split(new string[] { lineDelimitor }, StringSplitOptions.None);
                    string[][] data = new string[lines.Length][];
                    int index;
                    int i;
                    for (i = 0; i < lines.Length; i++)
                    {
                        data[i] = lines[i].Split(colDelimitor);

                        index = Array.IndexOf(data[i], "SystemMgr");
                        if (index != -1) break;
                    }

                    for (; i < lines.Length; i++)
                    {
                        data[i] = lines[i].Split(colDelimitor);

                        index = Array.IndexOf(data[i], "SaveImages");
                        if (index != -1)
                        {
                            SaveImages = Convert.ToBoolean(data[i][index + 2]);
                            continue;
                        }

                        index = Array.IndexOf(data[i], "RootPath");
                        if (index != -1)
                        {
                            RootPath = Convert.ToString(data[i][index + 2]);
                            continue;
                        }

                        index = Array.IndexOf(data[i], "NumSaveImageThread");
                        if (index != -1)
                        {
                            NumSaveImageThread = Convert.ToInt32(data[i][index + 2]);
                            continue;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                throw e;
            }
        }
    }

    public class ImageInfo
    {
        public Mat SrcImg;
        public int iIndex;
        public int iTopY;
        public DateTime DT;

        public ImageInfo()
        {
            iIndex = -1;
            iTopY = -1;
        }
    }

    public class ImageQueue
    {
        private Queue<ImageInfo> ImgQueue;
        private int iMaxCapacity;

        public ImageQueue()
        {
            ImgQueue = new Queue<ImageInfo>();
            iMaxCapacity = -1;
        }

        public ImageQueue(int iCapacity)
        {
            ImgQueue = new Queue<ImageInfo>();
            iMaxCapacity = iCapacity;
        }

        public void push(ImageInfo Img)
        {
            lock (ImgQueue)
            {
                if (ImgQueue.Count < iMaxCapacity || iMaxCapacity == -1)
                    ImgQueue.Enqueue(Img);
            }
        }

        public ImageInfo pop()
        {
            ImageInfo Img = new ImageInfo();
            lock (ImgQueue)
            {
                if (ImgQueue.Count > 0)
                {
                    Img = ImgQueue.Dequeue();
                }
                else
                {
                    Img.iIndex = -1;
                }
            }

            return Img;
        }

        public void Clear()
        {
            lock (ImgQueue)
            {
                ImgQueue.Clear();
            }
        }

        public int Count
        {
            get
            {
                int nCount;

                lock (ImgQueue)
                {
                    nCount = ImgQueue.Count;
                }
                return nCount;
            }
        }

        public int Capacity
        {
            get
            { return iMaxCapacity; }

            set
            { iMaxCapacity = value; }
        }

        public bool Empty
        {
            get
            {
                if (ImgQueue.Count > 0)
                    return false;
                else
                    return true;
            }
        }
    }
    
    public class SaveImageThread
    {
        int iThreadID;
        public bool bStop;
        Thread thSaveImage;

        ImageQueue IQ;
        SystemMgr SM;
        Stopwatch sw = new Stopwatch();
        public SaveImageThread(int iID, ImageQueue ImgQueue, SystemMgr SysMgr)
        {
            iThreadID = iID;
            bStop = false;
            IQ = ImgQueue;
            SM = SysMgr;
        }

        public void Start()
        {
            thSaveImage = new Thread(SaveImage);
            thSaveImage.Start();
        }

        public void Join()
        {
            thSaveImage.Join();
        }

        private String SaveImages(String Path, ImageInfo ImgInfo)
        {
            DateTime DT = ImgInfo.DT;
            String filename = DT.Year.ToString() + DT.Month.ToString("00") + DT.Day.ToString("00") + "_" + DT.Hour.ToString("00") + DT.Minute.ToString("00") + DT.Second.ToString("00") + "." + DT.Millisecond.ToString("000") + ".bmp";
            ImgInfo.SrcImg.Save(Path + "\\" + filename);

            return filename;
        }

        private void SaveImage()
        {
            while (bStop == false)
            {
                if (bStop == true) break;

                ImageInfo ImgInfo;
                lock(IQ)
                {
                    ImgInfo = IQ.pop();
                }

                if(ImgInfo.iIndex == -1)
                {
                    Thread.Sleep(1);
                    continue;
                }

                try
                {
                    /*
                    unsafe
                    {
                        byte* pImg = (byte*)(void*)ImgInfo.SrcImg.DataPointer + ImgInfo.SrcImg.Width * 3 + 5;//(row 3 & column 5)
                        byte pixelValue = *pImg;
                    }
                    int[,] kernel = { { -1, -1, -1 }, { -1, 8, -1 }, { -1, -1, -1 } };
                    */
                    
                    if (SM.SaveImages == true)
                    {
                        String path = SM.RootPath + "\\";
                        SaveImages(path, ImgInfo);
                    }
                    int M = ImgInfo.SrcImg.Width;
                    int N = ImgInfo.SrcImg.Height;
                    int GRID_SIZE = 32;
                    int GRID_CENTERIZE = (int)(0.5 * GRID_SIZE);
                    //Gray image
                    Image<Bgr, byte> img = ImgInfo.SrcImg.ToImage<Bgr, byte>();
                    Image<Gray, byte> imgGray = new Image<Gray, byte>(M, N);
                    CvInvoke.CvtColor(img, imgGray, ColorConversion.Bgr2Gray);
                    //Gaussian Blur
                    Image<Gray,byte> Gaussianed =new Image<Gray, byte>(M,N);
                    Size kernel = new Size(3, 3);
                    CvInvoke.GaussianBlur(imgGray, Gaussianed, kernel, sigmaX: 0);
                    //Erode
                    int dilatation_size = 3;
                    Mat element = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(2 * dilatation_size + 1, 2 * dilatation_size + 1), new Point(dilatation_size, dilatation_size));
                    Image<Gray,byte> eroded = new Image<Gray, byte>(M,N);
                    CvInvoke.Erode(Gaussianed, eroded, element, new Point(-1, -1), 1, BorderType.Constant, new MCvScalar());
                    String filename = ImgInfo.DT.Year.ToString() + ImgInfo.DT.Month.ToString("00") + ImgInfo.DT.Day.ToString("00") + "_" + ImgInfo.DT.Hour.ToString("00") + ImgInfo.DT.Minute.ToString("00") + ImgInfo.DT.Second.ToString("00");
                    //eroded.Save("D:\\eroded\\"+filename+".png");

                    Mat thersh = new Mat();
                    double ret = CvInvoke.Threshold(Gaussianed, thersh, 0, 255, ThresholdType.Otsu);
                    Gaussianed.Dispose();
                    thersh.Dispose();
                    CvInvoke.Threshold(eroded, eroded, ret, 255, ThresholdType.ToZero);
                    //Image<Gray, byte> mask = eroded.Clone();
                    //mask.Save("D:\\mask\\" + filename + ".png");

                    //laplace transform
                    Matrix<float> l_data =new Matrix<float>(new float[,] { { 1f,1f,1f}, {1f,-8f,1f }, {1f,1f,1f } });
                    Image<Gray,byte> lap = eroded.Clone();
                    CvInvoke.Filter2D(lap, lap, l_data, new Point(-1, -1), 0);
                    
                    //lap.Save("D:\\laplacian\\" + filename + ".png");
                    
                    CvInvoke.Dilate(lap, lap, element, new Point(-1, -1), 1, BorderType.Constant, new MCvScalar());
                    //lap.Save("D:\\dilated\\" + filename + ".png");

                    VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                    Mat hierarchy = new Mat();
                    CvInvoke.FindContours(lap, contours, hierarchy, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);
                    int count = contours.Size;
                    int i,j;
                    List<Double> contour_area_list = new List<double>();
                    for (i = 0; i < count; i++)
                    {
                        double area = CvInvoke.ContourArea(contours[i]);
                        contour_area_list.Add(area);
                    }
                    Double max_area = 0;
                    int max_index = 0;
                    count = contour_area_list.Count;
                    for (i = 0; i < count; i++)
                    {
                        if (contour_area_list[i] > max_area)
                        {
                            max_area = contour_area_list[i];
                            max_index = i;
                        }
                    }
                    Console.WriteLine("max contour: "+max_index+" area = "+ max_area);
                    Image<Bgr, byte> imgcontour = img.Clone();
                    CvInvoke.DrawContours(imgcontour, contours, max_index, new MCvScalar(0, 255, 0),2);
                    //imgcontour.Save("D:\\contour\\" + filename + ".png");
                    imgcontour.Dispose();
                    
                    Mat black = imgGray.Mat.Clone();//9
                    black.SetTo(new MCvScalar(0));
                    CvInvoke.DrawContours(black, contours, max_index, new MCvScalar(255), -1);
                    Image<Gray, byte> cimg = black.ToImage<Gray, byte>();
                    black.Dispose();
                    Image<Gray, float> grad_x = new Image<Gray, float>(M, N);
                    Image<Gray, float> grad_y = new Image<Gray, float>(M, N);
                    CvInvoke.Scharr(eroded, grad_x, DepthType.Cv32F, 1, 0);
                    CvInvoke.Scharr(eroded, grad_y, DepthType.Cv32F, 0, 1);
                    for (i = 0; i < N; i++)
                    {
                        for (j = 0; j < M; j++)
                        {
                            grad_x.Data[i, j, 0] = grad_x.Data[i, j, 0] * grad_x.Data[i, j, 0] + grad_y.Data[i, j, 0] * grad_y.Data[i, j, 0];
                        }
                    }
                    grad_y.Dispose();
                    CvInvoke.Sqrt(grad_x, grad_x);
                    //grad_x below is gradient from this line
                    float max = 0, min = float.MaxValue;
                    for (i = 0; i < N; i++)
                    {
                        for (j = 0; j < M; j++)
                        {
                            if (grad_x.Data[i, j, 0] > max)
                            {
                                max = grad_x.Data[i, j, 0];
                            }
                            if (grad_x.Data[i, j, 0] < min)
                            {
                                min = grad_x.Data[i, j, 0];
                            }
                        }
                    }
                    Console.WriteLine("Grad max value: " + max + ", min value: " + min);
                    for (i = 0; i < N; i++)
                    {
                        for (j = 0; j < M; j++)
                        {
                            grad_x.Data[i, j, 0] = 255 * (grad_x.Data[i, j, 0] - min) / (max - min);
                        }
                    }
                    //Image<Gray, byte> byteconturs = grad_x.Convert<Gray, byte>();
                    //byteconturs.Save("D:\\gradient\\" + filename + ".png");
                    //byteconturs.Dispose();
                    //Console.WriteLine("gradient visualization OK");
                    Image<Bgr, byte> img_grid = img.Clone();//23
                    int block_count = 0;
                    byte ret_byte = (byte)ret;
                    byte p1 = (byte)(ret+10);
                    byte p2 = 30;
                    byte p3 = 200;
                    byte p4 = 40;

                    Console.WriteLine("RET: " + ret);
                    int blockm = 0, blockn = 0;
                    byte blockmax = Byte.MinValue, blockmin = Byte.MaxValue, blockmed;
                    float blockGraMax = byte.MinValue;

                    for (i = 0; i < N; i = i + GRID_SIZE)
                    {
                        for (j = 0; j < M; j = j + GRID_SIZE)
                        {
                            if ((j + GRID_SIZE < M)
                                && (i + GRID_SIZE < N))
                            {
                                if ((cimg.Data[i, j, 0] == 255)
                                    && (cimg.Data[i, j + GRID_SIZE, 0] == 255)
                                    && (cimg.Data[i + GRID_SIZE, j, 0] == 255)
                                    && (cimg.Data[i + GRID_SIZE, j + GRID_SIZE, 0] == 255)
                                    )
                                {

                                    CvInvoke.Circle(img_grid, new Point(j, i), 3, new MCvScalar(0, 255, 0));
                                    block_count = block_count + 1;
                                    blockmax = Byte.MinValue;
                                    blockmin = Byte.MaxValue;
                                    blockmed = 0;
                                    blockGraMax = float.MinValue;
                                    List<byte> median = new List<byte>(25);

                                    for (blockm = i; blockm < i + GRID_SIZE; blockm++)
                                    {
                                        for (blockn = j; blockn < j + GRID_SIZE; blockn++)
                                        {

                                            if (imgGray.Data[blockm, blockn, 0] > blockmax)
                                            {
                                                blockmax = imgGray.Data[blockm, blockn, 0];
                                            }
                                            if (imgGray.Data[blockm, blockn, 0] < blockmin)
                                            {
                                                blockmin = imgGray.Data[blockm, blockn, 0];
                                            }

                                            median.Add(imgGray.Data[blockm, blockn, 0]);
                                            
                                            if (grad_x.Data[blockm, blockn, 0] > blockGraMax)
                                            {
                                                blockGraMax = grad_x.Data[blockm, blockn, 0];
                                            }

                                        }

                                    }
                                    median.Sort();
                                    blockmed = median[12];
                                    /*if (blockGraMax > 170)
                                        Console.WriteLine("blockgramax: " + blockGraMax);
                                    */
                                    //Console.WriteLine("blockmed: " + blockmed + ", blockGraMax: " + blockGraMax);
                                    //Console.WriteLine("max: " + blockmax + ",med: " + blockmed+",min: "+blockmin);
                                    if (blockmed > ret_byte && (blockGraMax > 170 || ((blockmax - blockmin) > p2 && blockmax > p3) ||
                                        ((blockmax - blockmin) > p4 && blockmin > p1)))
                                    {

                                        CvInvoke.Circle(img_grid, new Point(j + GRID_CENTERIZE, i + GRID_CENTERIZE), 3, new MCvScalar(0, 0, 255), -1);
                                    }
                                }
                            }
                        }
                    }
                    Console.WriteLine("# of blocks :" + block_count);
                    img_grid.Save("D:\\grid\\" + filename + ".png");
                    grad_x.Dispose();
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }
    }
}
