using AsyncSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PortableCleaner
{
    public class PlcManager
    {
        private AsyncSocketClient sock = null;

        #region 속성
        private string ip;
        public string IP { get { return ip; } }

        private int port;
        public int Port { get { return port; } }

        private bool isConnected;
        public bool IsConnected { get { return isConnected; } }
        private string lastErrorMsg;
        public string LastErrorMsg { get { return lastErrorMsg; } }
        private int lastErrorID;
        public int LastErrorID { get { return lastErrorID; } }

        private ReceiveData lastReadReceiveData;
        public ReceiveData LastReadReceiveData { get { return lastReadReceiveData; } }
         
        #endregion

        public PlcManager(string ip, int port)
        {
            this.ip = ip;
            this.port = port;

            sock = new AsyncSocketClient(0);

            sock.OnConnet += new AsyncSocketConnectEventHandler(OnConnet);
            sock.OnClose += new AsyncSocketCloseEventHandler(OnClose);
            sock.OnSend += new AsyncSocketSendEventHandler(OnSend);
            sock.OnReceive += new AsyncSocketReceiveEventHandler(OnReceive);
            sock.OnError += new AsyncSocketErrorEventHandler(OnError);
        }

        public bool Connect()
        {
            try
            {
                sock.Connect(ip, port);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool Close()
        {
            try
            {
                sock.Close();
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void OnConnet(object sender, AsyncSocketConnectionEventArgs e)
        {
            isConnected = true;
        }

        private void OnClose(object sender, AsyncSocketConnectionEventArgs e)
        {
            isConnected = false;
        }

        private void OnSend(object sender, AsyncSocketSendEventArgs e)
        {

        }

        private void OnReceive(object sender, AsyncSocketReceiveEventArgs e)
        {
            bool isReadReply = false;
            if (e.ReceiveData[20] == 0x55)
            {
              
                isReadReply = true;
                byte[] receiveInvokeIDBytes = new byte[2];
                receiveInvokeIDBytes[0] = e.ReceiveData[14];
                receiveInvokeIDBytes[1] = e.ReceiveData[15];

                ushort id = BitConverter.ToUInt16(receiveInvokeIDBytes, 0);

                lastReadReceiveData = new ReceiveData(id, e.ReceiveData);

           

            }
            else if (e.ReceiveData[20] == 0x59)
            {
               
            }
        }

        private void OnError(object sender, AsyncSocketErrorEventArgs e)
        {
            lastErrorMsg = e.AsyncSocketException.Message;
            lastErrorID = e.ID;
        }

        public void WriteDW(string addr, byte[] data)
        {
            int reCount = 5;
            for (int i = 0; i < reCount; i++)
            {
                try
                {
                    int connectTimeOut = 0;

                    Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    sock.SendTimeout = 1000;
                    sock.ReceiveTimeout = 1000;
                    //var result =sock.BeginConnect(IPAddress.Parse(ip), port, null, null);
                    sock.Connect(IPAddress.Parse(ip), Port);

                    //bool success = result.AsyncWaitHandle.WaitOne(connectTimeOut, true);
                    //if(success == true)
                    //{
                    //    sock.EndConnect(result);
                    //}
                    List<byte> frame = new List<byte>();
                    //체크섬
                    frame.Add(0x10);
                    //명령어
                    frame.Add(0x58);
                    frame.Add(0x00);
                    //데이터 타입
                    frame.Add(0x02);
                    frame.Add(0x00);
                    //예약 영역
                    frame.Add(0x00);
                    frame.Add(0x00);
                    //블록 개수 
                    frame.Add(0x01);
                    frame.Add(0x00);
                    //변수이름 길이
                    frame.Add(0x07);
                    frame.Add(0x00);
                    //데이터 주소
                    frame.AddRange(Encoding.ASCII.GetBytes(addr));

                    //데이터 수
                    frame.Add(0x01);
                    frame.Add(0x00);

                    //데이터
                    frame.Add(data[0]);
                    frame.Add(data[1]);

                    List<byte> header = new List<byte>();
                    //Company ID
                    header.AddRange(Encoding.ASCII.GetBytes("LSIS-XGT"));
                    header.Add(0x00);
                    header.Add(0x00);
                    //PLC INFO
                    header.Add(0x00);
                    header.Add(0x00);
                    //CPU Info
                    header.Add(0xB0);
                    //Source of Frame
                    header.Add(0x33);
                    //Invoke ID
                    header.Add(0x00);
                    header.Add(0x00);
                    //Length
                    header.Add(0x15);
                    header.Add(0x00);// 프레임 length
                                     //FEnet Position
                    header.Add(0x00);

                    List<byte> listSendData = new List<byte>(header);
                    listSendData.AddRange(frame);

                    sock.Send(listSendData.ToArray());
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    byte[] buffer = new byte[65535];
                    sock.Receive(buffer);

                    sock.Close();
                    break;
                }
                catch
                {
                    Thread.Sleep(500);
                    Console.WriteLine("Failed to send To Plc");

                }
            }
        }

        public void ReadDw(string addr, ushort id, ushort length)
        {

            lastReadReceiveData = null;
            string sendAddr = addr; ;

            if (addr.StartsWith("%DW"))
            {
                length *= 2;

                sendAddr = "%DB";
                sendAddr += (Convert.ToInt32(addr.Replace("%DW", "")) * 2).ToString();
            }


            byte[] lengthBytes = BitConverter.GetBytes(length);
            byte[] idBytes = BitConverter.GetBytes(id);

            ushort sendAddrLength = (ushort)sendAddr.Length;
            byte[] sendAddrLengthBytes = BitConverter.GetBytes(sendAddrLength);


            List<byte> frame = new List<byte>();
            //체크섬
            frame.Add(0x10);
            //명령어
            frame.Add(0x54);
            frame.Add(0x00);
            //데이터 타입
            frame.Add(0x14);
            frame.Add(0x00);
            //예약 영역
            frame.Add(0x00);
            frame.Add(0x00);
            //블록 개수 
            frame.Add(0x01);
            frame.Add(0x00);
            //변수명 길이
            frame.Add(sendAddrLengthBytes[0]);
            frame.Add(sendAddrLengthBytes[1]);
            //데이터 주소
            frame.AddRange(Encoding.ASCII.GetBytes(sendAddr));
            //읽은 데이터 개수
            frame.Add(lengthBytes[0]);
            frame.Add(lengthBytes[1]);


            //Header
            List<byte> header = new List<byte>();
            //Company ID
            header.AddRange(Encoding.ASCII.GetBytes("LSIS-XGT"));
            header.Add(0x00);
            header.Add(0x00);
            //PLC INFO
            header.Add(0x00);
            header.Add(0x00);
            //CPU Info
            header.Add(0xB0);
            //Source of Frame
            header.Add(0x33);
            //Invoke ID
            header.Add(idBytes[0]);
            header.Add(idBytes[1]);
            //Length
            header.Add(0x14);
            header.Add(0x00);// 프레임 length
            //FEnet Position
            header.Add(0x00);

            List<byte> listSendData = new List<byte>(header);
            listSendData.AddRange(frame);

            sock.Send(listSendData.ToArray());
        }

        public class ReceiveData
        {
            ushort id;

            public ushort ID { get { return id; } }

            ushort dataLength;
            public ushort DataLength { get { return dataLength; } }

            private List<UInt16> data = new List<ushort>();
            public List<UInt16> Data { get { return data; } }
            
            public ReceiveData(ushort id, byte[] receiveData)
            {
                this.id = id;
                //데이터 크기
                byte[] dataLengthBytes = new byte[2];
                dataLengthBytes[0] = receiveData[30];
                dataLengthBytes[1] = receiveData[31];
                this.dataLength = BitConverter.ToUInt16(dataLengthBytes, 0);

                List<UInt16> result = new List<ushort>();

                for(int i = 0; i < dataLength; i+=2)
                {
                    byte[] currentData = new byte[2];
                    currentData[0] = receiveData[32 + i];
                    currentData[1] = receiveData[32 + i + 1];
                    result.Add(BitConverter.ToUInt16(currentData, 0));
                }

                data = result;
            }
        }

    }
}
