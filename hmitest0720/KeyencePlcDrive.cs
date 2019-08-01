using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace hmitest0720
{
    public class KeyencePlcDrive : IPLCDrive
    {
        private short _id;
        private string _name;
        private string _ip;
        private int _port;
        private Socket socketSyc;
        private int _timeout;

        public short ID
        {
            get { return _id; }
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }


        public string ServerName
        {
            get { return _ip; }
            set { _ip = value; }
        }
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }

        public bool IsClosed
        {
            get { return socketSyc == null || socketSyc.Connected == false; }
        }

        public int TimeOut
        {
            get { return _timeout; }
            set { _timeout = value; }
        }

        public IEnumerable<IGroup> Groups => throw new NotImplementedException();

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public string GetAddress(DeviceAddress address)
        {
            throw new NotImplementedException();
        }

        public DeviceAddress GetDeviceAddress(string address,ushort len)
        {
            
            Response<DeviceAddress> response=DeviceAddress.ParseKeyenceFrom(address,len);
            return response.Value;

        }

        public bool Connect()
        {
            try
            {
                if (socketSyc != null)
                    socketSyc.Close();
                //IPAddress ip = IPAddress.Parse(_ip);
                // ----------------------------------------------------------------
                // Connect synchronous client
                if (_timeout <= 0) _timeout = 1000;
                socketSyc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socketSyc.SendTimeout = _timeout;
                socketSyc.ReceiveTimeout = _timeout;
                socketSyc.NoDelay = true;
                socketSyc.Connect(_ip, _port);
                return true;
            }
            catch (SocketException error)
            {               
                return false;
            }
        }
        public byte[] SyncSend(byte[] sendBuf)
        {
            if (sendBuf.Length < 1) return null;
            if (IsClosed) return null;
            try
            {
                int sendLen = socketSyc.Send(sendBuf);
                if (sendLen <1)
                {
                    return null;
                }
                byte[] data=new byte[1024];
                int receiveLen = socketSyc.Receive(data);
                byte[] buf = new byte[receiveLen];
                Array.Copy(data, 0, buf, 0, receiveLen);
                return buf;
            }
            catch
            {

            }
            return null;

        }
      

        public Response<bool> ReadBit(DeviceAddress address)
        {
            throw new NotImplementedException();
        }

        public Response<float> ReadFloat(DeviceAddress address)
        {
            Response<float[]> response= ReadFloat(address, 1);
            Response<float> response2 = new Response<float>();
            if (response.IsSuccess)
            {
                response2.IsSuccess = true;
                response2.Value = response.Value[0];
                return response2;
            }
            return Response.CreateFailResponse<float>(false,3,"fail");
        }
        public Response<float[]> ReadFloat(DeviceAddress address, ushort length)
        {
            float[] f = new float[length];
            ushort len = (ushort)(length * 2);
            byte[] command = MelsecHelper.BuildAsciiReadMcCoreCommand(address, false);

            byte[] read = SyncSend(PackMcCommand(command, 0, 0));

            ushort errorCode = Convert.ToUInt16(Encoding.ASCII.GetString(read, 18, 4), 16);
            if (errorCode != 0) Response.CreateFailResponse<float>(false, 3, "fail");

            byte[] extract = ExtractActualData(read, false);
            for (int i = 0; i < (extract.Length) / 4; i++)
            {
                f[i] = BitConverter.ToSingle(extract, i * 4);
            }
            return Response.CreateSuccessResponse<float[]>(f);
        }

        public bool WriteFloat(DeviceAddress address, float value)
        {
            throw new NotImplementedException();
        }

        
        public static byte[] PackMcCommand(byte[] mcCore, byte networkNumber = 0, byte networkStationNumber = 0)
        {
            byte[] plcCommand = new byte[22 + mcCore.Length];
            plcCommand[0] = 0x35;                                                                        // 副标题
            plcCommand[1] = 0x30;
            plcCommand[2] = 0x30;
            plcCommand[3] = 0x30;
            plcCommand[4] = MelsecHelper.BuildBytesFromData(networkNumber)[0];                         // 网络号
            plcCommand[5] = MelsecHelper.BuildBytesFromData(networkNumber)[1];
            plcCommand[6] = 0x46;                                                                        // PLC编号
            plcCommand[7] = 0x46;
            plcCommand[8] = 0x30;                                                                        // 目标模块IO编号
            plcCommand[9] = 0x33;
            plcCommand[10] = 0x46;
            plcCommand[11] = 0x46;
            plcCommand[12] = MelsecHelper.BuildBytesFromData(networkStationNumber)[0];                  // 目标模块站号
            plcCommand[13] = MelsecHelper.BuildBytesFromData(networkStationNumber)[1];
            plcCommand[14] = MelsecHelper.BuildBytesFromData((ushort)(plcCommand.Length - 18))[0];     // 请求数据长度
            plcCommand[15] = MelsecHelper.BuildBytesFromData((ushort)(plcCommand.Length - 18))[1];
            plcCommand[16] = MelsecHelper.BuildBytesFromData((ushort)(plcCommand.Length - 18))[2];
            plcCommand[17] = MelsecHelper.BuildBytesFromData((ushort)(plcCommand.Length - 18))[3];
            plcCommand[18] = 0x30;                                                                        // CPU监视定时器
            plcCommand[19] = 0x30;
            plcCommand[20] = 0x31;
            plcCommand[21] = 0x30;
            mcCore.CopyTo(plcCommand, 22);

            return plcCommand;
        }
        public static byte[] ExtractActualData(byte[] response, bool isBit)
        {
            if (isBit)
            {
                // 位读取
                byte[] Content = new byte[response.Length - 22];
                for (int i = 22; i < response.Length; i++)
                {
                    Content[i - 22] = response[i] == 0x30 ? (byte)0x00 : (byte)0x01;
                }

                return (Content);
            }
            else
            {
                // 字读取
                byte[] Content = new byte[(response.Length - 22) / 2];
                for (int i = 0; i < Content.Length / 2; i++)
                {
                    ushort tmp = Convert.ToUInt16(Encoding.ASCII.GetString(response, i * 4 + 22, 4), 16);
                    BitConverter.GetBytes(tmp).CopyTo(Content, i * 2);
                }

                return (Content);
            }
        }

        public IGroup AddGroup(string name, short id, int updateRate, float deadBand = 0, bool active = false)
        {
            throw new NotImplementedException();
        }

        public bool RemoveGroup(IGroup group)
        {
            throw new NotImplementedException();
        }
    }

    public class DeviceAddress : DeviceAddressDataBase
    {
        #region Constructor

        /// <summary>
        /// 实例化一个默认的对象
        /// </summary>
        public DeviceAddress()
        {
            McDataType = MelsecMcDataType.D;
        }

        #endregion

        /// <summary>
        /// 三菱的数据地址信息
        /// </summary>
        public MelsecMcDataType McDataType { get; set; }

        /// <summary>
        /// 从指定的地址信息解析成真正的设备地址信息，默认是三菱的地址
        /// </summary>
        /// <param name="address">地址信息</param>
        /// <param name="length">数据长度</param>
        public override void Parse(string address, ushort length)
        {
            Response<DeviceAddress> response = DeviceAddress.ParseKeyenceFrom(address, length);
            if (response.IsSuccess)
            {
                this.AddressStart = response.Value.AddressStart;
                this.Length = response.Value.Length;
                this.McDataType = response.Value.McDataType;
            }
        }
  
        /// <summary>
        /// 从实际基恩士的地址里面解析出
        /// </summary>
        /// <param name="address">基恩士的地址数据信息</param>
        /// <param name="length">读取的数据长度</param>
        /// <returns>是否成功的结果对象</returns>
        public static Response<DeviceAddress> ParseKeyenceFrom(string address, ushort length)
        {
            DeviceAddress addressData = new DeviceAddress();
            addressData.Length = length;
            string err;
            try
            {
                switch (address[0])
                {
                    case 'M':
                    case 'm':
                        {
                            addressData.McDataType = MelsecMcDataType.Keyence_M;
                            addressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.Keyence_M.FromBase);
                            break;
                        }
                    case 'X':
                    case 'x':
                        {
                            addressData.McDataType = MelsecMcDataType.Keyence_X;
                            addressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.Keyence_X.FromBase);
                            break;
                        }
                    case 'Y':
                    case 'y':
                        {
                            addressData.McDataType = MelsecMcDataType.Keyence_Y;
                            addressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.Keyence_Y.FromBase);
                            break;
                        }
                    case 'B':
                    case 'b':
                        {
                            addressData.McDataType = MelsecMcDataType.Keyence_B;
                            addressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.Keyence_B.FromBase);
                            break;
                        }
                    case 'L':
                    case 'l':
                        {
                            addressData.McDataType = MelsecMcDataType.Keyence_L;
                            addressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.Keyence_L.FromBase);
                            break;
                        }
                    case 'S':
                    case 's':
                        {
                            if (address[1] == 'M' || address[1] == 'm')
                            {
                                addressData.McDataType = MelsecMcDataType.Keyence_SM;
                                addressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.Keyence_SM.FromBase);
                                break;
                            }
                            else if (address[1] == 'D' || address[1] == 'd')
                            {
                                addressData.McDataType = MelsecMcDataType.Keyence_SD;
                                addressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.Keyence_SD.FromBase);
                                break;
                            }
                            else
                            {
                                err = "NotSupportedDataType";
                                throw new Exception(err);
                            }
                        }
                    case 'D':
                    case 'd':
                        {
                            addressData.McDataType = MelsecMcDataType.Keyence_D;
                            addressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.Keyence_D.FromBase);
                            break;
                        }
                    case 'R':
                    case 'r':
                        {
                            addressData.McDataType = MelsecMcDataType.Keyence_R;
                            addressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.Keyence_R.FromBase);
                            break;
                        }
                    case 'Z':
                    case 'z':
                        {
                            if (address[1] == 'R' || address[1] == 'r')
                            {
                                addressData.McDataType = MelsecMcDataType.Keyence_ZR;
                                addressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.Keyence_ZR.FromBase);
                                break;
                            }
                            else
                            {
                                err = "NotSupportedDataType";
                                throw new Exception(err);
                            }
                        }
                    case 'W':
                    case 'w':
                        {
                            addressData.McDataType = MelsecMcDataType.Keyence_W;
                            addressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.Keyence_W.FromBase);
                            break;
                        }
                    case 'T':
                    case 't':
                        {
                            if (address[1] == 'N' || address[1] == 'n')
                            {
                                addressData.McDataType = MelsecMcDataType.Keyence_TN;
                                addressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.Keyence_TN.FromBase);
                                break;
                            }
                            else if (address[1] == 'S' || address[1] == 's')
                            {
                                addressData.McDataType = MelsecMcDataType.Keyence_TS;
                                addressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.Keyence_TS.FromBase);
                                break;
                            }
                            else
                            {
                                err = "NotSupportedDataType";
                                throw new Exception(err);
                            }
                        }
                    case 'C':
                    case 'c':
                        {
                            if (address[1] == 'N' || address[1] == 'n')
                            {
                                addressData.McDataType = MelsecMcDataType.Keyence_CN;
                                addressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.Keyence_CN.FromBase);
                                break;
                            }
                            else if (address[1] == 'S' || address[1] == 's')
                            {
                                addressData.McDataType = MelsecMcDataType.Keyence_CS;
                                addressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.Keyence_CS.FromBase);
                                break;
                            }
                            else
                            {
                                err = "NotSupportedDataType";
                                throw new Exception(err);
                            }
                        }
                    default:
                        err = "NotSupportedDataType";
                        throw new Exception(err);
                }
            }
            catch (Exception ex)
            {
                return new Response<DeviceAddress>("NotSupportedDataType");
            }
            return Response.CreateSuccessResponse(addressData);
        }
    }

    public class DeviceAddressDataBase
    {
        /// <summary>
        /// 数字的起始地址，也就是偏移地址
        /// </summary>
        public int AddressStart { get; set; }

        /// <summary>
        /// 读取的数据长度
        /// </summary>
        public ushort Length { get; set; }


        /// <summary>
        /// 从指定的地址信息解析成真正的设备地址信息
        /// </summary>
        /// <param name="address">地址信息</param>
        /// <param name="length">数据长度</param>
        public virtual void Parse(string address, ushort length)
        {

        }


    }
    public class MelsecMcDataType
    {
        /// <summary>
        /// 如果您清楚类型代号，可以根据值进行扩展
        /// </summary>
        /// <param name="code">数据类型的代号</param>
        /// <param name="type">0或1，默认为0</param>
        /// <param name="asciiCode">ASCII格式的类型信息</param>
        /// <param name="fromBase">指示地址的多少进制的，10或是16</param>
        public MelsecMcDataType(byte code, byte type, string asciiCode, int fromBase)
        {
            DataCode = code;
            AsciiCode = asciiCode;
            FromBase = fromBase;
            if (type < 2) DataType = type;
        }

        /// <summary>
        /// 类型的代号值
        /// </summary>
        public byte DataCode { get; private set; } = 0x00;

        /// <summary>
        /// 数据的类型，0代表按字，1代表按位
        /// </summary>
        public byte DataType { get; private set; } = 0x00;

        /// <summary>
        /// 当以ASCII格式通讯时的类型描述
        /// </summary>
        public string AsciiCode { get; private set; }

        /// <summary>
        /// 指示地址是10进制，还是16进制的
        /// </summary>
        public int FromBase { get; private set; }

        /// <summary>
        /// X输入继电器
        /// </summary>
        public readonly static MelsecMcDataType X = new MelsecMcDataType(0x9C, 0x01, "X*", 16);

        /// <summary>
        /// Y输出继电器
        /// </summary>
        public readonly static MelsecMcDataType Y = new MelsecMcDataType(0x9D, 0x01, "Y*", 16);

        /// <summary>
        /// M中间继电器
        /// </summary>
        public readonly static MelsecMcDataType M = new MelsecMcDataType(0x90, 0x01, "M*", 10);

        /// <summary>
        /// D数据寄存器
        /// </summary>
        public readonly static MelsecMcDataType D = new MelsecMcDataType(0xA8, 0x00, "D*", 10);

        /// <summary>
        /// W链接寄存器
        /// </summary>
        public readonly static MelsecMcDataType W = new MelsecMcDataType(0xB4, 0x00, "W*", 16);

        /// <summary>
        /// L锁存继电器
        /// </summary>
        public readonly static MelsecMcDataType L = new MelsecMcDataType(0x92, 0x01, "L*", 10);

        /// <summary>
        /// F报警器
        /// </summary>
        public readonly static MelsecMcDataType F = new MelsecMcDataType(0x93, 0x01, "F*", 10);

        /// <summary>
        /// V边沿继电器
        /// </summary>
        public readonly static MelsecMcDataType V = new MelsecMcDataType(0x94, 0x01, "V*", 10);

        /// <summary>
        /// B链接继电器
        /// </summary>
        public readonly static MelsecMcDataType B = new MelsecMcDataType(0xA0, 0x01, "B*", 16);

        /// <summary>
        /// R文件寄存器
        /// </summary>
        public readonly static MelsecMcDataType R = new MelsecMcDataType(0xAF, 0x00, "R*", 10);

        /// <summary>
        /// S步进继电器
        /// </summary>
        public readonly static MelsecMcDataType S = new MelsecMcDataType(0x98, 0x01, "S*", 10);

        /// <summary>
        /// 变址寄存器
        /// </summary>
        public readonly static MelsecMcDataType Z = new MelsecMcDataType(0xCC, 0x00, "Z*", 10);

        /// <summary>
        /// 定时器的当前值
        /// </summary>
        public readonly static MelsecMcDataType TN = new MelsecMcDataType(0xC2, 0x00, "TN", 10);

        /// <summary>
        /// 定时器的触点
        /// </summary>
        public readonly static MelsecMcDataType TS = new MelsecMcDataType(0xC1, 0x01, "TS", 10);

        /// <summary>
        /// 定时器的线圈
        /// </summary>
        public readonly static MelsecMcDataType TC = new MelsecMcDataType(0xC0, 0x01, "TC", 10);

        /// <summary>
        /// 累计定时器的触点
        /// </summary>
        public readonly static MelsecMcDataType SS = new MelsecMcDataType(0xC7, 0x01, "SS", 10);

        /// <summary>
        /// 累计定时器的线圈
        /// </summary>
        public readonly static MelsecMcDataType SC = new MelsecMcDataType(0xC6, 0x01, "SC", 10);

        /// <summary>
        /// 累计定时器的当前值
        /// </summary>
        public readonly static MelsecMcDataType SN = new MelsecMcDataType(0xC8, 0x00, "SN", 100);

        /// <summary>
        /// 计数器的当前值
        /// </summary>
        public readonly static MelsecMcDataType CN = new MelsecMcDataType(0xC5, 0x00, "CN", 10);

        /// <summary>
        /// 计数器的触点
        /// </summary>
        public readonly static MelsecMcDataType CS = new MelsecMcDataType(0xC4, 0x01, "CS", 10);

        /// <summary>
        /// 计数器的线圈
        /// </summary>
        public readonly static MelsecMcDataType CC = new MelsecMcDataType(0xC3, 0x01, "CC", 10);

        /// <summary>
        /// 文件寄存器ZR区
        /// </summary>
        public readonly static MelsecMcDataType ZR = new MelsecMcDataType(0xB0, 0x00, "ZR", 16);




        /// <summary>
        /// X输入继电器
        /// </summary>
        public readonly static MelsecMcDataType Keyence_X = new MelsecMcDataType(0x9C, 0x01, "X*", 16);
        /// <summary>
        /// Y输出继电器
        /// </summary>
        public readonly static MelsecMcDataType Keyence_Y = new MelsecMcDataType(0x9D, 0x01, "Y*", 16);
        /// <summary>
        /// 链接继电器
        /// </summary>
        public readonly static MelsecMcDataType Keyence_B = new MelsecMcDataType(0xA0, 0x01, "B*", 16);
        /// <summary>
        /// 内部辅助继电器
        /// </summary>
        public readonly static MelsecMcDataType Keyence_M = new MelsecMcDataType(0x90, 0x01, "M*", 10);
        /// <summary>
        /// 锁存继电器
        /// </summary>
        public readonly static MelsecMcDataType Keyence_L = new MelsecMcDataType(0x92, 0x01, "L*", 10);
        /// <summary>
        /// 控制继电器
        /// </summary>
        public readonly static MelsecMcDataType Keyence_SM = new MelsecMcDataType(0x91, 0x01, "SM", 10);
        /// <summary>
        /// 控制存储器
        /// </summary>
        public readonly static MelsecMcDataType Keyence_SD = new MelsecMcDataType(0xA9, 0x00, "SD", 10);
        /// <summary>
        /// 数据存储器
        /// </summary>
        public readonly static MelsecMcDataType Keyence_D = new MelsecMcDataType(0xA8, 0x00, "D*", 10);
        /// <summary>
        /// 文件寄存器
        /// </summary>
        public readonly static MelsecMcDataType Keyence_R = new MelsecMcDataType(0xAF, 0x00, "R*", 10);
        /// <summary>
        /// 文件寄存器
        /// </summary>
        public readonly static MelsecMcDataType Keyence_ZR = new MelsecMcDataType(0xB0, 0x00, "ZR", 16);
        /// <summary>
        /// 链路寄存器
        /// </summary>
        public readonly static MelsecMcDataType Keyence_W = new MelsecMcDataType(0xB4, 0x00, "W*", 16);
        /// <summary>
        /// 计时器（当前值）
        /// </summary>
        public readonly static MelsecMcDataType Keyence_TN = new MelsecMcDataType(0xC2, 0x00, "TN", 10);
        /// <summary>
        /// 计时器（接点）
        /// </summary>
        public readonly static MelsecMcDataType Keyence_TS = new MelsecMcDataType(0xC1, 0x01, "TS", 10);
        /// <summary>
        /// 计数器（当前值）
        /// </summary>
        public readonly static MelsecMcDataType Keyence_CN = new MelsecMcDataType(0xC5, 0x00, "CN", 10);
        /// <summary>
        /// 计数器（接点）
        /// </summary>
        public readonly static MelsecMcDataType Keyence_CS = new MelsecMcDataType(0xC4, 0x01, "CS", 10);


        /// <summary>
        /// 输入继电器
        /// </summary>
        public readonly static MelsecMcDataType Panasonic_X = new MelsecMcDataType(0x9C, 0x01, "X*", 10);
        /// <summary>
        /// 输出继电器
        /// </summary>
        public readonly static MelsecMcDataType Panasonic_Y = new MelsecMcDataType(0x9D, 0x01, "Y*", 10);
        /// <summary>
        /// 链接继电器
        /// </summary>
        public readonly static MelsecMcDataType Panasonic_L = new MelsecMcDataType(0xA0, 0x01, "L*", 10);
        /// <summary>
        /// 内部继电器
        /// </summary>
        public readonly static MelsecMcDataType Panasonic_R = new MelsecMcDataType(0x90, 0x01, "R*", 10);
        /// <summary>
        /// 数据存储器
        /// </summary>
        public readonly static MelsecMcDataType Panasonic_DT = new MelsecMcDataType(0xA8, 0x00, "D*", 10);
        /// <summary>
        /// 链接存储器
        /// </summary>
        public readonly static MelsecMcDataType Panasonic_LD = new MelsecMcDataType(0xB4, 0x00, "W*", 10);
        /// <summary>
        /// 计时器（当前值）
        /// </summary>
        public readonly static MelsecMcDataType Panasonic_TN = new MelsecMcDataType(0xC2, 0x00, "TN", 10);
        /// <summary>
        /// 计时器（接点）
        /// </summary>
        public readonly static MelsecMcDataType Panasonic_TS = new MelsecMcDataType(0xC1, 0x01, "TS", 10);
        /// <summary>
        /// 计数器（当前值）
        /// </summary>
        public readonly static MelsecMcDataType Panasonic_CN = new MelsecMcDataType(0xC5, 0x00, "CN", 10);
        /// <summary>
        /// 计数器（接点）
        /// </summary>
        public readonly static MelsecMcDataType Panasonic_CS = new MelsecMcDataType(0xC4, 0x01, "CS", 10);
        /// <summary>
        /// 特殊链接继电器
        /// </summary>
        public readonly static MelsecMcDataType Panasonic_SM = new MelsecMcDataType(0x91, 0x01, "SM", 10);
        /// <summary>
        /// 特殊链接存储器
        /// </summary>
        public readonly static MelsecMcDataType Panasonic_SD = new MelsecMcDataType(0xA9, 0x00, "SD", 10);

    }
    public class MelsecHelper
    {
        #region Melsec Mc

        /// <summary>
        /// 解析A1E协议数据地址
        /// </summary>
        /// <param name="address">数据地址</param>
        /// <returns></returns>

        /// <summary>
        /// 从三菱地址，是否位读取进行创建读取的MC的核心报文
        /// </summary>
        /// <param name="isBit">是否进行了位读取操作</param>
        /// <param name="addressData">三菱Mc协议的数据地址</param>
        /// <returns>带有成功标识的报文对象</returns>
        public static byte[] BuildReadMcCoreCommand(DeviceAddress addressData, bool isBit)
        {
            byte[] command = new byte[10];
            command[0] = 0x01;                                                      // 批量读取数据命令
            command[1] = 0x04;
            command[2] = isBit ? (byte)0x01 : (byte)0x00;                           // 以点为单位还是字为单位成批读取
            command[3] = 0x00;
            command[4] = BitConverter.GetBytes(addressData.AddressStart)[0];      // 起始地址的地位
            command[5] = BitConverter.GetBytes(addressData.AddressStart)[1];
            command[6] = BitConverter.GetBytes(addressData.AddressStart)[2];
            command[7] = addressData.McDataType.DataCode;                           // 指明读取的数据
            command[8] = (byte)(addressData.Length % 256);                          // 软元件的长度
            command[9] = (byte)(addressData.Length / 256);

            return command;
        }

        /// <summary>
        /// 从三菱地址，是否位读取进行创建读取Ascii格式的MC的核心报文
        /// </summary>
        /// <param name="addressData">三菱Mc协议的数据地址</param>
        /// <param name="isBit">是否进行了位读取操作</param>
        /// <returns>带有成功标识的报文对象</returns>
        public static byte[] BuildAsciiReadMcCoreCommand(DeviceAddress addressData, bool isBit)
        {
            byte[] command = new byte[20];
            command[0] = 0x30;                                                               // 批量读取数据命令
            command[1] = 0x34;
            command[2] = 0x30;
            command[3] = 0x31;
            command[4] = 0x30;                                                               // 以点为单位还是字为单位成批读取
            command[5] = 0x30;
            command[6] = 0x30;
            command[7] = isBit ? (byte)0x31 : (byte)0x30;
            command[8] = Encoding.ASCII.GetBytes(addressData.McDataType.AsciiCode)[0];          // 软元件类型
            command[9] = Encoding.ASCII.GetBytes(addressData.McDataType.AsciiCode)[1];
            command[10] = MelsecHelper.BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType)[0];            // 起始地址的地位
            command[11] = MelsecHelper.BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType)[1];
            command[12] = MelsecHelper.BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType)[2];
            command[13] = MelsecHelper.BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType)[3];
            command[14] = MelsecHelper.BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType)[4];
            command[15] = MelsecHelper.BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType)[5];
            command[16] = MelsecHelper.BuildBytesFromData(addressData.Length)[0];                                             // 软元件点数
            command[17] = MelsecHelper.BuildBytesFromData(addressData.Length)[1];
            command[18] = MelsecHelper.BuildBytesFromData(addressData.Length)[2];
            command[19] = MelsecHelper.BuildBytesFromData(addressData.Length)[3];

            return command;
        }

        /// <summary>
        /// 以字为单位，创建数据写入的核心报文
        /// </summary>
        /// <param name="addressData">三菱Mc协议的数据地址</param>
        /// <param name="value">实际的原始数据信息</param>
        /// <returns>带有成功标识的报文对象</returns>
        public static byte[] BuildWriteWordCoreCommand(DeviceAddress addressData, byte[] value)
        {
            if (value == null) value = new byte[0];
            byte[] command = new byte[10 + value.Length];
            command[0] = 0x01;                                                        // 批量写入数据命令
            command[1] = 0x14;
            command[2] = 0x00;                                                        // 以字为单位成批读取
            command[3] = 0x00;
            command[4] = BitConverter.GetBytes(addressData.AddressStart)[0];        // 起始地址的地位
            command[5] = BitConverter.GetBytes(addressData.AddressStart)[1];
            command[6] = BitConverter.GetBytes(addressData.AddressStart)[2];
            command[7] = addressData.McDataType.DataCode;                             // 指明写入的数据
            command[8] = (byte)(value.Length / 2 % 256);                              // 软元件长度的地位
            command[9] = (byte)(value.Length / 2 / 256);
            value.CopyTo(command, 10);

            return command;
        }

        /// <summary>
        /// 以字为单位，创建ASCII数据写入的核心报文
        /// </summary>
        /// <param name="addressData">三菱Mc协议的数据地址</param>
        /// <param name="value">实际的原始数据信息</param>
        /// <returns>带有成功标识的报文对象</returns>
        public static byte[] BuildAsciiWriteWordCoreCommand(DeviceAddress addressData, byte[] value)
        {
            if (value == null) value = new byte[0];
            byte[] buffer = new byte[value.Length * 2];
            for (int i = 0; i < value.Length / 2; i++)
            {
                MelsecHelper.BuildBytesFromData(BitConverter.ToUInt16(value, i * 2)).CopyTo(buffer, 4 * i);
            }
            value = buffer;

            byte[] command = new byte[20 + value.Length];
            command[0] = 0x31;                                                                                          // 批量写入的命令
            command[1] = 0x34;
            command[2] = 0x30;
            command[3] = 0x31;
            command[4] = 0x30;                                                                                          // 子命令
            command[5] = 0x30;
            command[6] = 0x30;
            command[7] = 0x30;
            command[8] = Encoding.ASCII.GetBytes(addressData.McDataType.AsciiCode)[0];                                // 软元件类型
            command[9] = Encoding.ASCII.GetBytes(addressData.McDataType.AsciiCode)[1];
            command[10] = MelsecHelper.BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType)[0];     // 起始地址的地位
            command[11] = MelsecHelper.BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType)[1];
            command[12] = MelsecHelper.BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType)[2];
            command[13] = MelsecHelper.BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType)[3];
            command[14] = MelsecHelper.BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType)[4];
            command[15] = MelsecHelper.BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType)[5];
            command[16] = MelsecHelper.BuildBytesFromData((ushort)(value.Length / 4))[0];                              // 软元件点数
            command[17] = MelsecHelper.BuildBytesFromData((ushort)(value.Length / 4))[1];
            command[18] = MelsecHelper.BuildBytesFromData((ushort)(value.Length / 4))[2];
            command[19] = MelsecHelper.BuildBytesFromData((ushort)(value.Length / 4))[3];
            value.CopyTo(command, 20);

            return command;
        }

        /// <summary>
        /// 以位为单位，创建数据写入的核心报文
        /// </summary>
        /// <param name="addressData">三菱Mc协议的数据地址</param>
        /// <param name="value">原始的bool数组数据</param>
        /// <returns>带有成功标识的报文对象</returns>
        public static byte[] BuildWriteBitCoreCommand(DeviceAddress addressData, bool[] value)
        {
            if (value == null) value = new bool[0];
            byte[] buffer = MelsecHelper.TransBoolArrayToByteData(value);
            byte[] command = new byte[10 + buffer.Length];
            command[0] = 0x01;                                                        // 批量写入数据命令
            command[1] = 0x14;
            command[2] = 0x01;                                                        // 以位为单位成批写入
            command[3] = 0x00;
            command[4] = BitConverter.GetBytes(addressData.AddressStart)[0];        // 起始地址的地位
            command[5] = BitConverter.GetBytes(addressData.AddressStart)[1];
            command[6] = BitConverter.GetBytes(addressData.AddressStart)[2];
            command[7] = addressData.McDataType.DataCode;                             // 指明写入的数据
            command[8] = (byte)(value.Length % 256);                                  // 软元件长度的地位
            command[9] = (byte)(value.Length / 256);
            buffer.CopyTo(command, 10);

            return command;
        }

        /// <summary>
        /// 以位为单位，创建ASCII数据写入的核心报文
        /// </summary>
        /// <param name="addressData">三菱Mc协议的数据地址</param>
        /// <param name="value">原始的bool数组数据</param>
        /// <returns>带有成功标识的报文对象</returns>
        public static byte[] BuildAsciiWriteBitCoreCommand(DeviceAddress addressData, bool[] value)
        {
            if (value == null) value = new bool[0];
            byte[] buffer = value.Select(m => m ? (byte)0x31 : (byte)0x30).ToArray();

            byte[] command = new byte[20 + buffer.Length];
            command[0] = 0x31;                                                                              // 批量写入的命令
            command[1] = 0x34;
            command[2] = 0x30;
            command[3] = 0x31;
            command[4] = 0x30;                                                                              // 子命令
            command[5] = 0x30;
            command[6] = 0x30;
            command[7] = 0x31;
            command[8] = Encoding.ASCII.GetBytes(addressData.McDataType.AsciiCode)[0];                         // 软元件类型
            command[9] = Encoding.ASCII.GetBytes(addressData.McDataType.AsciiCode)[1];
            command[10] = MelsecHelper.BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType)[0];     // 起始地址的地位
            command[11] = MelsecHelper.BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType)[1];
            command[12] = MelsecHelper.BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType)[2];
            command[13] = MelsecHelper.BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType)[3];
            command[14] = MelsecHelper.BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType)[4];
            command[15] = MelsecHelper.BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType)[5];
            command[16] = MelsecHelper.BuildBytesFromData((ushort)(value.Length))[0];              // 软元件点数
            command[17] = MelsecHelper.BuildBytesFromData((ushort)(value.Length))[1];
            command[18] = MelsecHelper.BuildBytesFromData((ushort)(value.Length))[2];
            command[19] = MelsecHelper.BuildBytesFromData((ushort)(value.Length))[3];
            buffer.CopyTo(command, 20);

            return command;
        }

        #endregion

        #region Common Logic

        /// <summary>
        /// 从字节构建一个ASCII格式的地址字节
        /// </summary>
        /// <param name="value">字节信息</param>
        /// <returns>ASCII格式的地址</returns>
        internal static byte[] BuildBytesFromData(byte value)
        {
            return Encoding.ASCII.GetBytes(value.ToString("X2"));
        }

        /// <summary>
        /// 从short数据构建一个ASCII格式地址字节
        /// </summary>
        /// <param name="value">short值</param>
        /// <returns>ASCII格式的地址</returns>
        internal static byte[] BuildBytesFromData(short value)
        {
            return Encoding.ASCII.GetBytes(value.ToString("X4"));
        }

        /// <summary>
        /// 从ushort数据构建一个ASCII格式地址字节
        /// </summary>
        /// <param name="value">ushort值</param>
        /// <returns>ASCII格式的地址</returns>
        internal static byte[] BuildBytesFromData(ushort value)
        {
            return Encoding.ASCII.GetBytes(value.ToString("X4"));
        }

        /// <summary>
        /// 从三菱的地址中构建MC协议的6字节的ASCII格式的地址
        /// </summary>
        /// <param name="address">三菱地址</param>
        /// <param name="type">三菱的数据类型</param>
        /// <returns>6字节的ASCII格式的地址</returns>
        internal static byte[] BuildBytesFromAddress(int address, MelsecMcDataType type)
        {
            return Encoding.ASCII.GetBytes(address.ToString(type.FromBase == 10 ? "D6" : "X6"));
        }


        /// <summary>
        /// 从字节数组构建一个ASCII格式的地址字节
        /// </summary>
        /// <param name="value">字节信息</param>
        /// <returns>ASCII格式的地址</returns>
        internal static byte[] BuildBytesFromData(byte[] value)
        {
            byte[] buffer = new byte[value.Length * 2];
            for (int i = 0; i < value.Length; i++)
            {
                BuildBytesFromData(value[i]).CopyTo(buffer, 2 * i);
            }
            return buffer;
        }

        /// <summary>
        /// 将0，1，0，1的字节数组压缩成三菱格式的字节数组来表示开关量的
        /// </summary>
        /// <param name="value">原始的数据字节</param>
        /// <returns>压缩过后的数据字节</returns>
        internal static byte[] TransBoolArrayToByteData(byte[] value)
        {
            int length = (value.Length + 1) / 2;
            byte[] buffer = new byte[length];

            for (int i = 0; i < length; i++)
            {
                if (value[i * 2 + 0] != 0x00) buffer[i] += 0x10;
                if ((i * 2 + 1) < value.Length)
                {
                    if (value[i * 2 + 1] != 0x00) buffer[i] += 0x01;
                }
            }

            return buffer;
        }

        /// <summary>
        /// 将bool的组压缩成三菱格式的字节数组来表示开关量的
        /// </summary>
        /// <param name="value">原始的数据字节</param>
        /// <returns>压缩过后的数据字节</returns>
        internal static byte[] TransBoolArrayToByteData(bool[] value)
        {
            int length = (value.Length + 1) / 2;
            byte[] buffer = new byte[length];

            for (int i = 0; i < length; i++)
            {
                if (value[i * 2 + 0]) buffer[i] += 0x10;
                if ((i * 2 + 1) < value.Length)
                {
                    if (value[i * 2 + 1]) buffer[i] += 0x01;
                }
            }

            return buffer;
        }

        #endregion

        #region CRC Check

        /// <summary>
        /// 计算Fx协议指令的和校验信息
        /// </summary>
        /// <param name="data">字节数据</param>
        /// <returns>校验之后的数据</returns>
        internal static byte[] FxCalculateCRC(byte[] data)
        {
            int sum = 0;
            for (int i = 1; i < data.Length - 2; i++)
            {
                sum += data[i];
            }
            return BuildBytesFromData((byte)sum);
        }

        /// <summary>
        /// 检查指定的和校验是否是正确的
        /// </summary>
        /// <param name="data">字节数据</param>
        /// <returns>是否成功</returns>
        internal static bool CheckCRC(byte[] data)
        {
            byte[] crc = FxCalculateCRC(data);
            if (crc[0] != data[data.Length - 2]) return false;
            if (crc[1] != data[data.Length - 1]) return false;
            return true;
        }

        #endregion
    }
}
