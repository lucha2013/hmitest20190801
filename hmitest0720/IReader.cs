using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hmitest0720
{
    public interface IReadWrite
    {
        Response<bool> ReadBit(DeviceAddress address);
        Response<float> ReadFloat(DeviceAddress address);
        bool WriteFloat(DeviceAddress address, float value);

    }

    public interface IDriver : IDisposable
    {
        short ID { get; }
        string Name { get; }
        string ServerName { get; set; }
        bool IsClosed { get; }
        int TimeOut { get; set; }
        IEnumerable<IGroup> Groups { get; }
        ////IDataServer Parent { get; }
        bool Connect();
        IGroup AddGroup(string name, short id, int updateRate, float deadBand = 0f, bool active = false);
        bool RemoveGroup(IGroup group);
        //event IOErrorEventHandler OnError;
    }
    public interface IPLCDrive : IDriver, IReadWrite
    {
        DeviceAddress GetDeviceAddress(string address,ushort len);
        string GetAddress(DeviceAddress address);

    }

    public interface IGroup
    {
        bool IsActive { get; set; }
        short ID { get; }
        string Name { get; }
        int UpdateRate { get; set; }
        IEnumerable<ITag> Items { get; }
        //float DeadBand { get; set; }
        bool AddTags(IEnumerable<ITag> tags);
        bool RemoveTags(params ITag[] tags);
    }

    


    public class Response
    {
        public Response()
        {

        }
        public Response(string message)
        {
            this.Message = message;
        }
        public Response(int errorCode,string message)
        {
            this.ErrorCode = errorCode;
            this.Message = message;
        }
        public bool IsSuccess { get; set; }
        public int ErrorCode { get; set; }
        public string Message { get; set; }

        public static Response<T> CreateSuccessResponse<T>(T value)
        {
            return new Response<T>()
            {
                IsSuccess = true,
                ErrorCode = 0,
                Value = value
            };
        }
        public static Response<T> CreateFailResponse<T>(Response response)
        {
            return new Response<T>()
            {
                IsSuccess = false,
                ErrorCode = response.ErrorCode,
                Message=response.Message

            };
        }
        public static Response<T> CreateFailResponse<T>(bool isSuccess,int errorCode,string msg)
        {
            return new Response<T>()
            {
                IsSuccess = isSuccess,
                ErrorCode = errorCode,
                Message = msg

            };
        }

    }
    public class Response<T>:Response
    {
        public Response() : base()
        {

        }
        public Response(string msg) : base(msg)
        {

        }
        public Response(int errorCode,string msg) : base(errorCode,msg)
        {

        }


        public T Value { get; set; }


    }

}
