using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;

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
            NumSaveImageThread = 1;
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

                try// algorithm is put here,but this program can't run without camera
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
                }
                catch(Exception e)
                {
                    throw e;
                }
            }
        }
    }
}
