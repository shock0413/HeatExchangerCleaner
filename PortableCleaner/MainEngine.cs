using AsyncSocket;
using GocatorLib;
using OpenCvSharp.Extensions;
using PortableCleaner.Struct;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Utill;

namespace PortableCleaner
{
    public partial class MainEngine
    {
        private int errorDataID = 0;

        IniFile iniConfig = new IniFile(AppDomain.CurrentDomain.BaseDirectory + "\\" + "Config.ini");
        MainWindow window;

        private bool isRelayMode = false;
        private bool isRetryMode = false;

        // 원점 이동 중 확인 플래그
        private bool isOriginMoving = false;

        Dictionary<string, string> inputValues;
        Dictionary<string, string> outputValues;

        PlcManager writePlcManager;
        PlcManager readPlcManager;

        bool isClosing = false;

        Stopwatch buttonTimmer = new Stopwatch();
        int buttonTimmerTime;

        GocatorManager gocatorManager;

        private bool finishOriginSetting = false;

        public MainEngine(MainWindow window)
        {
            this.window = window;

            LoadPlcAddressConfig();
            InitPlc();
            InitPlcTick();
            InitConfig();
            InitEvents();
            InitGocatorManager();
            InitLimitCheckThread();

            InitPhoneServer();
            InitPhoneCommandServer();
        }

        void LoadPlcAddressConfig()
        {
            inputValues = iniConfig.GetSectionValues("PLC Input");
            outputValues = iniConfig.GetSectionValues("PLC Output");
        }

        void InitPlc()
        {
            string ip = iniConfig.GetString("PLC", "IP", "192.168.1.10");
            int port = iniConfig.GetInt32("PLC", "Port", 2004);
            writePlcManager = new PlcManager(ip, port);
            readPlcManager = new PlcManager(ip, port);
            writePlcManager.Connect();
            readPlcManager.Connect();

            ReleaseJogLeft();
            ReleaseJogRight();
            ReleaseJogUp();
            ReleaseJogDown();
        }

        void InitConfig()
        {
            buttonTimmerTime = iniConfig.GetInt32("Info", "Button Time", 1000);
        }

        void InitGocatorManager()
        {
            string ip = iniConfig.GetString("Gocator", "IP", "192.168.1.9");
            Console.WriteLine("Gocator IP : " + ip);
            gocatorManager = new GocatorManager(ip);
            gocatorManager.OnCaptureEvent += GocatorManager_OnCaptureEvent;
            gocatorManager.OnCapturedIntensityEvent += GocatorManager_OnCapturedIntensityEvent;

            gocatorManager.Connect();
        }

        private void GocatorManager_OnCapturedIntensityEvent(byte[] datas, int width, int length)
        {
            try
            {
                if (width > 1000)
                {
                    PixelFormat pf = PixelFormats.Gray8;
                    long rawStride = (width * pf.BitsPerPixel + 7) / 8;
                    byte[] rawImage = new byte[rawStride * length];

                    BitmapSource bitmap = BitmapSource.Create((int)width, (int)length,
                        96, 96, pf, null,
                        datas, (int)rawStride);
                    OpenCvSharp.Mat mat = bitmap.ToMat();
                    mat = mat.Resize(new OpenCvSharp.Size((int)(mat.Width * inspectionInfo.xResolution), (int)(mat.Height * inspectionInfo.yResolution)));
                    mat.SaveImage(Environment.CurrentDirectory + "//appImage.bmp");
                    bitmap = mat.ToBitmapSource();
                    bitmap.Freeze();


                    window.Dispatcher.Invoke(() =>
                    {
                        InspectionInfo.appImage = bitmap;
                        inspectionInfo.appImageWidth = bitmap.Width;
                        inspectionInfo.appImageHeight = bitmap.Height;
                        InspectionInfo.IsFinishScanningIntensityImage = true;
                    });
                }
            }
            catch (Exception e)
            {
                LogManager.Error("[GocatorManager_OnCapturedIntensityEvent] 에러 발생 : " + e.Message);
            }
        }

