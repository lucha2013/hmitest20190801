/*
 * Created by SharpDevelop.
 * User: admin
 * Date: 2018/3/18 星期日
 * Time: 14:24
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KvControl{

	
	enum ServiceStatus{
		Initial,
		Ready,
		Posted,
		Pause,
		Stop,
		Error
	}
	/// <summary>
	/// Description of KVSerialPortManager.
	/// </summary>
	public sealed class KVSerialPortManager    {
		private static KVSerialPortManager instance = new KVSerialPortManager();
		
		public static KVSerialPortManager Instance {
			get {
				return instance;
			}
		}
		
		private SerialPort serialPort=null;
		
		private IKvController currentBox=null;
		
		private ServiceStatus Status;
		
		private KVSerialPortManager()
		{
		}
		
		private void LogRequest(string cmd)
		{
			serialPort.Write(cmd);
			Console.Write(cmd);
		}
		public void Init(SerialPort port){
			Status = ServiceStatus.Initial;
			serialPort = port;
			if(!serialPort.IsOpen){
				serialPort.Open();
				LogRequest("CR\r\n");
				var i = 0;
				while(i<5){
					if(serialPort.BytesToRead>0){
						var resp = serialPort.ReadLine();
						if(resp.StartsWith("CC")){
							Status = ServiceStatus.Ready;
							break;
						}else{
							Status = ServiceStatus.Error;							
						}
					}
					Thread.Sleep(10);
					i++;
				}
				if(this.Status!=ServiceStatus.Ready){
					throw new Exception("Connect PLC failed!");
				}
			}
			serialPort.DataReceived+= delegate{
				if(serialPort.BytesToRead>0){
					var resp = serialPort.ReadLine();
					if(this.currentBox!=null){
						this.currentBox.UpdateUI(resp);
						this.currentBox = null;
					}
					Status = ServiceStatus.Ready;
				}
			};
			
			Request nextRequest=null;
			
			var postTask = new Task(delegate{			                        	
			                        	while(true && Status!=ServiceStatus.Stop){
			                        		if(Status == ServiceStatus.Ready){
			                        			
			                        			if(firstPriorityList.Count>0){
			                        				nextRequest = firstPriorityList.Dequeue();
			                        			}else if(requestsList.Count>0){
			                        				nextRequest = requestsList.Dequeue();
//			                        				if(nextRequest.Box!=null){
//			                        					var ctrl = (System.Windows.Forms.Control)nextRequest.Box;
//			                        					if(!ctrl.IsHandleCreated){
//			                        						requestsList.Enqueue(nextRequest);
//			                        						Status = ServiceStatus.Ready;
//			                        						continue;
//			                        					}
//			                        				}
			                        			}
			                        			if(nextRequest.Box!=null){
			                        				currentBox = nextRequest.Box;			                        				
			                        			}			                        			
			                        			var cmd  = nextRequest.Box!=null?nextRequest.Box.GetReadCmd(): nextRequest.RequestText;
			                        			
			                        			if(string.IsNullOrEmpty(cmd)){
			                        				Status = ServiceStatus.Ready;
			                        				continue;
			                        			}
			                        			LogRequest(cmd);
			                        			Status = ServiceStatus.Posted;
			                        			if(nextRequest.Box!=null){
			                        				if(nextRequest.Box.KVAutoSync){
			                        					requestsList.Enqueue(nextRequest);
			                        				}
			                        			}
			                        			continue;
			                        		}
			                        		Thread.Sleep(1);
			                        	}
			                        });
			
			postTask.Start();
		}
		
		private Queue<Request> requestsList = new Queue<Request>(256);

		private Queue<Request> firstPriorityList = new Queue<Request>(256);
		
		
		private Queue<IKvController> autoSyncList = new Queue<IKvController>(256);
		
		public void Stop(){
			this.Status = ServiceStatus.Stop;
		}
		
		public void Pause(){
			this.Status = ServiceStatus.Pause;
		}
		
		/// <summary>
		/// Configure serialport with real case;
		/// </summary>
		/// <param name="portName">Default COM1</param>
		/// <param name="baudrate">Default 9600</param>
		/// <param name="parity">Default Even</param>
		public void ConfigSerialPort(string portName,int baudrate,Parity parity){
			this.serialPort.PortName = portName;
			this.serialPort.BaudRate = baudrate;
			this.serialPort.Parity = parity;
		}
		
		public void EnQueue(IKvController box){
			requestsList.Enqueue(new Request(){Box = box});
		}
		
		public void AppendRequest(string cmd){
			firstPriorityList.Enqueue(new Request(){RequestText = cmd});
		}
		
		
		public void Release(){
			Status = ServiceStatus.Stop;
			if(serialPort.IsOpen){
				serialPort.Close();
			}
			serialPort.Dispose();
		}
		class Request{
			public string RequestText{get;set;}
			public IKvController Box{get;set;}
		}
		
		class Response{
			public string ResponseText{get;set;}
			public IKvController IKvBox{get;set;}
		}
	}
}
