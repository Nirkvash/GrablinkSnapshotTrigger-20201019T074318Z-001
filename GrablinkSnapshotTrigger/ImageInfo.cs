using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        public void Push(ImageInfo Img)
        {
            lock (ImgQueue)
            {
                if (ImgQueue.Count < iMaxCapacity || iMaxCapacity == -1)
                    ImgQueue.Enqueue(Img);
            }
        }

        public ImageInfo Pop()
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

    public class ImageList
    {
        private List<ImageInfo> ImgList;
        private int iMaxCapacity;

        public ImageList()
        {
            ImgList = new List<ImageInfo>();
            iMaxCapacity = -1;
        }
        public ImageList(int iCapacity)
        {
            ImgList = new List<ImageInfo>();
            iMaxCapacity = iCapacity;
        }

        public void Add(ImageInfo Img)
        {
            lock (ImgList)
            {
                if (ImgList.Count < iMaxCapacity || iMaxCapacity == -1)
                {
                    ImgList.Add(Img);
                }
            }
        }
        public void Sort()
        {
            ImgList.Sort(ImageCompare);

        }
        private int ImageCompare(ImageInfo a, ImageInfo b)
        {
            if (a.iIndex == -1)
            {
                if (b.iIndex == -1)
                    return 0;
                else
                    return -1;
            }
            else
            {
                if (b.iIndex == -1)
                    return 1;
                else
                {
                    if (a.iIndex > b.iIndex)
                        return 1;
                    else
                    {
                        return -1;
                    }
                }
            }
        }
        public int Count
        {
            get
            {
                int ncount;
                lock (ImgList)
                {
                    ncount = ImgList.Count;
                }
                return ncount;
            }
        }
        public ImageInfo index(int i)
        {
                ImageInfo tmp = new ImageInfo();
                tmp = ImgList[i];
                return tmp;
        }
        public void Clear()
        {
            ImgList.Clear();
        }
        
    }
    public class SaveImageThread
    {
        int iThreadID;
        public bool bStop;
        Thread thSaveImage;

        bool[] IQE;
        ImageQueue IQ;
        ImageList IL;
        SystemMgr SM;
        Stopwatch sw = new Stopwatch();
        public SaveImageThread(int iID, ImageQueue ImgQueue, SystemMgr SysMgr,ImageList ImageList,bool[] IQEmpty)
        {
            iThreadID = iID;
            bStop = false;
            IQ = ImgQueue;
            IL = ImageList;
            SM = SysMgr;
            IQE = IQEmpty;
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

        static IEnumerable<int> SteppedIterator(int startIndex, int endEndex, int stepSize)
        {
            for (int i = startIndex; i < endEndex; i += stepSize)
            {
                yield return i;
            }
        }
        private void SaveImage()
        {
            while (bStop == false)
            {
                if (bStop == true) break;

                ImageInfo ImgInfo;
                int i;
                lock (IQ)
                {
                    ImgInfo = IQ.Pop();
                }

                if (ImgInfo.iIndex == -1)
                {
                    lock (IQE)
                    {
                        IQE[iThreadID] = true;
                        bool ILSort = true;
                        for (i = 0; i < iThreadID; i++)
                        {
                            ILSort = ILSort & IQE[i];
                        }
                        if (ILSort && IL.Count != 0)
                        {
                            IL.Sort();
                            Mat completeDetection = new Mat();
                            for (i = 0; i < IL.Count; i++)
                            {
                                completeDetection += IL.index(i).SrcImg;
                            }
                            completeDetection.Save("D:/grid/" + IL.index(0).DT.ToString() + ".bmp");
                        }
                        else
                        {
                            Thread.Sleep(1);
                        }
                    }
                    continue;
                }
                IQE[iThreadID] = false;
                Console.WriteLine("iID:" +iThreadID);
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
                    
                    String filename = ImgInfo.DT.Year.ToString() + ImgInfo.DT.Month.ToString("00") + ImgInfo.DT.Day.ToString("00") + "_" + ImgInfo.DT.Hour.ToString("00") + ImgInfo.DT.Minute.ToString("00") + ImgInfo.DT.Second.ToString("00") + "." + ImgInfo.DT.Millisecond.ToString("000");
                    int width = ImgInfo.SrcImg.Width;
                    int height = ImgInfo.SrcImg.Height;
                    int GRID_SIZE = 32;
                    int GRID_CENTERIZE = (int)(0.5 * GRID_SIZE);
                    int dilatation_size = 3;
                    
                    double ret;
                    using (Image<Bgr, byte> img = ImgInfo.SrcImg.ToImage<Bgr, byte>())
                    {
                        using (Image<Gray, byte> imgGray = new Image<Gray, byte>(width, height))//2
                        {
                            CvInvoke.CvtColor(img, imgGray, ColorConversion.Bgr2Gray);
                            //Console.WriteLine("Gray done");
                            
                            using (Image<Gray, byte> Gaussianed = new Image<Gray, byte>(width, height))
                            {
                                CvInvoke.GaussianBlur(imgGray, Gaussianed, new Size(3, 3), sigmaX: 0);
                                using (Mat thersh = new Mat())
                                {
                                    ret = CvInvoke.Threshold(Gaussianed, thersh, 0, 255, ThresholdType.Otsu);
                                    if (ret < 70)
                                        continue;
                                }
                                if (SM.SaveImages == true)
                                {
                                    String path = SM.RootPath + "\\";
                                    SaveImages(path, ImgInfo);
                                }
                                //Console.WriteLine(iThreadID+",RET: " + ret);
                                using (Mat element = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(2 * dilatation_size + 1, 2 * dilatation_size + 1), new Point(dilatation_size, dilatation_size)))
                                {
                                    CvInvoke.Erode(Gaussianed, Gaussianed, element, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
                                    CvInvoke.Threshold(Gaussianed, Gaussianed, ret-1, 255, ThresholdType.ToZero);

                                    using (Image<Gray, byte> lap = new Image<Gray, byte>(width, height))
                                    {
                                        Matrix<float> l_data = new Matrix<float>(new float[,] { { 1, 1, 1 }, { 1, -8, 1 }, { 1, 1, 1 } });
                                        CvInvoke.Filter2D(Gaussianed, lap, l_data, new Point(-1, -1), 0);
                                        l_data.Dispose();
                                        CvInvoke.Dilate(lap, lap, element, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
                                        using (Mat black = new Mat(new Size(width, height), DepthType.Cv8U, 1))
                                        {
                                            black.SetTo(new MCvScalar(0));
                                            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                                            {
                                                using (Mat hierarchy = new Mat())
                                                {
                                                    CvInvoke.FindContours(lap, contours, hierarchy, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);
                                                    int count = contours.Size;
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

                                                    //Console.WriteLine("max contour: " + max_index + " area = " + max_area);
                                                    //Console.WriteLine("# of contours: " + contour_area_list.Count);
                                                    contour_area_list = null;
                                                    CvInvoke.DrawContours(black, contours, max_index, new MCvScalar(255), -1);
                                                    
                                                }
                                                using (Image<Gray, byte> cimg = black.ToImage<Gray, byte>())
                                                {
                                                    List<VectorOfPoint> holes = new List<VectorOfPoint>();
                                                    List<VectorOfPoint> defects = new List<VectorOfPoint>();
                                                    using (Image<Gray, byte> edges = new Image<Gray, byte>(width, height))
                                                    {
                                                        CvInvoke.Canny(Gaussianed, edges, 70, 200);
                                                        using (VectorOfVectorOfPoint contours_d = new VectorOfVectorOfPoint())
                                                        {
                                                            using (Mat hierarchy_d = new Mat())
                                                            {
                                                                
                                                                CvInvoke.FindContours(edges, contours_d, hierarchy_d, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);
                                                                Moments[] moments = new Moments[contours_d.Size];
                                                                for (i = 0; i < contours_d.Size; i++)
                                                                {
                                                                    moments[i] = CvInvoke.Moments(contours_d[i]);
                                                                    if (CvInvoke.ContourArea(contours_d[i]) == 0.0)
                                                                        continue;
                                                                    int cx = (int)(moments[i].M10 / moments[i].M00);
                                                                    int cy = (int)(moments[i].M01 / moments[i].M00);
                                                                    if (cimg.Data[cy,cx,0]==255)
                                                                    {
                                                                        if(CvInvoke.ContourArea(contours_d[i]) > 1000)
                                                                        {
                                                                            holes.Add(contours_d[i]);
                                                                        }
                                                                        else if(CvInvoke.ContourArea(contours_d[i]) > 10)
                                                                        {
                                                                            defects.Add(contours_d[i]);
                                                                        }
                                                                    }
                                                                }
                                                                Rectangle[] RectofHoles =new Rectangle[holes.Count];
                                                                int countHoles = holes.Count;
                                                                for (i=0;i<countHoles;i++)
                                                                {
                                                                    RectofHoles[i] = CvInvoke.BoundingRectangle(holes[i]);
                                                                    CvInvoke.Rectangle(cimg,new Rectangle(RectofHoles[i].X-GRID_CENTERIZE,RectofHoles[i].Y - GRID_CENTERIZE,
                                                                        RectofHoles[i].Width+GRID_CENTERIZE, RectofHoles[i].Height+GRID_CENTERIZE),new MCvScalar(0),-1);
                                                                    
                                                                }
                                                                cimg.Save("D:\\WOH\\" + filename + ".bmp");
                                                            }
                                                        }
                                                    }
                                                    using (Image<Gray, float> grad_x = new Image<Gray, float>(width, height))
                                                    {
                                                        using (Image<Gray, float> grad_y = new Image<Gray, float>(width, height))
                                                        {
                                                            Parallel.Invoke(() =>
                                                            {
                                                                CvInvoke.Scharr(Gaussianed, grad_x, DepthType.Cv32F, 1, 0);
                                                                CvInvoke.Pow(grad_x, 2, grad_x);
                                                            },
                                                            () =>
                                                            {
                                                                CvInvoke.Scharr(Gaussianed, grad_y, DepthType.Cv32F, 0, 1);
                                                                CvInvoke.Pow(grad_y, 2, grad_y);
                                                            }
                                                            );

                                                            CvInvoke.Add(grad_x, grad_y, grad_x);
                                                        }
                                                        CvInvoke.Sqrt(grad_x, grad_x);
                                                        double max = 0, min = 0;
                                                        Point maxloc = new Point();
                                                        Point minloc = new Point();
                                                        CvInvoke.MinMaxLoc(grad_x, ref min, ref max, ref minloc, ref maxloc);
                                                        float tmp = 255 / (float)(max - min);
                                                        Parallel.For(0, height, h =>
                                                        {
                                                              for(int w=0;w<width;w++)
                                                              {
                                                                  grad_x.Data[h, w, 0] = tmp * (grad_x.Data[h, w, 0] - (float)min);
                                                              }
                                                        });

                                                        using (Image<Bgr, byte> img_grid = img.Clone())
                                                        {
                                                            
                                                            byte ret_byte = (byte)ret;
                                                            byte minimumHold = (byte)(ret + 10);
                                                            byte maximumHold = 200;
                                                            byte minimumDiff = 30;
                                                            byte maximumDiff = 30;
                                                            
                                                            try
                                                            {
                                                                Parallel.ForEach(SteppedIterator(0, height, GRID_SIZE), index_i =>
                                                                {
                                                                    Parallel.ForEach(SteppedIterator(0, width, GRID_SIZE), index_j =>
                                                                    {
                                                                        if ((cimg.Data[index_i, index_j, 0] == 255)
                                                                                    && (cimg.Data[index_i, Math.Min(index_j + GRID_SIZE - 1, width - 1), 0] == 255)
                                                                                    && (cimg.Data[Math.Min(index_i + GRID_SIZE - 1, height - 1), index_j, 0] == 255)
                                                                                    && (cimg.Data[Math.Min(index_i + GRID_SIZE - 1, height - 1), Math.Min(index_j + GRID_SIZE - 1, width - 1), 0] == 255)
                                                                                    )
                                                                        {
                                                                            CvInvoke.Circle(img_grid, new Point(index_j, index_i), 3, new MCvScalar(0, 255, 0));
                                                                            byte blockmax = Byte.MinValue, blockmin = Byte.MaxValue, blockmed = 0;
                                                                            float blockGraMax = float.MinValue;
                                                                            List<byte> temp = new List<byte>();
                                                                            for (int blockm = index_i; blockm < Math.Min(index_i + GRID_SIZE, height); blockm++)
                                                                            {
                                                                                for (int blockn = index_j; blockn < Math.Min(index_j + GRID_SIZE, width); blockn++)
                                                                                {
                                                                                    temp.Add(imgGray.Data[blockm, blockn, 0]);
                                                                                    if (imgGray.Data[blockm, blockn, 0] > blockmax)
                                                                                    {
                                                                                        blockmax = imgGray.Data[blockm, blockn, 0];
                                                                                    }
                                                                                    if (imgGray.Data[blockm, blockn, 0] < blockmin)
                                                                                    {
                                                                                        blockmin = imgGray.Data[blockm, blockn, 0];
                                                                                    }
                                                                                    if (grad_x.Data[blockm, blockn, 0] > blockGraMax)
                                                                                    {
                                                                                        blockGraMax = grad_x.Data[blockm, blockn, 0];
                                                                                    }
                                                                                }
                                                                            }
                                                                            temp.Sort();
                                                                            blockmed = temp[temp.Count / 2];
                                                                            temp.Clear();

                                                                            if (blockmed > ret_byte && (blockGraMax > 170 || ((blockmax - blockmin) > maximumDiff && blockmax > maximumHold) ||
                                                                                    ((blockmax - blockmin) > minimumDiff && blockmin > minimumHold)))
                                                                            {
                                                                                CvInvoke.Circle(img_grid, new Point(index_j + GRID_CENTERIZE, index_i + GRID_CENTERIZE), 3, new MCvScalar(0, 0, 255), -1);
                                                                            }
                                                                        }

                                                                    });
                                                                });
                                                            }
                                                            catch (Exception)
                                                            {
                                                                throw;
                                                            }
                                                            ImageInfo detection = new ImageInfo();
                                                            detection.SrcImg = img_grid.Mat;
                                                            detection.iIndex = ImgInfo.iIndex;
                                                            detection.iTopY = ImgInfo.iTopY;
                                                            detection.DT = DateTime.Now;
                                                            lock(IL)
                                                            {
                                                                IL.Add(detection);
                                                            }
                                                        }
                                                    }
                                                }

                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }
    }
}