        void InitLimitCheckThread()
        {
            new Thread(new ThreadStart(() =>
            {
                while(isClosing == false)
                {
                    if (finishOriginSetting == true)
                    {
                        if (Convert.ToDouble(currentMotorZ) >= zMaxLimit)
                        {
                            if (output_MoveDown == "1")
                            {
                                ReleaseJogDown();

                                Console.WriteLine("Jog Limit Error");
                            }
                        }

                        if (Convert.ToDouble(currentMotorZ) <= zMinLimit)
                        {
                            if (output_MoveUp == "1")
                            {
                                ReleaseJogUp();
                            }
                            Console.WriteLine("Jog Limit Error");
                        }

                        if (Convert.ToDouble(currentMotorX) <= xMinLimit)
                        {
                            if(output_MoveLeft == "1")
                            {
                                ReleaseJogLeft();
                                Console.WriteLine("Jog Limit Error");
                            }
             
                        }

                        if (Convert.ToDouble(currentMotorX) >= xMaxLimit)
                        {
                            if (output_MoveRight == "1")
                            {
                                ReleaseJogRight();
                                Console.WriteLine("Jog Limit Error");
                            }
               
                        }

                        Thread.Sleep(100);
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                }
            })).Start();
            
        }

        private AsyncSocketServer phoneServer;
        private int phoneClientID;
        AsyncSocketClient phoneClient;

        private AsyncFrameSocket.AsyncSocketServer phoneCommandServer;
        private int phoneCommandClientID;
        AsyncFrameSocket.AsyncSocketClient phoneCommandClient;


        int[] jogThreadjob = new int[6];

        private void InitPhoneServer()
        {
            new Thread(new ThreadStart(() =>
            {
                int port = iniConfig.GetInt32("Server", "Phone Port", 9600);

                phoneServer = new AsyncSocketServer(port);
                phoneServer.OnAccept += new AsyncSocketAcceptEventHandler(OnPhoneServerAccept);
                phoneServer.OnError += new AsyncSocketErrorEventHandler(OnPhoneServerError);

                phoneServer.Listen(IPAddress.Any);
            })).Start();

            new Thread(new ThreadStart(() =>
            {
                while (isClosing == false)
                {
                    

                    if (IsControllSystem == false )
                    {
                        //Console.WriteLine(jogThreadjob[0] + "," +
                        //jogThreadjob[1] + "," +
                        //jogThreadjob[2] + "," +
                        //jogThreadjob[3] + "," +
                        //jogThreadjob[4] + "," +
                        //jogThreadjob[5]);

                        Stopwatch sw = new Stopwatch();
                        sw.Start();

                        //continue;

                        if (jogThreadjob[0] == 1)
                        {
                            if (output_MoveRight == "1")
                            {
                                ReleaseJogRight();

                            }
                            if (output_MoveLeft == "0")
                            {
                                SetJogLeft();
                            }
                        }
                        else
                        {
                            if (output_MoveLeft == "1")
                            {
                                ReleaseJogLeft();
                            }
                        }

                        if (jogThreadjob[1] == 0x01)
                        {
                            if (Output_MoveLeft == "1")
                            {
                                ReleaseJogLeft();

                            }
                            if (output_MoveRight == "0")
                            {
                                SetJogRight();
                            }
                        }
                        else
                        {
                            if (output_MoveRight == "1")
                            {
                                ReleaseJogRight();
                            }
                        }

                        if (jogThreadjob[2] == 0x01)
                        {
                            if (output_MoveDown == "1")
                            {
                                ReleaseJogDown();

                            }
                            if (output_MoveUp == "0")
                            {
                                SetJogUp();
                            }
                        }
                        else
                        {
                            if (output_MoveUp == "1")
                            {
                                ReleaseJogUp();
                            }
                        }

                        if (jogThreadjob[3] == 0x01)
                        {
                            if (output_MoveUp == "1")
                            {
                                ReleaseJogUp();

                            }
                            if (output_MoveDown == "0")
                            {
                                SetJogDown();
                            }
                        }
                        else
                        {
                            if (output_MoveDown == "1")
                            {
                                ReleaseJogDown();
                            }
                        }

                        if (jogThreadjob[4] == 0x01)
                        {
                            if(output_NozleBackword == "1")
                            {
                                NozleBackwordOff();
                            }

                            if(output_NozleForword == "0")
                            {
                                NozleForwordOn();
                            }
                            
                        }
                        else
                        {
                            if (output_NozleForword == "1")
                            {
                                NozleForwordOff();
                            }
                        }

                        if (jogThreadjob[5] == 0x01)
                        {
                            if (output_NozleForword == "1")
                            {
                                NozleForwordOff();
                            }

                            if (output_NozleBackword == "0")
                            {
                                NozleBackwordOn();
                            }

                        }
                        else
                        {
                            if (output_NozleBackword == "1")
                            {
                                NozleBackwordOff();
                            }
                        }

                        //Console.WriteLine("////////////조그 실행 소요시간 : " + sw.ElapsedMilliseconds);

                        while (sw.ElapsedMilliseconds < 100)
                        {
                            Thread.Sleep(1);
                        }
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
            })).Start();

            new Thread(new ThreadStart(() =>
            {
                jogThreadjob = new int[6];
                while (isClosing == false)
                {
                    while (receivedData.Count > 8)
                    {
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        //데이터 분리
                        byte heartbeat = receivedData.Dequeue();
                        byte left = receivedData.Dequeue();
                        byte right = receivedData.Dequeue();
                        byte up = receivedData.Dequeue();
                        byte down = receivedData.Dequeue();
                        byte nozelForword = receivedData.Dequeue();
                        byte nozelBackword = receivedData.Dequeue();
                        byte getControl = receivedData.Dequeue();

                        if (getControl == 0x01)
                        {
                            IsControllSystem = false;
                        }

                        jogThreadjob[0] = left;
                        jogThreadjob[1] = right;
                        jogThreadjob[2] = up;
                        jogThreadjob[3] = down;
                        jogThreadjob[4] = nozelForword;
                        jogThreadjob[5] = nozelBackword;
                        sw.Stop();
                        //Console.WriteLine("dequeue time : " + sw.ElapsedMilliseconds);
                        
                    }

                    Thread.Sleep(50);
                }
            })).Start();
        }

        private void OnPhoneServerAccept(object sender, AsyncSocketAcceptEventArgs e)
        {
            new Thread(new ThreadStart(() =>
            {
                AsyncSocketClient worker = new AsyncSocketClient(phoneClientID++, e.Worker);

                // 데이터 수신을 대기한다.
                worker.OnConnet += new AsyncSocketConnectEventHandler(OnPhoneServerConnet);
                worker.OnClose += new AsyncSocketCloseEventHandler(OnPhoneServerClose);
                worker.OnError += new AsyncSocketErrorEventHandler(OnPhoneServerError);
                worker.OnSend += new AsyncSocketSendEventHandler(OnPhoneServerSend);
                worker.OnReceive += new AsyncSocketReceiveEventHandler(OnPhoneServerReceive);

                phoneClient = worker;

                worker.Receive();
                
            })).Start();
        }

        private void OnPhoneServerConnet(object sender, AsyncSocketConnectionEventArgs e)
        {
            
            Console.WriteLine("Connect");
        }


        private void OnPhoneServerClose(object sender, AsyncSocketConnectionEventArgs e)
        {
            Console.WriteLine("close");
        }


        private void OnPhoneServerSend(object sender, AsyncSocketSendEventArgs e)
        {

        }

        private void OnPhoneServerError(object sender, AsyncSocketErrorEventArgs e)
        {
 
        }

        Queue<byte> receivedData = new Queue<byte>();
        byte phoneHeartbeat = 0;
        private void OnPhoneServerReceive(object sender, AsyncSocketReceiveEventArgs e)
        {
            //Console.WriteLine(DateTime.Now.ToString("ss fff"));
            try
            {
                for (int i = 0; i < e.ReceiveBytes; i++)
                {
                    receivedData.Enqueue(e.ReceiveData[i]);

                }

                List<byte> sendBuffer = new List<byte>();
                if (phoneHeartbeat == 0x00)
                {
                    phoneHeartbeat = 0x01;
                }
                else
                {
                    phoneHeartbeat = 0x00;
                }
                sendBuffer.Add(phoneHeartbeat);
                if (IsControllSystem)
                {
                    sendBuffer.Add(0x01);
                }
                else
                {
                    sendBuffer.Add(0x00);
                }
                if (input_Nozle1Forword == "1")
                {
                    sendBuffer.Add(0x01);
                }
                else
                {
                    sendBuffer.Add(0x00);
                }
                if (input_Nozle2Forword == "1")
                {
                    sendBuffer.Add(0x01);
                }
                else
                {
                    sendBuffer.Add(0x00);
                }
                if (input_Nozle3Forword == "1")
                {
                    sendBuffer.Add(0x01);
                }
                else
                {
                    sendBuffer.Add(0x00);
                }

                if (input_Nozle1Backword == "1")
                {
                    sendBuffer.Add(0x01);
                }
                else
                {
                    sendBuffer.Add(0x00);
                }
                if (input_Nozle2Backword == "1")
                {
                    sendBuffer.Add(0x01);
                }
                else
                {
                    sendBuffer.Add(0x00);
                }
                if (input_Nozle3Backword == "1")
                {
                    sendBuffer.Add(0x01);
                }
                else
                {
                    sendBuffer.Add(0x00);
                }

                List<byte> temp = new List<byte>();
                temp.Add(0x01);
                temp.AddRange(BitConverter.GetBytes(sendBuffer.Count));
                temp.Add(0x02);
                temp.AddRange(sendBuffer);
                temp.Add(0x03);

                phoneClient.Send(temp.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine("phone send ex ");
            }
        }
        //

        private void InitPhoneCommandServer()
        {
            new Thread(new ThreadStart(() =>
            {
                int port = iniConfig.GetInt32("Server", "Phone Command Port", 9601);

                phoneCommandServer = new AsyncFrameSocket.AsyncSocketServer(port);
                phoneCommandServer.OnAccept += new AsyncFrameSocket.AsyncSocketAcceptEventHandler(OnPhoneCommandServerAccept);
                phoneCommandServer.OnError += new AsyncFrameSocket.AsyncSocketErrorEventHandler(OnPhoneCommandServerError);

                phoneCommandServer.Listen(IPAddress.Any);
            })).Start();
        }

        private void OnPhoneCommandServerAccept(object sender, AsyncFrameSocket.AsyncSocketAcceptEventArgs e)
        {
            new Thread(new ThreadStart(() =>
            {
                AsyncFrameSocket.AsyncSocketClient worker = new AsyncFrameSocket.AsyncSocketClient(phoneCommandClientID++, e.Worker);

                // 데이터 수신을 대기한다.
                worker.OnConnet += new AsyncFrameSocket.AsyncSocketConnectEventHandler(OnPhoneCommandServerConnet);
                worker.OnClose += new AsyncFrameSocket.AsyncSocketCloseEventHandler(OnPhoneCommandServerClose);
                worker.OnError += new AsyncFrameSocket.AsyncSocketErrorEventHandler(OnPhoneCommandServerError);
                worker.OnSend += new AsyncFrameSocket.AsyncSocketSendEventHandler(OnPhoneCommandServerSend);
                worker.OnReceive += new AsyncFrameSocket.AsyncSocketReceiveEventHandler(OnPhoneCommandServerReceive);

                phoneCommandClient = worker;

                worker.Receive();

            })).Start();
        }

        private void OnPhoneCommandServerConnet(object sender, AsyncFrameSocket.AsyncSocketConnectionEventArgs e)
        {

            Console.WriteLine("Connect");
        }


        private void OnPhoneCommandServerClose(object sender, AsyncFrameSocket.AsyncSocketConnectionEventArgs e)
        {
            Console.WriteLine("close");
        }


        private void OnPhoneCommandServerSend(object sender, AsyncFrameSocket.AsyncSocketSendEventArgs e)
        {

        }

        private void OnPhoneCommandServerError(object sender, AsyncFrameSocket.AsyncSocketErrorEventArgs e)
        {

        }

        private void OnPhoneCommandServerReceive(object sender, AsyncFrameSocket.AsyncSocketReceiveEventArgs e)
        {
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    string receivedStr = Encoding.ASCII.GetString(e.ReceiveData, 0, e.ReceiveBytes);
                    string command = receivedStr.Split(',')[0];
                    Console.WriteLine("Received Command : " + command);
                    if(command == "CENTERING")
                    {
                        string param = receivedStr.Split(',')[1];
                        if (param == "1")
                        {
                            isManualLazerOn = false;
                            ManualLazerOn();
                        }
                        else if(param == "0")
                        {
                            isManualLazerOn = true;
                            ManualLazerOn();
                        }
                    }
                    else if(command == "START")
                    {
                        isRetryMode = false;
                        isRelayMode = false;

                        InspectionInfo = new InspectionInfo();
                        string model = receivedStr.Split(',')[1];
                        string length = receivedStr.Split(',')[2];

                        InspectionInfo.BundleName = model;
                        InspectionInfo.BundleLength = Convert.ToInt32(length);

                        SendCommandReply(sender, "RPY_START,", 0x00);
                    }
                    else if (command == "TEST_START")
                    {
                        isRetryMode = false;
                        isRelayMode = false;

                        InspectionInfo = new InspectionInfo();
                        string model = receivedStr.Split(',')[1];
                        string length = receivedStr.Split(',')[2];

                        InspectionInfo.BundleName = model;
                        InspectionInfo.BundleLength = Convert.ToInt32(length);

                        SendCommandReply(sender, "RPY_TEST_START,", 0x00);
                    }
                    else if (command == "RELAY_START")
                    {
                        isRetryMode = false;
                        isRelayMode = true;

                        if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "InspectionData\\inspectionData.data"))
                        {
                            try
                            {
                                string[] lines = File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + "InspectionData\\inspectionData.data");

                                if (lines.Length > 12)
                                {
                                    InspectionInfo = new InspectionInfo();
                                    InspectionInfo.BundleName = lines[0];
                                    InspectionInfo.BundleLength = Convert.ToInt32(lines[1]);
                                    InspectionInfo.IsUseNozle = Convert.ToBoolean(lines[2]);
                                    InspectionInfo.CleaningMaxCount = Convert.ToInt32(lines[3]);
                                    InspectionInfo.CleaningCount = Convert.ToInt32(lines[4]);
                                    InspectionInfo.CurrentCleaingHoleIndex = Convert.ToInt32(lines[5]);
                                    InspectionInfo.IsFinishScanning = Convert.ToBoolean(lines[6]);
                                    InspectionInfo.IsFinishScanningIntensityImage = Convert.ToBoolean(lines[7]);
                                    InspectionInfo.scanXDistance = Convert.ToDouble(lines[8]);
                                    InspectionInfo.scanZDistance = Convert.ToDouble(lines[9]);
                                    InspectionInfo.xResolution = Convert.ToDouble(lines[10]);
                                    InspectionInfo.zResolution = Convert.ToDouble(lines[11]);
                                    int holeCount = Convert.ToInt32(lines[12]);
                                    originLeftTopX = Convert.ToDouble(lines[13]);
                                    originLeftTopZ = Convert.ToDouble(lines[14]);
                                    originRightBottomX = Convert.ToDouble(lines[15]);
                                    originRightBottomZ = Convert.ToDouble(lines[16]);

                                    InspectionInfo.Holes.Clear();

                                    for (int i = 17; i < holeCount + 17; i++)
                                    {
                                        string[] strSplit = lines[i].Split(',');
                                        StructHole structHole = new StructHole();
                                        structHole.Index = Convert.ToInt32(strSplit[0]);
                                        structHole.X = Convert.ToDouble(strSplit[1]);
                                        structHole.Y = Convert.ToDouble(strSplit[2]);
                                        structHole.VisionX = Convert.ToDouble(strSplit[3]);
                                        structHole.VisionY = Convert.ToDouble(strSplit[4]);
                                        structHole.GroupIndex = Convert.ToInt32(strSplit[5]);
                                        structHole.Row = Convert.ToInt32(strSplit[6]);
                                        structHole.Column = Convert.ToInt32(strSplit[7]);
                                        structHole.IsCleaningFinish = Convert.ToBoolean(strSplit[8]);
                                        structHole.IsOK = Convert.ToBoolean(strSplit[9]);
                                        structHole.IsSortStartPoint = Convert.ToBoolean(strSplit[10]);
                                        structHole.IsTarget = Convert.ToBoolean(strSplit[11]);
                                        structHole.AfterDistance = Convert.ToDouble(strSplit[12]);

                                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            InspectionInfo.Holes.Add(structHole);
                                        });
                                    }

                                    if (InspectionInfo.Holes.Count > 3)
                                    {
                                        for (int i = 1; i < InspectionInfo.Holes.Count - 1; i++)
                                        {
                                            InspectionInfo.Holes[i].BeforePoint = InspectionInfo.Holes[i - 1];
                                            InspectionInfo.Holes[i].AfterPoint = InspectionInfo.Holes[i + 1];
                                        }
                                    }

                                    OpenCvSharp.Mat appImage = OpenCvSharp.Cv2.ImRead(AppDomain.CurrentDomain.BaseDirectory + "InspectionData\\appImage.bmp");
                                    OpenCvSharp.Mat cleaningImage = OpenCvSharp.Cv2.ImRead(AppDomain.CurrentDomain.BaseDirectory + "InspectionData\\cleaningImage.bmp");
                                    OpenCvSharp.Mat holeSettingImage = OpenCvSharp.Cv2.ImRead(AppDomain.CurrentDomain.BaseDirectory + "InspectionData\\holeSettingImage.bmp");
                                    OpenCvSharp.Mat holeSettingOriginalImage = OpenCvSharp.Cv2.ImRead(AppDomain.CurrentDomain.BaseDirectory + "InspectionData\\holeSettingOriginalImage.bmp");

                                    window.Dispatcher.Invoke(() =>
                                    {
                                        InspectionInfo.appImage = appImage.ToBitmapSource();
                                        inspectionInfo.appImageWidth = appImage.Width;
                                        inspectionInfo.appImageHeight = appImage.Height;
                                        InspectionInfo.CleaningImage = cleaningImage.ToBitmapSource();
                                        InspectionInfo.HoleSettingImage = holeSettingImage.ToBitmapSource();
                                        InspectionInfo.HoleSettingOriginalImage = holeSettingOriginalImage.ToBitmapSource();
                                    });
                                }
                            }
                            catch (Exception exc)
                            {
                                Console.WriteLine(exc.Message);
                            }
                        }

                        SendCommandReply(sender, "RPY_RELAY_START,", 0x00);
                    }
                    else if(command == "ORIGIN_LEFT_TOP")
                    {
                        try
                        {
                            /*
                            inspectionInfo.IsFinishScanning = false;
                            inspectionInfo.IsFinishScanningIntensityImage = false;
                            inspectionInfo.Holes.Clear();
                            inspectionInfo.CurrentCleaingHoleIndex = 0;
                            */

                            string name = inspectionInfo.BundleName;
                            int length = inspectionInfo.BundleLength;

                            InspectionInfo = new InspectionInfo();
                            inspectionInfo.BundleName = name;
                            inspectionInfo.BundleLength = length;

                            SetOriginLeftTop();
                            SendCommandReply(sender, "RPY_ORIGIN_LEFT_TOP,", 0x00);
                        }
                        catch
                        {
                            SendCommandReply(sender, "RPY_ORIGIN_LEFT_TOP,", 0xff);
                        }
                    }
                    else if(command == "START_SCAN")
                    {
                        try
                        {
                            SetOriginRightBottom();
                            StartScan();

                            Thread.Sleep(300);
                            try
                            {
                                SendCommandReply(phoneCommandClient, "RPY_START_SCAN,", 0x00);
                            }
                            catch
                            {

                            }
                        }
                        catch
                        {

                        }
                    }
                    else if(command == "START_CLEANING")
                    {
                        try
                        {
                            string isUseNozel = receivedStr.Split(',')[1];
                            if (isUseNozel == "0")
                            {
                                InspectionInfo.IsUseNozle = false;
                                NozleOff();
                            }
                            else if (isUseNozel == "1")
                            {
                                InspectionInfo.IsUseNozle = true;
                                NozleOn();
                            }

                            Console.WriteLine("is use Nozel : " + isUseNozel  + "/" + InspectionInfo.IsUseNozle);

                            StartAutoCleaning();

                            SendCommandReply(sender, "RPY_START_CLEANING,", 0x00);
                        }
                        catch
                        {
                            SendCommandReply(sender, "RPY_START_CLEANING,", 0xff);
                        }
                    }
                    else if (command == "RETRY_START")
                    {
                        try
                        {
                            isRelayMode = false;
                            isRetryMode = true;
                            inspectionInfo.CurrentCleaingHoleIndex = 0;
                            SendCommandReply(sender, "RPY_SKIP_ONE_HOLE,", 0x00);
                        }
                        catch
                        {
                            SendCommandReply(sender, "RPY_SKIP_ONE_HOLE,", 0xff);
                        }
                    }
                    else if(command == "SKIP_ONE_HOLE")
                    {
                        try
                        {
                            SkipOneHole();
                            SendCommandReply(sender, "RPY_SKIP_ONE_HOLE,", 0x00);
                        }
                        catch
                        {
                            SendCommandReply(sender, "RPY_SKIP_ONE_HOLE,", 0xff);
                        }
                    }
                    else if (command == "MOVE_HOLE")
                    {
                        try
                        {
                            
                            string moveIndex = receivedStr.Split(',')[1];
                            MoveHole(Convert.ToInt32(moveIndex) - 2);
                            Console.WriteLine("move Index : " + moveIndex);

                            SendCommandReply(sender, "RPY_MOVE_HOLE,", 0x00);
                        }
                        catch
                        {
                            SendCommandReply(sender, "RPY_MOVE_HOLE,", 0xff);
                        }
                    }
                    else if(command == "PUMP_ON")
                    {
                        try
                        {
                            PumpOn();
                            SendCommandReply(sender, "RPY_PUMP_ON,", 0x00);
                        }
                        catch
                        {
                            SendCommandReply(sender, "RPY_PUMP_ON,", 0xff);
                        }
                    }
                    else if (command == "PUMP_OFF")
                    {
                        try
                        {   
                            PumpOff();
                            SendCommandReply(sender, "RPY_PUMP_OFF,", 0x00);
                        }
                        catch
                        {
                            SendCommandReply(sender, "RPY_PUMP_OFF,", 0xff);
                        }
                    }
                    else if (command == "SCAN_IMAGE")
                    {
                        try
                        {
                            if (inspectionInfo.IsFinishScanning && inspectionInfo.IsFinishScanningIntensityImage)
                            {
                                Console.WriteLine("============ready to send image");
                                OpenCvSharp.Mat mat = null;

                                window.Dispatcher.Invoke(() =>
                                {
                                    try
                                    {
                                        mat = inspectionInfo.appImage.ToMat();
                                    }
                                    catch
                                    {

                                    }
                                });

                                int width = mat.Cols;
                                int height = mat.Rows;
                                int channels = mat.Channels();

                                byte[] output = new byte[width * height * channels];
                                OpenCvSharp.Cv2.ImEncode(".bmp", mat, out output);


                                string rpyStr = command;
                                List<byte> buffer = new List<byte>(Encoding.ASCII.GetBytes("RPY_SCAN_IMAGE,"));
                                buffer.Add(0x01);
                                buffer.AddRange(BitConverter.GetBytes(width));
                                buffer.AddRange(BitConverter.GetBytes(height));
                                buffer.AddRange(BitConverter.GetBytes(channels));
                                buffer.AddRange(output);
                                Console.WriteLine("scan Image Replay");

                                AsyncFrameSocket.AsyncSocketClient sock = sender as AsyncFrameSocket.AsyncSocketClient;
                                sock.Send(buffer.ToArray());
                            }
                            else
                            {
                                SendCommandReply(sender, "RPY_SCAN_IMAGE,", 0x00);
                            }
                        }
                        catch (Exception exc)
                        {
                            SendCommandReply(sender, "RPY_SCAN_IMAGE,", 0x00);
                            Console.WriteLine("예외 발생 : " + exc.Message);
                        }
                    }
                    else if(command == "SCAN_HOLES")
                    {
                        List<System.Windows.Point> holes = new List<System.Windows.Point>();
                        for(int i = 0; i < inspectionInfo.Holes.Count; i ++)
                        {
                            holes.Add(new System.Windows.Point(inspectionInfo.Holes[i].VisionX, inspectionInfo.Holes[i].VisionY));
                        }

                        string sendMsg = "RPY_SCAN_HOLES,";
                        sendMsg += holes.Count + ",";
                        int targetIndex = 1;
                        for(int i = 0; i < holes.Count; i ++)
                        {
                            StructHole hole = inspectionInfo.Holes[i];

                            int[] colorArr = new int[] { 1, 1, 1, 3, 3, 3, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, };
                            int colorIndex = 0;

                            string name = "";
                            if(hole.IsTarget)
                            {
                                name = targetIndex.ToString();
                                targetIndex++;
                            }

                            if (hole.IsCleaningFinish)
                            {
                                if (hole.IsOK)
                                {
                                    colorIndex = 2;
                                }
                                else
                                {
                                    colorIndex = 3;
                                }
                            }
                            else
                            {
                                if (hole.IsTarget)
                                {
                                    colorIndex = 1;
                                }
                                else
                                {
                                    colorIndex = 0;
                                }
                            }

                            int x = (int)holes[i].X;
                            int y = (int)holes[i].Y;

                            sendMsg += i + ",";
                            sendMsg += name + ",";
                            sendMsg += x + ",";
                            sendMsg += y + ",";
                            sendMsg += colorIndex + ",";
                        }

                        AsyncFrameSocket.AsyncSocketClient sock = sender as AsyncFrameSocket.AsyncSocketClient;
                        sock.Send(Encoding.ASCII.GetBytes(sendMsg));
                    }
                    else if (command == "EMERGENCY_STOP")
                    {
                        try
                        {
                            if (isEmergency)
                            {
                                EmergencyOff();

                                isEmergency = false;

                                btn_Emg_Text = "비상정지 ON";

                                AlramResetOn();

                                Thread.Sleep(100);

                                AlramResetOff();
                            }
                            else
                            {
                                EmergencyOn();

                                isEmergency = true;

                                btn_Emg_Text = "비상정지 OFF";
                            }

                            SendCommandReply(sender, "RPY_EMERGENCY_STOP,", 0x00);
                        }
                        catch
                        {
                            SendCommandReply(sender, "RPY_EMERGENCY_STOP,", 0xff);
                        }
                    }
                    else if (command == "CLEANING_DATA")
                    {
                        string sendMsg = "RPY_CLEANING_DATA,";

                        sendMsg += InspectionInfo.CleaningMaxCount + "," + InspectionInfo.CleaningCount;

                        AsyncFrameSocket.AsyncSocketClient sock = sender as AsyncFrameSocket.AsyncSocketClient;
                        sock.Send(Encoding.ASCII.GetBytes(sendMsg));
                    }
                    else if(command == "PAUSE_CLEANING")
                    {
                        window.Dispatcher.Invoke(() =>
                        {
                            IsPauseCleaning = true;
                        });
                        
                        try
                        {
                            SendCommandReply(sender, "RPY_PAUSE_CLEANING,", 0x00);
                        }
                        catch
                        {
                            SendCommandReply(sender, "RPY_PAUSE_CLEANING,", 0xff);
                        }
                    }
                    else if (command == "RESUME_CLEANING")
                    {
                        window.Dispatcher.Invoke(() =>
                        {
                            IsPauseCleaning = false;
                        });

                        try
                        {
                            SendCommandReply(sender, "RPY_RESUME_CLEANING,", 0x00);
                        }
                        catch
                        {
                            SendCommandReply(sender, "RPY_RESUME_CLEANING,", 0xff);
                        }
                    }
                    else if(command == "CANCEL_CLEANING")
                    {
                        try
                        {
                            if (startCleaning)
                            {
                                StopCleaning();

                                if (IsPauseCleaning)
                                {
                                    IsPauseCleaning = false;
                                }

                                SendCommandReply(sender, "RPY_CANCEL_CLEANING,", 0x00);
                            }
                            else
                            {
                                SendCommandReply(sender, "RPY_CANCEL_CLEANING,", 0xff);
                            }
                        }
                        catch
                        {
                            SendCommandReply(sender, "RPY_CANCEL_CLEANING,", 0xff);
                        }
                    }
                    else if(command == "X_HOME")
                    {
                        int zValue = (int)(Convert.ToDouble(currentMotorZ) * 100);

                        if (!isOriginMoving)
                        {
                            isOriginMoving = true;
                            Move(0, zValue);
                        }

                        Thread.Sleep(500);
                        EndMove();

                        while (true)
                        {
                            if (Convert.ToDouble(currentMotorX) == 0)
                            {
                                isOriginMoving = false;
                                break;
                            }
                            Thread.Sleep(500);
                        }
                    }
                    else if(command == "Y_HOME")
                    {
                        int xValue = (int)(Convert.ToDouble(currentMotorX) * 100);

                        if (!isOriginMoving)
                        {
                            isOriginMoving = true;
                            Move(xValue, 0);
                        }

                        Thread.Sleep(500);
                        EndMove();

                        while (true)
                        {
                            if (Convert.ToDouble(currentMotorZ) == 0)
                            {
                                isOriginMoving = false;
                                break;
                            }
                            Thread.Sleep(500);
                        }
                    }
                    else if(command == "HOME_POSITION_SETTING")
                    {
                        SetOrigin();
                    }
                    else if (command == "ADD_HOLE")
                    {
                        string xStr = receivedStr.Split(',')[1];
                        string yStr = receivedStr.Split(',')[2];
                        double x = Convert.ToDouble(xStr);
                        double y = Convert.ToDouble(yStr);
                        bool isAdded = false;

                        window.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                double visionOriginX = iniConfig.GetDouble("Origin", "Vision X", 0); ;//60.38;
                                double visionOriginY = iniConfig.GetDouble("Origin", "Vision Y", 0); ; //29.12;
                                double motorBiasX = -visionOriginX;
                                double motorBiasZ = -visionOriginY;

                                //홀 정렬 및 표시
                                List<StructHole> holes = InspectionInfo.Holes.ToList();

                                StructHole structHole = new StructHole();
                                // structHole.X = x + motorBiasX;
                                // structHole.Y = y + motorBiasZ;
                                structHole.X = x + motorBiasX;
                                structHole.Y = y + motorBiasZ;
                                structHole.VisionX = x;
                                structHole.VisionY = y;

                                Console.WriteLine("motor x : " + structHole.VisionX + ", motor y : " + structHole.VisionY);
                                Console.WriteLine("originLeftTopX : " + originLeftTopX);
                                Console.WriteLine("originLeftTopZ : " + originLeftTopZ);
                                Console.WriteLine("originRightBottomX : " + originRightBottomX);
                                Console.WriteLine("originRightBottomZ : " + originRightBottomZ);
                                // Console.WriteLine("앱이미지 너비 : " + InspectionInfo.appImage.Width);
                                // Console.WriteLine("앱이미지 높이 : " + InspectionInfo.appImage.Height);

                                double appImageWidth = inspectionInfo.appImageWidth;
                                double appImageHeight = inspectionInfo.appImageHeight;

                                // 중복 체크
                                bool isChecked = false;

                                for (int i = 0; i < holes.Count; i++)
                                {
                                    StructHole hole = holes[i];

                                    if (Math.Abs(structHole.VisionX - hole.VisionX) < 10 && Math.Abs(structHole.VisionY - hole.VisionY) < 10)
                                    {
                                        isChecked = true;
                                        break;
                                    }
                                }

                                if (!isChecked && structHole.VisionY > 0 && structHole.VisionY < appImageHeight && structHole.VisionX > 0 && structHole.VisionX < appImageWidth)
                                {
                                    holes.Add(structHole);
                                    isAdded = true;
                                }

                                holes = holes.OrderBy(_x => _x.StartDistance).ToList();
                                //홀 정렬
                                holes = CleaningManager.SortHole(holes, 3, 20, 40);
                                holes = CleaningManager.CheckHole(holes);

                                List<StructHole> hs = holes.Where(h => h.IsTarget).ToList();

                                for (int j = hs.Count - 1; j >= 0; j--)
                                {
                                    if (hs[j].IsCleaningFinish)
                                    {
                                        int index = 0;

                                        if (j + 1 > hs.Count)
                                        {
                                            index = j;
                                        }
                                        else
                                        {
                                            index = j + 1;
                                        }

                                        inspectionInfo.CurrentCleaingHoleIndex = index;
                                        break;
                                    }
                                    else
                                    {
                                        inspectionInfo.CurrentCleaingHoleIndex = j;
                                    }
                                }

                                InspectionInfo.Holes = new ObservableCollection<StructHole>(holes);

                                InspectionInfo.HoleSettingImage = DrawSettingHoles(holes);

                                if (isAdded)
                                {
                                    SendCommandReply(sender, "RPY_ADD_HOLE,", 0x00);
                                }
                                else
                                {
                                    SendCommandReply(sender, "RPY_ADD_HOLE,", 0xff);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        });
                    }
                    else if (command == "REMOVE_HOLE")
                    {
                        string xStr = receivedStr.Split(',')[1];
                        string yStr = receivedStr.Split(',')[2];
                        double x = Convert.ToDouble(xStr);
                        double y = Convert.ToDouble(yStr);

                        try
                        {
                            window.Dispatcher.Invoke(() =>
                            {
                                List<StructHole> holes = InspectionInfo.Holes.ToList();

                                for (int i = 0; i < holes.Count; i++)
                                {
                                    StructHole hole = holes[i];

                                    if (Math.Abs(hole.VisionX - x) <= 10 && Math.Abs(hole.VisionY - y) <= 10)
                                    {
                                        window.Dispatcher.Invoke(() =>
                                        {
                                            Console.WriteLine("motor x : " + hole.X + ", motor y : " + hole.Y);
                                            holes.RemoveAt(i);
                                        });

                                        break;
                                    }
                                }

                                holes = holes.OrderBy(_x => _x.StartDistance).ToList();
                                //홀 정렬
                                holes = CleaningManager.SortHole(holes, 3, 20, 40);
                                holes = CleaningManager.CheckHole(holes);

                                List<StructHole> hs = holes.Where(h => h.IsTarget).ToList();

                                for (int j = hs.Count - 1; j >= 0; j--)
                                {
                                    if (hs[j].IsCleaningFinish)
                                    {
                                        int index = 0;

                                        if (j + 1 > hs.Count)
                                        {
                                            index = j;
                                        }
                                        else
                                        {
                                            index = j + 1;
                                        }

                                        inspectionInfo.CurrentCleaingHoleIndex = index;
                                        break;
                                    }
                                    else
                                    {
                                        inspectionInfo.CurrentCleaingHoleIndex = j;
                                    }
                                }

                                InspectionInfo.Holes = new ObservableCollection<StructHole>(holes);

                                InspectionInfo.HoleSettingImage = DrawSettingHoles(holes);
                            });

                            SendCommandReply(sender, "RPY_REMOVE_HOLE,", 0x00);
                        }
                        catch
                        {
                            SendCommandReply(sender, "RPY_REMOVE_HOLE,", 0xff);
                        }
                    }
                    else if (command == "ERROR_MSG")
                    {
                        // SendErrorMsg(sendMsg);

                        string sendStr = "";

                        for (int i = errorList.Count - 1; i >= 0; i--)
                        {
                            StructErrorData errorData = errorList[i];
                            sendStr += errorData.ID + "," + errorData.Priority + "," + errorData.Command + "," + errorData.DateTime + "," + errorData.IsCleared.ToString() + "," + errorData.ClearDateTime + ",";
                        }

                        SendErrorMsg(sendStr);
                    }
                    else if (command == "ACCELERATE_MODE_ON")
                    {
                        try
                        {
                            SetJogXSpeed(30);
                            SetJogZSpeed(30);
                            SendCommandReply(sender, "RPY_ACCELERATE_MODE_ON,", 0x00);
                        }
                        catch
                        {
                            SendCommandReply(sender, "RPY_ACCELERATE_MODE_ON,", 0xff);
                        }
                    }
                    else if (command == "ACCELERATE_MODE_OFF")
                    {
                        try
                        {
                            SetJogXSpeed(10);
                            SetJogZSpeed(10);
                            SendCommandReply(sender, "RPY_ACCELERATE_MODE_OFF,", 0x00);
                        }
                        catch
                        {
                            SendCommandReply(sender, "RPY_ACCELERATE_MODE_OFF,", 0xff);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            })).Start();
        }

        //
        private void SendCommandReply(object sender, string command, byte result)
        {
            AsyncFrameSocket.AsyncSocketClient sock = sender as AsyncFrameSocket.AsyncSocketClient;
            string rpyStr = command;
            List<byte> buffer = new List<byte>(Encoding.ASCII.GetBytes(rpyStr));
            buffer.Add(result);

            Console.WriteLine("PC to Phone : " + command + " / " + Convert.ToInt32(result));
            sock.Send(buffer.ToArray());
        }

        private void GocatorManager_OnCaptureEvent(short[] data, long width, long length, double xResolution, double yResolution, double zResolution)
        {
            try
            {
                if (inspectionInfo != null)
                {

                    if (inspectionInfo.width < width)
                    {
                        inspectionInfo.data = data;
                        inspectionInfo.width = width;
                        inspectionInfo.length = length;
                        inspectionInfo.xResolution = xResolution;
                        inspectionInfo.yResolution = yResolution;
                        inspectionInfo.zResolution = zResolution;
                    }

                    if (width * xResolution >= InspectionInfo.scanXDistance - 100)
                    {
                        bool final = false;
                    }

                    double minZ = short.MaxValue;
                    double maxZ = short.MinValue;

                    for (int k = 0; k < length; k++)
                    {
                        for (int p = 0; p < width; p++)
                        {
                            short z = data[k * width + p];

                            if (z != -32768)
                            {
                                if (minZ > z)
                                {
                                    minZ = z;
                                }
                                if (maxZ < z)
                                {
                                    maxZ = z;
                                }
                            }
                        }

                    }

                    PixelFormat pf = PixelFormats.Gray8;
                    long rawStride = (width * pf.BitsPerPixel + 7) / 8;
                    byte[] rawImage = new byte[rawStride * length];

                    byte[] greyRaw = new byte[rawImage.Length];

                    double distanceZ = maxZ - minZ;

                    for (int k = 0; k < data.Length; k++)
                    {
                        if (data[k] == -32768)
                        {
                            greyRaw[k] = 0;
                        }
                        else
                        {

                            double z = ((double)(data[k] / distanceZ * 255));
                            byte[] byteValue = BitConverter.GetBytes(((int)z));
                            greyRaw[k] = byteValue[0];
                        }
                    }

                    BitmapSource bitmap = BitmapSource.Create((int)width, (int)length,
                        96, 96, pf, null,
                        greyRaw, (int)rawStride);

                    bitmap.Freeze();

                    window.Dispatcher.Invoke(() =>
                    {
                        ScanImage = bitmap;
                        // bitmap.ToMat().SaveImage(zds + ".bmp");
                        zds++;
                    });
                }
            }
            catch (Exception e)
            {
                LogManager.Error("[GocatorManager_OnCaptureEvent] 에러 발생 : " + e.Message);
            }
        }

        int zds = 0;

        void InitPlcTick()
        {
            new Thread(new ThreadStart(() =>
            {
                ushort id = 0;
                while (isClosing == false)
                {
                    try
                    {
                        //Input Receive
                        id++;
                        readPlcManager.ReadDw(inputValues["HB"], id, 22);

                        //readPlcManager.ReadDw("%DW9590", id, 22);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        bool isSuccess = false;
                        while (sw.ElapsedMilliseconds < 1000)
                        {
                            if ((readPlcManager.LastReadReceiveData != null && readPlcManager.LastReadReceiveData.ID == id))
                            {
                                isSuccess = true;
              
                                break;
                            }
                        }

                        if (isSuccess)
                        {
                            UpdateReceiveInputData(readPlcManager.LastReadReceiveData);
                        }
                        else
                        {
                            try
                            {
                                readPlcManager.Close();
                            }
                            catch
                            {

                            }

                            readPlcManager.Connect();
                        }

                        //Thread.Sleep(100);

                        //output Receive
                        id++;
                        //readPlcManager.ReadDw(inputValues["HB"], id, 22);

                        readPlcManager.ReadDw("%DW9500", id, 23);
                        sw = new Stopwatch();
                        sw.Reset();
                        sw.Start();
                        isSuccess = false;
                        while (sw.ElapsedMilliseconds < 1000)
                        {
                            if ((readPlcManager.LastReadReceiveData != null && readPlcManager.LastReadReceiveData.ID == id))
                            {
                                isSuccess = true;

                                break;
                            }
                        }

                        if (isSuccess)
                        {
                            UpdateReceiveOutputData1(readPlcManager.LastReadReceiveData);
                        }
                        else
                        {
                            try
                            {
                                readPlcManager.Close();
                            }
                            catch
                            {

                            }

                            readPlcManager.Connect();
                        }

                        Thread.Sleep(100);
                     
                        readPlcManager.ReadDw("%DW9590", id, 10);
                        sw = new Stopwatch();
                        sw.Reset();
                        sw.Start();
                        isSuccess = false;

                        while (sw.ElapsedMilliseconds < 1000)
                        {
                            if ((readPlcManager.LastReadReceiveData != null && readPlcManager.LastReadReceiveData.ID == id))
                            {
                                isSuccess = true;

                                break;
                            }
                        }

                        if (isSuccess)
                        {
                            UpdateReceiveOutputData2(readPlcManager.LastReadReceiveData);
                        }
                        else
                        {
                            try
                            {
                                readPlcManager.Close();
                            }
                            catch
                            {

                            }

                            readPlcManager.Connect();
                        }

                        Thread.Sleep(100);
                    }
                    catch
                    {

                    }
                }
            })).Start();
        }

        private void SendErrorMsg(string msgStr)
        {
            if (phoneCommandClient != null)
            {
                try
                {
                    string sendMsg = "RPY_ERROR_MSG," + msgStr;
                    phoneCommandClient.Send(Encoding.ASCII.GetBytes(sendMsg));
                }
                catch
                {

                }
            }
        }

        // 통신 상태 체크
        bool isPhoneConnected = true;
        bool isPhoneCommandConnected = true;

        private bool isEmergencyErrorChecked = false;
        private bool isNozelErrorChecked = false;
        private bool isXLimitErrorChecked = false;
        private bool isZLimitErrorChecked = false;
        private bool isServoMoveErrorChecked = false;
        private bool isPhoneServerErrorChecked = false;

        private string sendMsg = "";
        private bool isSendMsgChanged = false;

        public void UpdateReceiveInputData(PlcManager.ReceiveData receiveData)
        {
            int hb = receiveData.Data[0];
            int nozel1Forword = receiveData.Data[1];
            int nozel2Forword = receiveData.Data[2];
            int nozel3Forword = receiveData.Data[3];
            int nozel1Backword = receiveData.Data[4];
            int nozel2Backword = receiveData.Data[5];
            int nozel3Backword = receiveData.Data[6];
            int pumpOn = receiveData.Data[7];
            int emergency = receiveData.Data[8];
            int moveReady = receiveData.Data[9];
            int cleanFinish = receiveData.Data[10];
            ushort cleanState = receiveData.Data[11];

            byte[] xPositionBytes1 = BitConverter.GetBytes(receiveData.Data[12]);
            byte[] xPositionBytes2 = BitConverter.GetBytes(receiveData.Data[13]);
            byte[] xPositionByte = new byte[] { xPositionBytes1[0], xPositionBytes1[1], xPositionBytes2[0], xPositionBytes2[1] };
            double xPosition = BitConverter.ToInt32(xPositionByte, 0) * 1.0 / 100;

            byte[] zPositionBytes1 = BitConverter.GetBytes(receiveData.Data[14]);
            byte[] zPositionBytes2 = BitConverter.GetBytes(receiveData.Data[15]);
            byte[] zPositionByte = new byte[] { zPositionBytes1[0], zPositionBytes1[1], zPositionBytes2[0], zPositionBytes2[1] };
            double zPosition = BitConverter.ToInt32(zPositionByte, 0) * 1.0 / 100;

            int xLimitError = receiveData.Data[16];
            int zLimitError = receiveData.Data[17];

            int nozleErrorState = receiveData.Data[18];

            double xServoLoad = receiveData.Data[20] / 1000;
            double zServoLoad = receiveData.Data[21] / 1000;

            window.Dispatcher.Invoke(() =>
            {
                CurrentMotorX = xPosition.ToString();
                CurrentMotorZ = zPosition.ToString();
                if (moveReady == 1)
                {
                    State = "이동 가능";
                }
                else
                {
                    State = "작업 중";
                }
                CleaningState = cleanState.ToString();
                IsCleaningFinish = cleanFinish.ToString();
                NozleErrorState = nozleErrorState.ToString();

                Input_HB = hb.ToString();
                Input_Nozle1Forword = nozel1Forword.ToString();
                Input_Nozle2Forword = nozel2Forword.ToString();
                Input_Nozle3Forword = nozel3Forword.ToString();
                Input_Nozle1Backword = nozel1Backword.ToString();
                Input_Nozle2Backword = nozel2Backword.ToString();
                Input_Nozle3Backword = nozel3Backword.ToString();

                if (nozel1Forword == 1)
                {
                    Nozle1ForwordLEDColor = Brushes.Green;
                }
                else
                {
                    Nozle1ForwordLEDColor = Brushes.Red;
                }
                if (nozel2Forword == 1)
                {
                    Nozle2ForwordLEDColor = Brushes.Green;
                }
                else
                {
                    Nozle2ForwordLEDColor = Brushes.Red;
                }
                if (nozel3Forword == 1)
                {
                    Nozle3ForwordLEDColor = Brushes.Green;
                }
                else
                {
                    Nozle3ForwordLEDColor = Brushes.Red;
                }

                if (nozel1Backword == 1)
                {
                    Nozle1BackwordLEDColor = Brushes.Green;
                }
                else
                {
                    Nozle1BackwordLEDColor = Brushes.Red;
                }
                if (nozel2Backword == 1)
                {
                    Nozle2BackwordLEDColor = Brushes.Green;
                }
                else
                {
                    Nozle2BackwordLEDColor = Brushes.Red;
                }
                if (nozel3Backword == 1)
                {
                    Nozle3BackwordLEDColor = Brushes.Green;
                }
                else
                {
                    Nozle3BackwordLEDColor = Brushes.Red;
                }

                Input_PumpOn = pumpOn.ToString();
                Input_Emergency = emergency.ToString();

                if (input_Emergency == "1" && !isEmergencyErrorChecked)
                {
                    isEmergencyErrorChecked = true;
                    StructErrorData errorData = new StructErrorData();
                    errorData.ID = errorDataID++;
                    errorData.DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    errorData.Title = "비상 정지";
                    errorData.Content = "비상 정지 스위치가 눌린 상태입니다.";
                    errorData.Command = "EMERGENCY";
                    errorData.ActionContent = "- 비상 정지 스위치를 오른쪽으로 돌려 복귀하세요.";
                    errorData.Priority = StructErrorData.ErrorPriority.High.ToString();
                    ErrorList.Insert(0, errorData);
                }
                else if (input_Emergency == "0" && isEmergencyErrorChecked)
                {
                    for (int i = 0; i < ErrorList.Count; i++)
                    {
                        if (ErrorList[i].Title == "비상 정지" && !ErrorList[i].IsCleared)
                        {
                            ErrorList[i].ClearDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            ErrorList[i].IsCleared = true;
                            isEmergencyErrorChecked = false;
                            break;
                        }
                    }
                }

                Input_MoveReady = moveReady.ToString();

                if (Input_MoveReady == "1")
                {
                    isOriginMoving = false;
                }

                if (input_MoveReady == "1" && isServoMoveErrorChecked)
                {
                    for (int i = 0; i < ErrorList.Count; i++)
                    {
                        if (ErrorList[i].Title == "서보 위치 이동 시간 경과 발생" && !ErrorList[i].IsCleared)
                        {
                            ErrorList[i].ClearDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            ErrorList[i].IsCleared = true;
                            isServoMoveErrorChecked = false;
                            moveSw.Stop();
                            moveSw.Reset();
                            break;
                        }
                    }
                }
                else if (input_MoveReady == "0" && !isServoMoveErrorChecked)
                {
                    if (moveSw.ElapsedMilliseconds / 1000 > 180)
                    {
                        isServoMoveErrorChecked = true;
                        StructErrorData errorData = new StructErrorData();
                        errorData.ID = errorDataID++;
                        errorData.DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        errorData.Title = "서보 위치 이동 시간 경과 발생";
                        errorData.Command = "SERVO_MOVE";
                        errorData.Content = "서보 운전 시간이 설정된 시간보다 경과한 상태입니다.";
                        errorData.ActionContent = "- 서보 이동에 장애 요소가 있는지 확인하세요." + Environment.NewLine + "- 운전 시간, 속도 설정 값 확인하세요.";
                        errorData.Priority = StructErrorData.ErrorPriority.Middle.ToString();

                        ErrorList.Insert(0, errorData);
                    }
                }

                Input_CleaningFinish = cleanFinish.ToString();
                Input_CleaningState = cleanState.ToString();
                Input_XPos = xPosition.ToString();
                Input_ZPos = zPosition.ToString();

                Input_XLimitError = xLimitError.ToString();

                if (input_XLimitError == "1" && !isXLimitErrorChecked)
                {
                    isXLimitErrorChecked = true;
                    StructErrorData errorData = new StructErrorData();
                    errorData.ID = errorDataID++;
                    errorData.DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    errorData.Title = "X축 서보 알람 발생";
                    errorData.Command = "SERVO_X";
                    errorData.Content = "X축 서보 모터 알람 발생 했습니다.";
                    errorData.ActionContent = "- 에러 내역 번호 확인 후 문의하세요.";
                    errorData.Priority = StructErrorData.ErrorPriority.Middle.ToString();

                    ErrorList.Insert(0, errorData);
                }
                else if (input_XLimitError == "0" && isXLimitErrorChecked)
                {
                    for (int i = 0; i < ErrorList.Count; i++)
                    {
                        if (ErrorList[i].Title == "X축 서보 알람 발생" && !ErrorList[i].IsCleared)
                        {
                            ErrorList[i].ClearDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            ErrorList[i].IsCleared = true;
                            isXLimitErrorChecked = false;
                            break;
                        }
                    }
                }

                Input_ZLimitError = zLimitError.ToString();

                if (input_ZLimitError == "1" && !isZLimitErrorChecked)
                {
                    isZLimitErrorChecked = true;
                    StructErrorData errorData = new StructErrorData();
                    errorData.ID = errorDataID++;
                    errorData.DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    errorData.Title = "Z축 서보 알람 발생";
                    errorData.Command = "SERVO_Z";
                    errorData.Content = "Z축 서보 모터 알람 발생 했습니다.";
                    errorData.ActionContent = "- 에러 내역 번호 확인 후 문의하세요.";
                    errorData.Priority = StructErrorData.ErrorPriority.Middle.ToString();

                    ErrorList.Insert(0, errorData);
                }
                else if (input_ZLimitError == "0" && isZLimitErrorChecked)
                {
                    for (int i = 0; i < ErrorList.Count; i++)
                    {
                        if (ErrorList[i].Title == "Z축 서보 알람 발생" && !ErrorList[i].IsCleared)
                        {
                            ErrorList[i].ClearDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            ErrorList[i].IsCleared = true;
                            isZLimitErrorChecked = false;
                            break;
                        }
                    }
                }

                Input_NozleCheckError = nozleErrorState.ToString();

                if (input_NozleCheckError == "1" && !isNozelErrorChecked)
                {
                    isNozelErrorChecked = true;
                    StructErrorData errorData = new StructErrorData();
                    errorData.ID = errorDataID++;
                    errorData.DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    errorData.Title = "노즐 이상";
                    errorData.Command = "NOZZLE";
                    errorData.Content = "노즐 전진 및 후진 시간이 설정된 시간보다 경과한 상태입니다.";
                    errorData.ActionContent = "- 노즐 상태 확인하세요." + Environment.NewLine + "- 감지 센서 확인하세요." + Environment.NewLine + "- 노즐 전진, 후진 시 이송에 장애 요소가 있는지 확인하세요.";
                    errorData.Priority = StructErrorData.ErrorPriority.Middle.ToString();

                    ErrorList.Insert(0, errorData);
                }
                else if (input_NozleCheckError == "0" && isNozelErrorChecked)
                {
                    for (int i = 0; i < ErrorList.Count; i++)
                    {
                        if (ErrorList[i].Title == "노즐 이상" && !ErrorList[i].IsCleared)
                        {
                            ErrorList[i].ClearDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            ErrorList[i].IsCleared = true;
                            isNozelErrorChecked = false;
                            break;
                        }
                    }
                }

                Input_XServoLoad = xServoLoad.ToString();
                Input_ZServoLoad = zServoLoad.ToString();

                if (phoneClient != null)
                {
                    if (!phoneClient.IsAliveSocket())
                    {
                        phoneClient.Close();
                        phoneClient = null;
                        isPhoneConnected = false;
                    }
                    else
                    {
                        isPhoneConnected = true;
                    }
                }

                if (phoneCommandClient != null)
                {
                    if (!phoneCommandClient.IsAliveSocket())
                    {
                        phoneCommandClient.Close();
                        phoneCommandClient = null;
                        isPhoneCommandConnected = false;
                    }
                    else
                    {
                        isPhoneCommandConnected = true;
                    }
                }

                if (!isPhoneConnected && !isPhoneCommandConnected && !isPhoneServerErrorChecked)
                {
                    isPhoneServerErrorChecked = true;
                    StructErrorData errorData = new StructErrorData();
                    errorData.ID = errorDataID++;
                    errorData.DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    errorData.Title = "통신 연결 이상 발생";
                    errorData.Command = "WIFI";
                    errorData.Content = "태블릿 WiFi 연결이 끊긴 상태입니다.";
                    errorData.ActionContent = "- WiFi 연결 상태를 확인하세요.";
                    errorData.Priority = StructErrorData.ErrorPriority.Low.ToString();

                    ErrorList.Insert(0, errorData);
                }
                else if (isPhoneConnected && isPhoneCommandConnected && isPhoneServerErrorChecked)
                {
                    for (int i = 0; i < ErrorList.Count; i++)
                    {
                        if (ErrorList[i].Title == "통신 연결 이상 발생" && !ErrorList[i].IsCleared)
                        {
                            ErrorList[i].ClearDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            ErrorList[i].IsCleared = true;
                            isPhoneServerErrorChecked = false;
                            break;
                        }
                    }
                }

                // 이상 내역이 있는지 체크
                bool isChecked = false;

                for (int i = 0; i < ErrorList.Count; i++)
                {
                    if (!ErrorList[i].IsCleared)
                    {
                        isChecked = true;

                        switch (ErrorList[i].Title)
                        {
                            case "비상 정지":
                                if (sendMsg != "EMERGENCY")
                                {
                                    sendMsg += "EMERGENCY";
                                    isSendMsgChanged = true;
                                }
                                else
                                {
                                    isSendMsgChanged = false;
                                }

                                break;
                            case "X축 서보 알람 발생":
                                if (sendMsg != "SERVO_X")
                                {
                                    sendMsg = "SERVO_X";
                                    isSendMsgChanged = true;
                                }
                                else
                                {
                                    isSendMsgChanged = false;
                                }

                                break;
                            case "Z축 서보 알람 발생":
                                if (sendMsg != "SERVO_Z")
                                {
                                    sendMsg = "SERVO_Z";
                                    isSendMsgChanged = true;
                                }
                                else
                                {
                                    isSendMsgChanged = false;
                                }

                                break;
                            case "서보 위치 이동 시간 경과 발생":
                                if (sendMsg != "SERVO_MOVE")
                                {
                                    sendMsg = "SERVO_MOVE";
                                    isSendMsgChanged = true;
                                }
                                else
                                {
                                    isSendMsgChanged = false;
                                }

                                break;
                            case "노즐 이상":
                                if (sendMsg != "NOZZLE")
                                {
                                    sendMsg = "NOZZLE";
                                    isSendMsgChanged = true;
                                }
                                else
                                {
                                    isSendMsgChanged = false;
                                }

                                break;
                            default:
                                break;
                        }

                        break;
                    }
                }

                // 이상 내역 없으면 전달
                if (!isChecked)
                {
                    if (sendMsg != "DEFAULT")
                    {
                        sendMsg = "DEFAULT";
                        isSendMsgChanged = true;
                    }
                    else
                    {
                        isSendMsgChanged = false;
                    }
                }
            });
        }

        public void UpdateReceiveOutputData1(PlcManager.ReceiveData receiveData)
        {
            int hb = receiveData.Data[0];

            byte[] xPositionBytes1 = BitConverter.GetBytes(receiveData.Data[1]);
            byte[] xPositionBytes2 = BitConverter.GetBytes(receiveData.Data[2]);
            byte[] xPositionByte = new byte[] { xPositionBytes1[0], xPositionBytes1[1], xPositionBytes2[0], xPositionBytes2[1] };
            double xPosition = BitConverter.ToInt32(xPositionByte, 0) * 1.0 / 100;

            byte[] zPositionBytes1 = BitConverter.GetBytes(receiveData.Data[3]);
            byte[] zPositionBytes2 = BitConverter.GetBytes(receiveData.Data[4]);
            byte[] zPositionByte = new byte[] { zPositionBytes1[0], zPositionBytes1[1], zPositionBytes2[0], zPositionBytes2[1] };
            double zPosition = BitConverter.ToInt32(zPositionByte, 0) * 1.0 / 100;

            int moveStart = receiveData.Data[5];
            int pumpOn = receiveData.Data[6];
            int cleanStart = receiveData.Data[7];
            int nozleSelect = receiveData.Data[8];
            int nozleForwordTime = receiveData.Data[9];
            int nozleBackwordTime = receiveData.Data[10];
            int moveRight = receiveData.Data[11];
            int moveLeft = receiveData.Data[12];
            int moveDown = receiveData.Data[13];
            int moveUp = receiveData.Data[14];
            int xJogSpeed = receiveData.Data[15];
            int zJogSpeed = receiveData.Data[16];
            int xMoveSpeed = receiveData.Data[17];
            int zMoveSpeed = receiveData.Data[18];
            int setOrigin = receiveData.Data[19];
            int alamReset = receiveData.Data[20];
            int nozleForword = receiveData.Data[21];
            int nozleBackword = receiveData.Data[22];

            window.Dispatcher.Invoke(() =>
            {
                Output_HB = hb.ToString();
                Output_XDesPos = xPosition.ToString();
                Output_ZDesPos = zPosition.ToString();
                Output_StartMove = moveStart.ToString();
                Output_PumpOn = pumpOn.ToString();
                Output_StartCleaning = cleanStart.ToString();
                Output_NozleSelect = nozleSelect.ToString();
                Output_NozleForwordLimit = nozleForwordTime.ToString();
                Output_NozleBackwordLimit = nozleBackwordTime.ToString();
                Output_MoveRight = moveRight.ToString();
                Output_MoveLeft = moveLeft.ToString();
                Output_MoveDown = moveDown.ToString();
                Output_MoveUp = moveUp.ToString();
                Output_XJogSpeed = xJogSpeed.ToString();
                Output_ZJogSpeed = zJogSpeed.ToString();
                Output_XMoveSpeed = xMoveSpeed.ToString();
                Output_ZMoveSpeed = zMoveSpeed.ToString();
                Output_SetOrigin = setOrigin.ToString();
                Output_AlramReset = alamReset.ToString();
                Output_NozleForword = nozleForword.ToString();
                Output_NozleBackword = nozleBackword.ToString();
            });
        }

        public void UpdateReceiveOutputData2(PlcManager.ReceiveData receiveData)
        {
            int freeMove = receiveData.Data[0];
             
            window.Dispatcher.Invoke(() =>
            {
                Output_FreeMove = freeMove.ToString();
            });
        }

        public void SetJogDown()
        {
            if(Convert.ToDouble(currentMotorZ) < zMaxLimit)
            {
                writePlcManager.WriteDW(outputValues["MOVE DOWN"], new byte[] { 0x01, 0x00 });
            }
 
        }

        public void SetJogUp()
        {
            if (Convert.ToDouble(currentMotorZ) > zMinLimit)
            {
                writePlcManager.WriteDW(outputValues["MOVE UP"], new byte[] { 0x01, 0x00 });
            }
        }

        public void SetJogLeft()
        {
            if (Convert.ToDouble(currentMotorX) > xMinLimit)
            {
                writePlcManager.WriteDW(outputValues["MOVE LEFT"], new byte[] { 0x01, 0x00 });
            }
        }

        public void SetJogRight()
        {
            if (Convert.ToDouble(currentMotorZ) < xMaxLimit)
            {
                writePlcManager.WriteDW(outputValues["MOVE RIGHT"], new byte[] { 0x01, 0x00 });
            }
        }

        public void ReleaseJogDown()
        {
            writePlcManager.WriteDW(outputValues["MOVE DOWN"], new byte[] { 0x00, 0x00 });
        }

        public void ReleaseJogUp()
        {
            writePlcManager.WriteDW(outputValues["MOVE UP"], new byte[] { 0x00, 0x00 });
        }

        public void ReleaseJogLeft()
        {
            writePlcManager.WriteDW(outputValues["MOVE LEFT"], new byte[] { 0x00, 0x00 });
        }

        public void ReleaseJogRight()
        {
            writePlcManager.WriteDW(outputValues["MOVE RIGHT"], new byte[] { 0x00, 0x00 });
        }
         
        public void SetOrigin()
        {
            finishOriginSetting = true;
            writePlcManager.WriteDW(outputValues["SET ORIGIN"], new byte[] { 0x01, 0x00 });
            xMinLimit = double.MinValue;
            xMaxLimit = double.MaxValue;

            zMinLimit = double.MinValue;
            zMaxLimit = double.MaxValue;

        }

        public void ReleaseOrigin()
        {
            writePlcManager.WriteDW(outputValues["SET ORIGIN"], new byte[] { 0x00, 0x00 });
        }

        public void SetJogXSpeed(ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            writePlcManager.WriteDW(outputValues["X JOG SPEED"], new byte[] { bytes[0], bytes[1] });
        }

        public void SetJogZSpeed(ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            writePlcManager.WriteDW(outputValues["Z JOG SPEED"], new byte[] { bytes[0], bytes[1] });
        }

        public void SetMoveXSpeed(ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            writePlcManager.WriteDW(outputValues["X MOVE SPEED"], new byte[] { bytes[0], bytes[1] });
        }

        public void SetMoveZSpeed(ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            writePlcManager.WriteDW(outputValues["Z MOVE SPEED"], new byte[] { bytes[0], bytes[1] });
        }


        public void NozleOn()
        {
            writePlcManager.WriteDW(outputValues["NOZLE SELECT"], new byte[] { 0x01, 0x00 });
        }
        public void NozleOff()
        {
            writePlcManager.WriteDW(outputValues["NOZLE SELECT"], new byte[] { 0x00, 0x00 });
        }

        public void SetNozleForwordTime()
        {
            byte[] bytes = BitConverter.GetBytes(nozleForwordTime);

            writePlcManager.WriteDW(outputValues["NOZLE FORWORD TIME"], new byte[] { bytes[0], bytes[1] });
        }

        public void SetNozleBackwordTime()
        {
            byte[] bytes = BitConverter.GetBytes(nozleBackwordTime);

            writePlcManager.WriteDW(outputValues["NOZLE BACKWORD TIME"], new byte[] { bytes[0], bytes[1] });
        }

        public void MoveInterLockOn()
        {
            writePlcManager.WriteDW(outputValues["FREE MOVE"], new byte[] { 0x01, 0x00 });
        }

        public void MoveInterLockOff()
        {
            writePlcManager.WriteDW(outputValues["FREE MOVE"], new byte[] { 0x00, 0x00 });
        }

        public void NozleForwordOn()
        {
             
            writePlcManager.WriteDW(outputValues["NOZLE FORWORD"], new byte[] { 0x01, 0x00 });
        }

        public void NozleForwordOff()
        {

            writePlcManager.WriteDW(outputValues["NOZLE FORWORD"], new byte[] { 0x00, 0x00 });
        }

        public void NozleBackwordOn()
        {

            writePlcManager.WriteDW(outputValues["NOZLE BACKWORD"], new byte[] { 0x01, 0x00 });
        }

        public void NozleBackwordOff()
        {

            writePlcManager.WriteDW(outputValues["NOZLE BACKWORD"], new byte[] { 0x00, 0x00 });
        }

        public void NozleStartCleaningOn()
        {

            writePlcManager.WriteDW(outputValues["CLEAN START"], new byte[] { 0x01, 0x00 });
        }


        public void NozleStartCleaningOff()
        {

            writePlcManager.WriteDW(outputValues["CLEAN START"], new byte[] { 0x00, 0x00 });
        }

        private Stopwatch moveSw = new Stopwatch();


        /// <summary>
        /// 단위 0.01mm
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        public void Move(int x, int z)
        {


            byte[] xBytes = BitConverter.GetBytes(x);
            byte[] zBytes = BitConverter.GetBytes(z);

            writePlcManager.WriteDW(outputValues["X POSITION 1"], new byte[] { xBytes[0], xBytes[1] });
            writePlcManager.WriteDW(outputValues["X POSITION 2"], new byte[] { xBytes[2], xBytes[3] });

            writePlcManager.WriteDW(outputValues["Z POSITION 1"], new byte[] { zBytes[0], zBytes[1] });
            writePlcManager.WriteDW(outputValues["Z POSITION 2"], new byte[] { zBytes[2], zBytes[3] });

            writePlcManager.WriteDW(outputValues["MOVE START"], new byte[] { 0x01, 0x00 });

            moveSw.Start();
        }

        public void EndMove()
        {
            writePlcManager.WriteDW(outputValues["MOVE START"], new byte[] { 0x00, 0x00 });
        }

        public void SetOriginLeftTop()
        {

            SetOrigin();

            new Thread(new ThreadStart(() =>
            {
               
                OriginLeftTopX = 0;
                OriginLeftTopZ = 0;

                Thread.Sleep(100);

                ReleaseOrigin();
            })).Start();

        }

        public void SetOriginRightTop()
        {
            OriginRightTopX = Convert.ToDouble(CurrentMotorX);
            OriginRightTopZ = Convert.ToDouble(CurrentMotorZ);
        }

        public void SetOriginLeftBottom()
        {
            OriginLeftBottomX = Convert.ToDouble(CurrentMotorX);
            OriginLeftBottomZ = Convert.ToDouble(CurrentMotorZ);
        }

        public void SetOriginRightBottom()
        {
            OriginRightBottomX = Convert.ToDouble(CurrentMotorX);
            OriginRightBottomZ = Convert.ToDouble(CurrentMotorZ);
        }

        private bool isScanCompeleted = false;
        private bool isEmergencyOn = false;

        public void StartScan()
        {
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    LogManager.Action("[StartScan] 실행");

                    isScanCompeleted = false;

                    double scanXArea = 350;
                    double mergeXArea = 50;

                    double sensorScanBiasX = -80;
                    double sensorScanBiasZ = -80;

                    /*
                    double startX = originLeftTopX + sensorScanBiasX;
                    double startZ = originLeftTopZ + sensorScanBiasZ;
                    */

                    double startX = originLeftTopX;
                    double startZ = originLeftTopZ;

                    /*
                    double endX = originRightBottomX + sensorScanBiasX;
                    double endZ = OriginRightBottomZ + sensorScanBiasZ;
                    */

                    double endX = originRightBottomX;
                    double endZ = OriginRightBottomZ;

                    double xDistance = endX - startX;
                    double zDistance = endZ - startZ;

                    int stitchCount = (int)((xDistance) / scanXArea);
                    stitchCount++;

                    LogManager.Action("[StartScan] stitchCount : " + stitchCount);

                    bool isBottom = false;
                    //시작 위치로 이동
                    Move((int)(startX * 100), (int)(startZ * 100));


                    Thread.Sleep(500);
                    EndMove();

                    //센서 종료
                    try
                    {
                        gocatorManager.StopScan();
                        LogManager.Action("[StartScan] 센서 종료");
                    }
                    catch (Exception e)
                    {
                        LogManager.Error("[StartScan] 센서 종료 에러 : " + e.Message);
                    }


                    if (xDistance == 0)
                    {
                        xDistance = 1;
                    }

                    try
                    {
                        LogManager.Action("[StartScan] 고게이터 세팅");

                        //고게이터 세팅
                        gocatorManager.ScanPixedLength = (int)zDistance;
                        gocatorManager.EncoderLostRange = 20;

                        gocatorManager.StitchSurfaceCount = stitchCount;
                        gocatorManager.StitchSurfaceXOffset.Clear();
                    }
                    catch (Exception e)
                    {
                        LogManager.Action("[StartScan] 고게이터 세팅 에러 : " + e.Message);
                    }

                    Console.WriteLine("Stitch Count : " + gocatorManager.StitchSurfaceCount);


                    double temp = xDistance;
                    double offset = 0;
                    //Stitch offset 값 설정
                    for (int i = 0; i < stitchCount; i++)
                    {
                        
                        if(temp > scanXArea)
                        {
                            temp -= scanXArea;
                            offset += scanXArea;
                        }
                        else
                        {
                            offset += temp;
                        }

                        try
                        {
                            LogManager.Action("[StartScan] 고게이터 StitchSurfaceXOffset 추가");
                            gocatorManager.StitchSurfaceXOffset.Add((int)offset);
                        }
                        catch (Exception e)
                        {
                            LogManager.Error("[StartScan] 고게이터 StitchSurfaceXOffset 추가 에러 : " + e.Message);
                        }
                    }

                    iniConfig.WriteValue("Gocator Setting", "Trigger", "Encoder");
                    Console.WriteLine("Ready to Scan");

                    try
                    {
                        LogManager.Action("[StartScan] 고게이터 스캔 시작");
                        gocatorManager.StartScan(1000000);
                    }
                    catch (Exception e)
                    {
                        LogManager.Error("[StartScan] 고게이터 스캔 시작 에러 : " + e.Message);
                    }

                    while (true)
                    {
                        if (Convert.ToDouble(currentMotorX) == startX && Convert.ToDouble(currentMotorZ) == startZ)
                        {
                            break;
                        }
                        Thread.Sleep(500);
                    }



                    // InspectionInfo = new InspectionInfo();
                    InspectionInfo.scanXDistance = xDistance;
                    InspectionInfo.scanZDistance = zDistance;

                    double desX = startX;
                    temp = xDistance;
                    while(temp > 0)
                    {
                        double desZ = 0;
                        //상단에서 시작할 경우 z위치 설정
                        if(isBottom == false)
                        {
                            desZ = endZ;
                            isBottom = true;
                        }
                        //하단에서 시작할 경우 z위치 설정
                        else
                        {
                            desZ = startZ;
                            isBottom = false;
                        }

                        //이동
                        Move((int)Math.Round(desX * 100, 0), (int)Math.Round(desZ * 100, 0));
                        Thread.Sleep(500);
                        EndMove();

                        //이동 대기
                        while (true)
                        {
                            if (Math.Round(Convert.ToDouble(currentMotorX), 0) == Math.Round(desX, 0) && Math.Round(Convert.ToDouble(currentMotorZ), 0) == Math.Round(desZ, 0))
                            {
                                break;
                            }
                            Thread.Sleep(500);
                        }

                        //우측으로 이동
                        temp -= scanXArea;
                        if(temp > 0)
                        {
                            if(temp > scanXArea)
                            {
                                desX += scanXArea;
                            }
                            else
                            {
                                desX += temp;
                            }

                            //이동
                            Move((int)Math.Round(desX * 100, 0), (int)Math.Round(desZ * 100, 0));
                            Thread.Sleep(500);
                            EndMove();

                            //이동 대기
                            while (true)
                            {
                                if (Math.Round(Convert.ToDouble(currentMotorX), 0) == Math.Round(desX, 0) && Math.Round(Convert.ToDouble(currentMotorZ), 0) == Math.Round(desZ, 0))
                                {
                                    break;
                                }
                                Thread.Sleep(500);
                            }
                        }
                    }
                     
                    Move((int)(0 * 100), (int)(0 * 100));
                    Thread.Sleep(500);
                    EndMove();

                    Doinspection();

                    try
                    {
                        gocatorManager.StopScan();
                        LogManager.Action("[StartScan] 고게이터 스캔 종료");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed To stop Gocator");
                        LogManager.Error("[StartScan] 고게이터 스캔 종료 에러 : " + e.Message);
                    }
                    try
                    {
                        OpenCvSharp.Mat mat = ScanImage.ToMat();
 
                        inspectionInfo.IsFinishScanning = true;
                    }
                    catch
                    {
                        Console.WriteLine("Failed To Create Image");
                    }

                    while (Input_MoveReady != "1")
                    {
                        if (Input_Emergency == "1")
                        {
                            isEmergencyOn = true;
                        }
                        else if (Input_Emergency == "0")
                        {
                            if (isEmergencyOn)
                            {
                                isEmergencyOn = false;
                                Move((int)(0 * 100), (int)(0 * 100));
                                Thread.Sleep(500);
                                EndMove();
                            }
                        }

                        Thread.Sleep(10);
                    }

                    isScanCompeleted = true;
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            })).Start();
        }

        public void Doinspection()
        {
            try
            {
                //이미지 이진화
                BitmapSource binaryImage = InspectionManager.GetBinaryImage(inspectionInfo, 10, 10);
                ScanImage = binaryImage;

                //블랍 추출
                List<InspectionManager.StructBlob> blobs = InspectionManager.GetBlobs(binaryImage, 2000, 10000);
                for (int i = 0; i < blobs.Count; i++)
                {
                    InspectionManager.CircleCheck(blobs[i]);
                }

                //원 검증
                /*
                string dir = AppDomain.CurrentDomain.BaseDirectory + "\\" + "CenterBlobs";

                Directory.Delete(dir, true);
                if (Directory.Exists(dir) == false)
                {
                    Directory.CreateDirectory(dir);
                }

                for(int i = 0; i < blobs.Count; i++)
                {
                    if(blobs[i].centerResult.Count > 0)
                    {
                        blobs[i].mat.SaveImage(dir + "\\" + i + ".bmp");  
                    }
                }
                */

                //홀 위치 표시
                ScanImage = InspectionManager.DrawResult(scanImage, blobs);

                //blobs = blobs.OrderBy(x => x).ToList();

                //찾은 홀 표시



                double visionOriginX = iniConfig.GetDouble("Origin", "Vision X", 0); ;//60.38;
                double visionOriginY = iniConfig.GetDouble("Origin", "Vision Y", 0); ; //29.12;
                double motorBiasX = -visionOriginX;
                double motorBiasZ = -visionOriginY;
                List<InspectionManager.StructBlob> motorBlobs = new List<InspectionManager.StructBlob>();
                for (int i = 0; i < blobs.Count; i++)
                {
                    if (blobs[i].IsCircle)
                    {
                        motorBlobs.Add(blobs[i]);

                        double motorX = blobs[i].VisionX * InspectionInfo.xResolution;
                        double motorZ = blobs[i].VisionY * InspectionInfo.yResolution;

                        blobs[i].MotorX = motorX + motorBiasX;
                        blobs[i].MotorZ = motorZ + motorBiasZ;

                        blobs[i].centerX = blobs[i].centerX;
                        blobs[i].centerY = blobs[i].centerY;
                        blobs[i].startX = blobs[i].startX;
                        blobs[i].startY = blobs[i].startY;
                    }
                }

                InspectionInfo.FindCircle = new ObservableCollection<InspectionManager.StructBlob>(motorBlobs);

                window.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        //찾은 원을 찾은 홀로 넣기
                        inspectionInfo.Holes.Clear();
                        for (int i = 0; i < inspectionInfo.FindCircle.Count; i++)
                        {
                            StructHole hole = new StructHole();
                            hole.X = inspectionInfo.FindCircle[i].MotorX;
                            hole.Y = inspectionInfo.FindCircle[i].MotorZ;
                            hole.VisionX = InspectionInfo.FindCircle[i].VisionX * inspectionInfo.xResolution;
                            hole.VisionY = InspectionInfo.FindCircle[i].VisionY * inspectionInfo.yResolution;
                            // if (hole.Y > -10 && hole.Y < originRightBottomZ - 50 && hole.X > -50)
                            if (hole.Y > -10 && hole.X > -50)
                            {

                                inspectionInfo.Holes.Add(hole);
                            }

                        }

                        //홀 정렬 및 표시
                        List<StructHole> holes = InspectionInfo.Holes.ToList();
                        holes = holes.OrderBy(x => x.StartDistance).ToList();
                        //홀 정렬
                        holes = CleaningManager.SortHole(holes, 3, 20, 40);
                        holes = CleaningManager.CheckHole(holes);

                        InspectionInfo.Holes = new ObservableCollection<StructHole>(holes);

                        //화면 그리기
                        OpenCvSharp.Mat mat = binaryImage.ToMat();
                        int resizeRows = (int)(mat.Rows * inspectionInfo.yResolution);
                        int resizeCols = (int)(mat.Cols * inspectionInfo.xResolution);
                        mat = mat.Resize(new OpenCvSharp.Size(resizeCols, resizeRows));

                        InspectionInfo.HoleSettingOriginalImage = mat.ToBitmapSource();
                        InspectionInfo.HoleSettingImage = DrawSettingHoles(holes);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                });
            }
            catch (Exception e)
            {
                LogManager.Error("Doinspection Error : " + e.Message);
            }
        }

        public void MoveSemiAutoMode(object obj)
        {
            new Thread(new ThreadStart(() =>
            {
                InspectionManager.StructBlob hole = obj as InspectionManager.StructBlob;

                if (hole != null)
                {
                    double x = hole.MotorX;
                    double z = hole.MotorZ;
                    Move((int)(x * 100), (int)(z * 100));

                    Thread.Sleep(500);

                    EndMove();

                    while (true)
                    {
                        if ((int)Convert.ToDouble(currentMotorX) == (int)x && (int)Convert.ToDouble(currentMotorZ) == (int)z)
                        {
                            break;
                        }
                        Thread.Sleep(500);
                    }

                    Thread.Sleep(500);

                    window.Dispatcher.Invoke(() =>
                    {
                        hole.MotorXResult = Convert.ToDouble(CurrentMotorX);
                        hole.MotorZResult = Convert.ToDouble(CurrentMotorZ);
                    });
                }
            })).Start();
        }

        public void ConfirmHoleSetting()
        {
            StartAutoCleaning();
        }

        private BitmapSource DrawSettingHoles(List<StructHole> holes)
        {
            OpenCvSharp.Mat mat = InspectionInfo.HoleSettingOriginalImage.ToMat();
            try
            {
                mat = mat.CvtColor(OpenCvSharp.ColorConversionCodes.GRAY2RGB);
            }
            catch
            {

            }
            
            for(int i = 0; i < holes.Count; i++)
            {
                if(holes[i].IsTarget)
                {
                    mat.Circle(new OpenCvSharp.Point(holes[i].VisionX, holes[i].VisionY), 13, OpenCvSharp.Scalar.Yellow, thickness:3);
                }
                else
                {
                    mat.Circle(new OpenCvSharp.Point(holes[i].VisionX, holes[i].VisionY), 13, OpenCvSharp.Scalar.White, thickness:3);
                }
            }

            BitmapSource bitmapSource = BitmapSourceConverter.ToBitmapSource(mat);
            if(bitmapSource.CanFreeze)
            {
                bitmapSource.Freeze();
            }
            return bitmapSource;
        }

        bool stopCleaning = false;
        bool startCleaning = false;

        private void StartAutoCleaning()
        {
            stopCleaning = false;
            startCleaning = false;
            if (InspectionInfo != null)
            {
                InspectionInfo.CleaningStartDateTime = DateTime.Now;

                if (!isRelayMode)
                {
                    InspectionInfo.CleaningMaxCount = inspectionInfo.Holes.Where(x => x.IsTarget).ToList().Count;

                    if (!isRetryMode)
                    {
                        inspectionInfo.CleaningImage = inspectionInfo.HoleSettingImage;
                    }
                }
                else
                {
                    inspectionInfo.CleaningImage = inspectionInfo.HoleSettingImage;
                }
                
                new Thread(new ThreadStart(() =>
                {
                    while (!isScanCompeleted)
                    {
                        Thread.Sleep(10);
                    }

                    List<StructHole> desHoles = inspectionInfo.Holes.Where(x=>x.IsTarget).ToList();
                    for (inspectionInfo.CurrentCleaingHoleIndex = inspectionInfo.CurrentCleaingHoleIndex; inspectionInfo.CurrentCleaingHoleIndex < desHoles.Count && stopCleaning == false; inspectionInfo.CurrentCleaingHoleIndex++)
                    {
                        try
                        {
                            if (!startCleaning)
                            {
                                startCleaning = true;
                            }

                            // 이미 세척된 홀인지 체크 (중복 체크)
                            if (desHoles[inspectionInfo.CurrentCleaingHoleIndex].IsCleaningFinish)
                            {
                                bool isBeforeChecked = false;
                                bool isAfterChceked = false;

                                if (desHoles[inspectionInfo.CurrentCleaingHoleIndex].BeforePoint != null)
                                {
                                    if (desHoles[inspectionInfo.CurrentCleaingHoleIndex].BeforePoint.IsCleaningFinish)
                                    {
                                        if (desHoles[inspectionInfo.CurrentCleaingHoleIndex].BeforePoint.IsOK)
                                        {
                                            isBeforeChecked = true;
                                        }
                                        else
                                        {
                                            isBeforeChecked = false;
                                        }
                                    }
                                    else
                                    {
                                        isBeforeChecked = false;
                                    }
                                }

                                if (desHoles[inspectionInfo.CurrentCleaingHoleIndex].AfterPoint != null)
                                {
                                    if (desHoles[inspectionInfo.CurrentCleaingHoleIndex].AfterPoint.IsCleaningFinish)
                                    {
                                        if (desHoles[inspectionInfo.CurrentCleaingHoleIndex].AfterPoint.IsOK)
                                        {
                                            isAfterChceked = true;
                                        }
                                        else
                                        {
                                            isAfterChceked = false;
                                        }
                                    }
                                    else
                                    {
                                        isAfterChceked = false;
                                    }
                                }

                                if (isBeforeChecked && isAfterChceked)
                                {
                                    continue;
                                }
                            }

                            double x = desHoles[inspectionInfo.CurrentCleaingHoleIndex].X + OffsetX;
                            double y = desHoles[inspectionInfo.CurrentCleaingHoleIndex].Y + OffsetY;

                            //이동
                            Move((int)(x * 100), (int)(y * 100));
                            Thread.Sleep(500);
                            //이동 대기
                            EndMove();
                            while (true)
                            {
                                if (Input_Emergency == "1")
                                {
                                    isEmergencyOn = true;
                                }
                                else if (Input_Emergency == "0")
                                {
                                    if (isEmergencyOn)
                                    {
                                        isEmergencyOn = false;
                                        Move((int)(x * 100), (int)(y * 100));
                                        Thread.Sleep(500);
                                        //이동 대기
                                        EndMove();
                                    }
                                }

                                if ((int)Convert.ToDouble(currentMotorX) == (int)x && (int)Convert.ToDouble(currentMotorZ) == (int)y)
                                {
                                    break;
                                }
                                Thread.Sleep(500);
                            }
                            //세척 시작
                            NozleStartCleaningOn();
                            Thread.Sleep(500);
                            NozleStartCleaningOff();

                            //세척 완료 대기
                            while (input_MoveReady.StartsWith("0"))
                            {
                                Thread.Sleep(100);
                                Console.WriteLine("세척 완료 대기중");
                            }

                            //화면 표시
                            int width = 1000;
                            int height = 1500;

                            OpenCvSharp.Mat mat = null;

                            window.Dispatcher.Invoke(() =>
                            {
                                mat = inspectionInfo.CleaningImage.ToMat();
                            });

                            //세척 결과 표시
                            if (cleaningState == 7.ToString())
                            {
                                mat.Circle(new OpenCvSharp.Point(desHoles[inspectionInfo.CurrentCleaingHoleIndex].BeforePoint.VisionX, desHoles[inspectionInfo.CurrentCleaingHoleIndex].BeforePoint.VisionY), 13, OpenCvSharp.Scalar.Blue, thickness: 3);
                                desHoles[inspectionInfo.CurrentCleaingHoleIndex].BeforePoint.IsCleaningFinish = true;
                                desHoles[inspectionInfo.CurrentCleaingHoleIndex].BeforePoint.IsOK = true;
                            }
                            else
                            {
                                mat.Circle(new OpenCvSharp.Point(desHoles[inspectionInfo.CurrentCleaingHoleIndex].BeforePoint.VisionX, desHoles[inspectionInfo.CurrentCleaingHoleIndex].BeforePoint.VisionY), 13, OpenCvSharp.Scalar.Red, thickness: 3);
                                desHoles[inspectionInfo.CurrentCleaingHoleIndex].BeforePoint.IsCleaningFinish = true;
                                desHoles[inspectionInfo.CurrentCleaingHoleIndex].BeforePoint.IsOK = false;
                            }

                            if (cleaningState == 7.ToString())
                            {
                                mat.Circle(new OpenCvSharp.Point(desHoles[inspectionInfo.CurrentCleaingHoleIndex].VisionX, desHoles[inspectionInfo.CurrentCleaingHoleIndex].VisionY), 13, OpenCvSharp.Scalar.Blue, thickness: 3);
                                desHoles[inspectionInfo.CurrentCleaingHoleIndex].IsCleaningFinish = true;
                                desHoles[inspectionInfo.CurrentCleaingHoleIndex].IsOK = true;
                            }
                            else
                            {
                                mat.Circle(new OpenCvSharp.Point(desHoles[inspectionInfo.CurrentCleaingHoleIndex].VisionX, desHoles[inspectionInfo.CurrentCleaingHoleIndex].VisionY), 13, OpenCvSharp.Scalar.Red, thickness: 3);
                                desHoles[inspectionInfo.CurrentCleaingHoleIndex].IsCleaningFinish = true;
                                desHoles[inspectionInfo.CurrentCleaingHoleIndex].IsOK = false;
                            }

                            if (cleaningState == 7.ToString())
                            {
                                mat.Circle(new OpenCvSharp.Point(desHoles[inspectionInfo.CurrentCleaingHoleIndex].AfterPoint.VisionX, desHoles[inspectionInfo.CurrentCleaingHoleIndex].AfterPoint.VisionY), 13, OpenCvSharp.Scalar.Blue, thickness: 3);
                                desHoles[inspectionInfo.CurrentCleaingHoleIndex].AfterPoint.IsCleaningFinish = true;
                                desHoles[inspectionInfo.CurrentCleaingHoleIndex].AfterPoint.IsOK = true;
                            }
                            else
                            {
                                mat.Circle(new OpenCvSharp.Point(desHoles[inspectionInfo.CurrentCleaingHoleIndex].AfterPoint.VisionX, desHoles[inspectionInfo.CurrentCleaingHoleIndex].AfterPoint.VisionY), 13, OpenCvSharp.Scalar.Red, thickness: 3);
                                desHoles[inspectionInfo.CurrentCleaingHoleIndex].AfterPoint.IsCleaningFinish = true;
                                desHoles[inspectionInfo.CurrentCleaingHoleIndex].AfterPoint.IsOK = false;
                            }


                            double xBias = desHoles[inspectionInfo.CurrentCleaingHoleIndex].X - width / 2;
                            if (xBias > 0)
                            {
                                xBias = 0;
                            }

                            double yBias = desHoles[inspectionInfo.CurrentCleaingHoleIndex].Y - height / 2;
                            if (yBias > 0)
                            {
                                yBias = 0;
                            }

                            //double cropStartX = desHoles[i].VisionX - width / 2 - xBias;
                            //double cropStartY = desHoles[i].VisionY - height / 2 - yBias;

                            //if((int)cropStartX + width > mat.Cols)
                            //{
                            //    width = mat.Cols - (int)cropStartX;
                            //}

                            //if((int)cropStartY + height > mat.Rows)
                            //{
                            //    height = mat.Rows - (int)cropStartY;
                            //}

                            //OpenCvSharp.Mat cropMat = mat.Clone(new OpenCvSharp.Rect((int)cropStartX, (int)cropStartY, width, height));

                            //BitmapSource bitmapSource = cropMat.ToBitmapSource();
                            BitmapSource bitmapSource = mat.ToBitmapSource();
                            if (bitmapSource.CanFreeze)
                            {
                                bitmapSource.Freeze();
                            }
                            window.Dispatcher.Invoke((() =>
                            {
                                InspectionInfo.CleaningImage = bitmapSource;
                            }));

                            // InspectionInfo.CleaningCount++;
                            InspectionInfo.CleaningCount = desHoles.Count(dh => dh.IsCleaningFinish);

                            while (isPauseCleaning == true)
                            {
                                Thread.Sleep(1000);
                                Console.WriteLine("State : Pause");
                            }

                            Thread.Sleep(500);
                        }
                        catch
                        {

                        }
                    }

                    //세척 완료
                    inspectionInfo.CleaningEndDateTime = DateTime.Now;

                    try
                    {
                        string sendData = "FINISH_CLEANING,";
                        sendData += inspectionInfo.CleaningHoleCount;
                        sendData += ",";
                        sendData += inspectionInfo.CleaningOkHoleCount;
                        sendData += ",";
                        sendData += inspectionInfo.CleaningNGHoleCount;
                        sendData += ",";
                        sendData += inspectionInfo.CleaningStartDateTimeStr;
                        sendData += ",";
                        sendData += inspectionInfo.CleaningEndDateTimeStr;
                        sendData += ",";
                        sendData += inspectionInfo.CleaningTimeStr;

                        phoneCommandClient.Send(Encoding.ASCII.GetBytes(sendData));

                        // 이력 저장
                        string historyDir = iniConfig.GetString("History", "Path", "E:\\") + "Result";
                        if (!Directory.Exists(historyDir))
                        {
                            Directory.CreateDirectory(historyDir);
                        }

                        string imageDir = historyDir + "\\Image";

                        if (!Directory.Exists(imageDir))
                        {
                            Directory.CreateDirectory(imageDir);
                        }

                        imageDir += "\\" + DateTime.Now.ToString("yyyyMMdd");

                        if (!Directory.Exists(imageDir))
                        {
                            Directory.CreateDirectory(imageDir);
                        }

                        string dataDir = historyDir + "\\Data";

                        if (!Directory.Exists(dataDir))
                        {
                            Directory.CreateDirectory(dataDir);
                        }

                        dataDir += "\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + inspectionInfo.BundleName + "_" + DateTime.Now.ToString("HHmmss");

                        if (!Directory.Exists(dataDir))
                        {
                            Directory.CreateDirectory(dataDir);
                        }

                        window.Dispatcher.Invoke(() =>
                        {
                            inspectionInfo.appImage.ToMat().SaveImage(dataDir + "\\AppImage.jpg");
                            inspectionInfo.CleaningImage.ToMat().SaveImage(dataDir + "\\CleaningImage.jpg");
                            inspectionInfo.HoleSettingImage.ToMat().SaveImage(dataDir + "\\HoleSettingImage.jpg");
                            inspectionInfo.HoleSettingOriginalImage.ToMat().SaveImage(dataDir + "\\HoleSettingOriginalImage.jpg");

                            /*
                            string content = "세척 시작 시간 : " + inspectionInfo.CleaningStartDateTimeStr + Environment.NewLine + "세척 종료 시간 : " + inspectionInfo.CleaningEndDateTimeStr + Environment.NewLine + "총 전체 홀 개수 : " + inspectionInfo.CleaningHoleCount + Environment.NewLine;
                            File.WriteAllText(dataDir + "//InspectionInfo.data", content);
                            */

                            SaveFile(dataDir + "//InspectionInfo.data");
                        });
                    }
                    catch
                    {

                    }
                    



                })).Start();
            }
        }

        bool isManualLazerOn = false;
        public void ManualLazerOn()
        {
            if(isManualLazerOn == false)
            {
                iniConfig.WriteValue("Gocator Setting", "Trigger", "TIME");
                gocatorManager.StartScan(1000000);
                isManualLazerOn = true;
            }
            else
            {
                gocatorManager.StopScan();
                isManualLazerOn = false;
            }
        }

        public void AutoModeRemoveHole()
        {
            if(SelectedAutoModeHole != null)
            {
                InspectionInfo.Holes.Remove(SelectedAutoModeHole);
            }
        }

        public void StopCleaning()
        {
            stopCleaning = true;

            try
            {
                if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "InspectionData"))
                {
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "InspectionData");
                }

                string content = "";

                content += InspectionInfo.BundleName + Environment.NewLine;
                content += InspectionInfo.BundleLength + Environment.NewLine;
                content += InspectionInfo.IsUseNozle + Environment.NewLine;
                content += InspectionInfo.CleaningMaxCount + Environment.NewLine;
                content += InspectionInfo.CleaningCount + Environment.NewLine;
                content += InspectionInfo.CurrentCleaingHoleIndex + Environment.NewLine;
                content += InspectionInfo.IsFinishScanning + Environment.NewLine;
                content += InspectionInfo.IsFinishScanningIntensityImage + Environment.NewLine;
                content += InspectionInfo.scanXDistance + Environment.NewLine;
                content += InspectionInfo.scanZDistance + Environment.NewLine;
                content += InspectionInfo.xResolution + Environment.NewLine;
                content += InspectionInfo.zResolution + Environment.NewLine;
                content += InspectionInfo.Holes.Count + Environment.NewLine;
                content += originLeftTopX + Environment.NewLine;
                content += originLeftTopZ + Environment.NewLine;
                content += originRightBottomX + Environment.NewLine;
                content += originRightBottomZ + Environment.NewLine;

                for (int i = 0; i < InspectionInfo.Holes.Count; i++)
                {
                    StructHole structHole = InspectionInfo.Holes[i];
                    content += structHole.Index + "," + structHole.X + "," + structHole.Y + "," + structHole.VisionX + "," + structHole.VisionY + "," + structHole.GroupIndex + "," + structHole.Row + "," + structHole.Column + "," + structHole.IsCleaningFinish + "," + structHole.IsOK + "," + structHole.IsSortStartPoint + "," + structHole.IsTarget + "," + structHole.AfterDistance + Environment.NewLine;
                }

                try
                {
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "InspectionData\\inspectionData.data", content);

                    window.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            if (InspectionInfo.appImage != null)
                            {
                                InspectionInfo.appImage.ToMat().SaveImage(AppDomain.CurrentDomain.BaseDirectory + "InspectionData\\appImage.bmp");
                            }

                            if (InspectionInfo.CleaningImage != null)
                            {
                                InspectionInfo.CleaningImage.ToMat().SaveImage(AppDomain.CurrentDomain.BaseDirectory + "InspectionData\\cleaningImage.bmp");
                            }

                            if (InspectionInfo.HoleSettingImage != null)
                            {
                                InspectionInfo.HoleSettingImage.ToMat().SaveImage(AppDomain.CurrentDomain.BaseDirectory + "InspectionData\\holeSettingImage.bmp");
                            }

                            if (InspectionInfo.HoleSettingOriginalImage != null)
                            {
                                InspectionInfo.HoleSettingOriginalImage.ToMat().SaveImage(AppDomain.CurrentDomain.BaseDirectory + "InspectionData\\holeSettingOriginalImage.bmp");
                            }
                        }
                        catch
                        {

                        }
                    });
                }
                catch (Exception e)
                {
                    LogManager.Error("[StopCleaning] 내부 에러 : " + e.Message);
                }
            }
            catch (Exception e)
            {
                LogManager.Error("[StopCleaning] 에러 : " + e.Message);
            }
            
        }
         
        public void SkipOneHole()
        {
            inspectionInfo.CurrentCleaingHoleIndex++;
        }

        public void MoveHole(int idx)
        {
            inspectionInfo.CurrentCleaingHoleIndex = idx;
        }
         

        public void PumpOn()
        {
            writePlcManager.WriteDW(outputValues["PUMP ON"], new byte[] { 0x01, 0x00 });
        }

        public void PumpOff()
        {
            writePlcManager.WriteDW(outputValues["PUMP ON"], new byte[] { 0x00, 0x00 });
        }

        public void AlramResetOn()
        {
            writePlcManager.WriteDW(outputValues["ALAM RESET"], new byte[] { 0x01, 0x00 });
        }
        public void AlramResetOff()
        {
            writePlcManager.WriteDW(outputValues["ALAM RESET"], new byte[] { 0x00, 0x00 });
        }

        public void EmergencyOn()
        {
            writePlcManager.WriteDW(outputValues["EMERGENCY"], new byte[] { 0x01, 0x00 });
        }

        public void EmergencyOff()
        {
            writePlcManager.WriteDW(outputValues["EMERGENCY"], new byte[] { 0x00, 0x00 });
        }

        public void SaveFile(string path)
        {
            string data = "";

            data += "[Info]";
            data += "\n";

            data += "CurrentCleaingHoleIndex=" + inspectionInfo.CurrentCleaingHoleIndex;
            data += "\n";
            data += "BundleName=" + inspectionInfo.BundleName;
            data += "\n";
            data += "CleaningStartDateTime=" + inspectionInfo.CleaningStartDateTimeStr;
            data += "\n";
            data += "CleaningEndDateTime=" + inspectionInfo.CleaningEndDateTimeStr;
            data += "\n";
            data += "CleaningTime=" + inspectionInfo.CleaningTimeStr;
            data += "\n";
            data += "xResolution=" + inspectionInfo.xResolution;
            data += "\n";
            data += "yResolution=" + inspectionInfo.yResolution;
            data += "\n";
            data += "zResolution=" + inspectionInfo.zResolution;
            data += "\n";
            data += "scanXDistance=" + inspectionInfo.scanXDistance;
            data += "\n";
            data += "scanZDistance=" + inspectionInfo.scanZDistance;
            data += "\n";
            data += "appImageWidth=" + inspectionInfo.appImageWidth;
            data += "\n";
            data += "appImageHeight=" + inspectionInfo.appImageHeight;
            data += "\n";
            data += "CleaningCount=" + inspectionInfo.CleaningCount;
            data += "\n";
            data += "CleaningHoleCount=" + inspectionInfo.CleaningHoleCount;
            data += "\n";
            data += "CleaningMaxCount=" + inspectionInfo.CleaningMaxCount;
            data += "\n";
            data += "CleaningNGHoleCount=" + inspectionInfo.CleaningNGHoleCount;
            data += "\n";
            data += "CleaningOkHoleCount=" + inspectionInfo.CleaningOkHoleCount;
            data += "\n";
            data += "CleaningPerStr=" + inspectionInfo.CleaningPerStr;
            data += "\n";
            data += "IsFinishScanning=" + inspectionInfo.IsFinishScanning;
            data += "\n";
            data += "IsFinishScanningIntensityImage=" + inspectionInfo.IsFinishScanningIntensityImage;
            data += "\n";
            data += "IsUseNozle=" + inspectionInfo.IsUseNozle;
            data += "\n";
            data += "NoCleaningCount=" + inspectionInfo.NoCleaningCount;
            data += "\n";
            data += "Last Cleaning Time=" + DateTime.Now.ToString("yyyyMMddHHmmss");
            data += "\n";

            //홀 전체 저장
            inspectionInfo.Holes.ToList().ForEach((x) =>
            {
                data += "[Hole " + x.Index + "]";
                data += "\n";
                data += "Index=" + x.Index;
                data += "\n";
                data += "VisionX=" + x.VisionX;
                data += "\n";
                data += "VisionY=" + x.VisionY;
                data += "\n";
                if (x.BeforePoint != null)
                {
                    data += "BeforePoint=" + x.BeforePoint.Index;
                    data += "\n";
                }
                if (x.AfterPoint != null)
                {
                    data += "AfterPoint=" + x.AfterPoint.Index;
                    data += "\n";
                }

                data += "IsTarget=" + x.IsTarget;
                data += "\n";
                data += "Row=" + x.Row;
                data += "\n";
                data += "GroupIndex=" + x.GroupIndex;
                data += "\n";
                data += "Column=" + x.Column;
                data += "\n";
                data += "AfterDistance=" + x.AfterDistance;
                data += "\n";



            });

            File.WriteAllText(path, data);
        }
    }
}
