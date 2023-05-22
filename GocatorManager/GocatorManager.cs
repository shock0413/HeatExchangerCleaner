using Lmi3d.GoSdk;
using Lmi3d.GoSdk.Messages;
using Lmi3d.GoSdk.Tools;
using Lmi3d.Zen;
using Lmi3d.Zen.Io;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GocatorLib
{
    public class GocatorManager
    {
        private Utill.IniFile iniConfig = new Utill.IniFile(AppDomain.CurrentDomain.BaseDirectory + "\\Config.ini");

        private string sensorIP;
        public string SensorIP { get { return sensorIP; } }
        GoSystem system;
        GoSensor sensor;

        bool isScanning = false;

        private BitmapSource captureImage = null;
        public BitmapSource CaptureImage { get { return captureImage; } }

        public delegate void OnCapturedEventHandler(short[] data, long width, long length, double xResolution, double yResolution, double zResolution);
        public event OnCapturedEventHandler OnCaptureEvent = null;

        public delegate void OnCapturedTickEventHandler(long maxEncoderValue);
        public event OnCapturedTickEventHandler OnCaptureTickEvent = delegate { };

        public delegate void OnCapturedIntensityEventHandler(byte[] datas, int width, int length);
        public event OnCapturedIntensityEventHandler OnCapturedIntensityEvent = delegate { };


        public int StitchSurfaceCount { get; set; }
        public List<int> StitchSurfaceXOffset = new List<int>();
        public int ScanPixedLength { get; set; }
        public int EncoderLostRange { get; set; }

        public GocatorManager(string ip)
        {
            this.sensorIP = ip;

            InitSensor();
        }

        private void InitSensor()
        {
            KApiLib.Construct();
            GoSdkLib.Construct();
        }

        public bool Connect()
        {
            try
            {
                system = new GoSystem();
                KIpAddress ipAddress = KIpAddress.Parse(SensorIP);
                sensor = system.FindSensorByIpAddress(ipAddress);
                sensor.Connect();

                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }

        public string GetGocatorState()
        {

            if (sensor != null)
            {
                return sensor.State.ToString();
            }
            else
            {
                return "Connection Error";
            }


        }

        public GocatorScanStateConstants StartScan(long timeOut)
        {
            Stopwatch sw1 = new Stopwatch();
            sw1.Start();

            try
            {
                Destroy();
            }
            catch
            {

            }

            Thread.Sleep(500);

            try
            {
                Connect();
            }
            catch
            {

            }

            if (system == null || sensor == null)
            {
                Connect();
            }

            while (sw1.ElapsedMilliseconds < 5000)
            {
                if (sensor.State == GoState.Ready || sensor.State == GoState.Running || sensor.State == GoState.Busy)
                {
                    break;
                }
            }

            if (sensor.State != GoState.Ready && sensor.State != GoState.Running)
            {
                try
                {
                    Destroy();
                }
                catch
                {

                }

                try
                {
                    Connect();
                }
                catch
                {

                }
            }

            if (sensor.State == GoState.Running)
            {
                sensor.Stop();
            }

            //sensor.ClearReplayData();
            sensor.Flush();

            //sensor.RecordingEnabled = false;

            while (sw1.ElapsedMilliseconds < 5000)
            {
                if (sensor.State == GoState.Ready)
                {
                    break;
                }
            }

            try
            {
                int exposure = iniConfig.GetInt32("Gocator Setting", "Exposure", 650);
                string trigger = iniConfig.GetString("Gocator Setting", "Trigger", "Encoder");

                GoSetup setup = sensor.Setup;
                setup.ScanMode = GoMode.Surface;

                if (trigger.ToUpper() == "TIME")
                {
                    setup.TriggerSource = GoTrigger.Time;
                }
                else
                {
                    setup.TriggerSource = GoTrigger.Encoder;
                }
                GoSurfaceGeneration goSurfaceGeneration = setup.GetSurfaceGeneration();
                goSurfaceGeneration.FixedLengthLength = ScanPixedLength - EncoderLostRange;
                setup.SetExposure(GoRole.Main, exposure);
            }
            catch
            {

            }

            //setup.ScanMode = GoMode.Surface;
            system.EnableData(true);

            sensor.Refresh();
      

            Type ty = sensor.Tools.GetTool(0).GetType();
            GoExtTool stitchTool = sensor.Tools.GetTool(0) as GoExtTool;
 

            if (stitchTool != null)
            {
                GoExtParamBool param1 = stitchTool.GetParameter(7) as GoExtParamBool;
                GoExtParamBool param2 = stitchTool.GetParameter(16) as GoExtParamBool;
                GoExtParamBool param3 = stitchTool.GetParameter(25) as GoExtParamBool;
                param3.Value = false;

                GoExtParamInt param = stitchTool.GetParameter(0) as GoExtParamInt;
                if (StitchSurfaceCount > 0)
                {
                    param.Value = StitchSurfaceCount - 1;
                }
                for(int i = 0; i < 24; i++)
                {
                    GoExtParamBool eachSurfaceIsUseParam = stitchTool.GetParameter(7 + 9 * i) as GoExtParamBool;
                    if(StitchSurfaceCount > i)
                    {
                        eachSurfaceIsUseParam.Value = true;
                        GoExtParamFloat eachSurfaceoffset = stitchTool.GetParameter(9 + 9 * i) as GoExtParamFloat;
                        eachSurfaceoffset.Value = StitchSurfaceXOffset[i];
                    }
                    else
                    {
                        eachSurfaceIsUseParam.Value = false;
                    }
                }

                param = stitchTool.GetParameter(0) as GoExtParamInt;
                
            }
            GoMeasurement mesur = stitchTool.GetMeasurement(0);

            isScanning = true;
            system.Start();

            sw1.Restart();

            while (sw1.ElapsedMilliseconds < 5000)
            {
                if (sensor.State == GoState.Running)
                {
                    break;
                }
            }

            try
            {
                new Thread(new ThreadStart(() =>
                {
                    long maxEncoderValue = 0;

                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    if (sensor != null)
                    {
                        GoTools collection_tools;
                        collection_tools = sensor.Tools;

                        while (isScanning)
                        {
                            GoDataSet dataSet = new GoDataSet();

                            bool isSuccess = false;
                            try
                            {
                                dataSet = system.ReceiveData(timeOut);
                                isSuccess = true;
                            }
                            catch (Exception ex)
                            {

                            }

                            if (isSuccess)
                            {
                                SurfaceResult.DataContext context = new SurfaceResult.DataContext();
                                for (UInt32 i = 0; i < dataSet.Count; i++)
                                {
                                    GoDataMsg dataObj = (GoDataMsg)dataSet.Get(i);
                                    Console.WriteLine("Data obj : " + dataObj.MessageType);
                                    switch (dataObj.MessageType)
                                    {
                                        case GoDataMessageType.Stamp:
                                            {
                                                GoStampMsg stampMsg = (GoStampMsg)dataObj;
                                                for (UInt32 j = 0; j < stampMsg.Count; j++)
                                                {
                                                    GoStamp stamp = stampMsg.Get(j);
                                                    
                                                    if (stamp.Encoder > maxEncoderValue)
                                                    {
                                                        maxEncoderValue = stamp.Encoder;
                                                    }
                                                }

                                                GoReplay replay = sensor.Replay;

                                            }
                                            break;
                                        case GoDataMessageType.UniformSurface:
                                            {
                                                GoUniformSurfaceMsg surfaceMsg = (GoUniformSurfaceMsg)dataObj;
                                                Console.WriteLine("Type: " + surfaceMsg.MessageType);
                                                ;
                                                long width = surfaceMsg.Width;
                                                long length = surfaceMsg.Length;
                                                long bufferSize = width * length;
                                                IntPtr bufferPointer = surfaceMsg.Data;

                                                Console.WriteLine("Uniform Surface received:");
                                                Console.WriteLine(" Buffer width: {0}", width);
                                                Console.WriteLine(" Buffer length: {0}", length);

                                                double xResolution = surfaceMsg.XResolution / 1000000.0;
                                                double yResolution = surfaceMsg.YResolution / 1000000.0;
                                                double zResolution = surfaceMsg.ZResolution / 1000000.0;

                                                short[] ranges = new short[bufferSize];
                                                Marshal.Copy(bufferPointer, ranges, 0, ranges.Length);
                                                OnCaptureEvent(ranges, width, length, xResolution, yResolution, zResolution);

                                                

                                                //if (width > 1000 && width < 3000)
                                                //// if (width > 200)
                                                //{
                                                //    OnCaptureTickEvent(maxEncoderValue);
                                                //    maxEncoderValue = 0;
                                                //}

                                                //if (width > 1000)
                                                //// if (width > 200)
                                                //{
                                                //    Console.WriteLine(" 영상 획득");

                                                //    short[] ranges = new short[bufferSize];
                                                //    Marshal.Copy(bufferPointer, ranges, 0, ranges.Length);

                                                //    double minZ = short.MaxValue;
                                                //    double maxZ = short.MinValue;

                                                //    try
                                                //    {
                                                //        StringBuilder sb = new StringBuilder();
                                                //        for (int k = 0; k < length; k++)
                                                //        {
                                                //            for (int j = 0; j < width; j++)
                                                //            {
                                                //                short z = ranges[k * width + j];
                                                //                sb.Append(ranges[k * width + j]);
                                                //                sb.Append(",");

                                                //                if (z != -32768)
                                                //                {
                                                //                    if (minZ > z)
                                                //                    {
                                                //                        minZ = z;
                                                //                    }
                                                //                    if (maxZ < z)
                                                //                    {
                                                //                        maxZ = z;
                                                //                    }
                                                //                }
                                                //            }
                                                //            sb.Append("\n");
                                                //        }

                                                //        //new Thread(new ThreadStart(() =>
                                                //        //{
                                                //        //    try
                                                //        //    {
                                                //        //        //File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + "export.csv", sb.ToString());
                                                //        //    }
                                                //        //    catch (Exception e)
                                                //        //    {
                                                //        //        Console.WriteLine("고게이터 데이터출력 실패 " + e.Message);
                                                //        //    }
                                                //        //})).Start();


                                                //        PixelFormat pf = PixelFormats.Gray8;
                                                //        long rawStride = (width * pf.BitsPerPixel + 7) / 8;
                                                //        byte[] rawImage = new byte[rawStride * length];

                                                //        byte[] greyRaw = new byte[rawImage.Length];

                                                //        double distanceZ = maxZ - minZ;

                                                //        for (int k = 0; k < ranges.Length; k++)
                                                //        {
                                                //            if (ranges[k] == -32768)
                                                //            {
                                                //                greyRaw[k] = 0;
                                                //            }
                                                //            else
                                                //            {

                                                //                double z = ((double)(ranges[k] / distanceZ * 255));
                                                //                byte[] byteValue = BitConverter.GetBytes(((int)z));
                                                //                greyRaw[k] = byteValue[0];
                                                //            }
                                                //        }

                                                //        BitmapSource bitmap = BitmapSource.Create((int)width, (int)length,
                                                //            96, 96, pf, null,
                                                //            greyRaw, (int)rawStride);

                                                //        bitmap.Freeze();

                                                //        /*
                                                //        using (FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "새 폴더\\" + Math.Abs(DateTime.Now.ToBinary()) + ".jpg", FileMode.Create))
                                                //        {
                                                //            BitmapEncoder encoder = new JpegBitmapEncoder();
                                                //            encoder.Frames.Add(BitmapFrame.Create(bitmap));
                                                //            encoder.Save(fs);
                                                //        }
                                                //        */

                                                //        Console.WriteLine(" 영상 완료");

                                                //        try
                                                //        {
                                                //            system.Stop();
                                                //        }
                                                //        catch (Exception ex)
                                                //        {
                                                //            Console.WriteLine("고게이터 종료시 에러 발생" + ex.Message);
                                                //        }
                                                //        //Destroy();

                                                //       
                                                //    }
                                                //    catch (Exception ex)
                                                //    {

                                                //    }
                                                //}

                                            }
                                            break;
                                        case GoDataMessageType.Measurement:
                                            {
                                                GoMeasurementMsg measurementMsg = (GoMeasurementMsg)dataObj;
                                                for (UInt32 k = 0; k < measurementMsg.Count; ++k)
                                                {
                                                    GoMeasurementData measurementData = measurementMsg.Get(k);

                                                    Console.WriteLine("ID: {0}", measurementMsg.Id);

                                                    //1. Retrieve Id
                                                    uint id = measurementMsg.Id;
                                                    //2. Retrieve the measurement from the set of tools using measurement ID
                                                    GoMeasurement measurement = collection_tools.FindMeasurementById(id);
                                                    //3. Print the measurement name 
                                                    Console.WriteLine("Measurement Name is : {0}", measurement.Name);
                                                    Console.WriteLine("Value: {0}", measurementData.Value);
                                                    Console.WriteLine("Decision: {0}", measurementData.Decision);
                                                }
                                            }
                                            break;
                                        case GoDataMessageType.SurfacePointCloud:
                                            {
                                                GoSurfacePointCloudMsg surfaceMsg = (GoSurfacePointCloudMsg)dataObj;
                                                context.xResolution = (double)surfaceMsg.XResolution / 1000000;
                                                context.yResolution = (double)surfaceMsg.YResolution / 1000000;
                                                context.zResolution = (double)surfaceMsg.ZResolution / 1000000;
                                                context.xOffset = (double)surfaceMsg.XOffset / 1000;
                                                context.yOffset = (double)surfaceMsg.YOffset / 1000;
                                                context.zOffset = (double)surfaceMsg.ZOffset / 1000;
                                                long surfacePointCount = surfaceMsg.Width * surfaceMsg.Length;
                                                Console.WriteLine("Surface Point Cloud received:");
                                                Console.WriteLine(" Buffer width: {0}", surfaceMsg.Width);
                                                Console.WriteLine(" Buffer length: {0}", surfaceMsg.Length);
                                                //GoPoint[] points = new GoPoint[surfacePointCount];
                                                //SurfaceResult.SurfacePoint[] surfaceBuffer = new SurfaceResult.SurfacePoint[surfacePointCount];
                                                //int structSize = Marshal.SizeOf(typeof(GoPoint));
                                                //IntPtr pointsPtr = surfaceMsg.Data;
                                                //for (UInt32 array = 0; array < surfacePointCount; ++array)
                                                //{
                                                //    IntPtr incPtr = new IntPtr(pointsPtr.ToInt64() + array * structSize);
                                                //    points[array] = (GoPoint)Marshal.PtrToStructure(incPtr, typeof(GoPoint));
                                                //}
                                                //for (UInt32 arrayIndex = 0; arrayIndex < surfacePointCount; ++arrayIndex)
                                                //{
                                                //    if (points[arrayIndex].x != -32768)
                                                //    {
                                                //        surfaceBuffer[arrayIndex].x = context.xOffset + context.xResolution * points[arrayIndex].x;
                                                //        surfaceBuffer[arrayIndex].y = context.yOffset + context.yResolution * points[arrayIndex].y;
                                                //        surfaceBuffer[arrayIndex].z = context.zOffset + context.zResolution * points[arrayIndex].z;
                                                //    }
                                                //    else
                                                //    {
                                                //        surfaceBuffer[arrayIndex].x = -32768;
                                                //        surfaceBuffer[arrayIndex].y = -32768;
                                                //        surfaceBuffer[arrayIndex].z = -32768;
                                                //    }
                                                //}
                                                //byte[] arr = (byte[])surfaceBuffer.Cast<byte>();

                                                //PixelFormat pf = PixelFormats.Bgr32;
                                                //long width = surfaceMsg.Width;
                                                //long height = surfaceMsg.Length;
                                                //long rawStride = (width * pf.BitsPerPixel + 7) / 8;
                                                //byte[] rawImage = new byte[rawStride * height];

                                                //// Initialize the image with data.
                                                //Random value = new Random();
                                                //value.NextBytes(rawImage);

                                                //// Create a BitmapSource.
                                                //BitmapSource bitmap = BitmapSource.Create((int)width, (int)height,
                                                //        96, 96, pf, null,
                                                //        rawImage, (int)rawStride);

                                                //BitmapEncoder encoder = new PngBitmapEncoder();
                                                //encoder.Frames.Add(BitmapFrame.Create(bitmap));

                                                //using (var fileStream = new System.IO.FileStream(AppDomain.CurrentDomain.BaseDirectory + "\\" + "stitchImage.bmp", System.IO.FileMode.Create))
                                                //{
                                                //    encoder.Save(fileStream);
                                                //}

                                            }
                                            break;
                                        case GoDataMessageType.SurfaceIntensity:
                                            {
                                                GoSurfaceIntensityMsg surfaceMsg = (GoSurfaceIntensityMsg)dataObj;
                                                long width = surfaceMsg.Width;
                                                long length = surfaceMsg.Length;
                                                long bufferSize = width * length;
                                                IntPtr bufferPointeri = surfaceMsg.Data;

                                                Console.WriteLine(" Surface Intensity received:");
                                                Console.WriteLine(" Buffer width: {0}", width);
                                                Console.WriteLine(" Buffer length: {0}", length);
                                                byte[] ranges = new byte[bufferSize];
                                                Marshal.Copy(bufferPointeri, ranges, 0, ranges.Length);

                                                OnCapturedIntensityEvent(ranges, (int)width, (int)length);
                                            }
                                            break;
                                    }

                                    Thread.Sleep(10);
                                }
                            }

                        }
                    }
                })).Start();
                return GocatorScanStateConstants.Success;
            }
            catch
            {
                return GocatorScanStateConstants.Fail;
            }

        }
        public void StopScan()
        {
            if (system != null)
            {
                isScanning = false;
                system.Stop();
            }
        }

        public void ClearData()
        {
            if (system != null)
            {
                system.ClearData();
            }
        }

        public void Destroy()
        {
            if (sensor != null)
            {
                try
                {
                    sensor.Dispose();
                    sensor.Destroy();
                    sensor = null;
                }
                catch
                {

                }
            }
            if (system != null)
            {
                try
                {
                    system.Dispose();
                    system.Destroy();
                    system = null;
                }
                catch
                {

                }
            }
        }

        bool isGocatorAxisCheckMode = false;

        public void ShowProfileLaser()
        {
            try
            {
                if (isGocatorAxisCheckMode == false)
                {
                    GoSetup setup = sensor.Setup;
                    setup.TriggerSource = GoTrigger.Time;
                    setup.ScanMode = GoMode.Profile;
                    setup.SetExposure(GoRole.Main, 100);

                    sensor.Start();

                    isGocatorAxisCheckMode = true;
                }
                else
                {
                    isGocatorAxisCheckMode = false;
                    sensor.Stop();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("축 설정 On / OFF 에 실패하였습니다.\n" + ex.Message, "에러");
            }
        }

        public void GetSensorMode(out string mode, out string triggerSource)
        {
            try
            {
                if (sensor != null)
                {
                    GoSetup setup = sensor.Setup;
                    if (setup != null)
                    {
                        mode = setup.ScanMode.ToString();
                        triggerSource = setup.TriggerSource.ToString();
                    }
                    else
                    {
                        mode = null;
                        triggerSource = null;
                    }

                }
                else
                {
                    mode = null;
                    triggerSource = null;
                }
            }
            catch
            {
                mode = null;
                triggerSource = null;
            }
        }

        public void GetIntervalValue(out double? xInterval, out double? yInterval)
        {
            double? tempXInterval = null;
            double? tempYInterval = null;

            try
            {
                if (sensor != null)
                {
                    GoSetup setup = sensor.Setup;
                    if (setup != null)
                    {
                        tempXInterval = setup.GetSpacingInterval(GoRole.Main);
                        tempYInterval = setup.EncoderSpacing;
                    }
                }
            }
            catch
            {

            }

            xInterval = tempXInterval;
            yInterval = tempYInterval;
        }
    }
}
