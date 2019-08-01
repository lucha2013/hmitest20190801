/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 2018/3/21
 * Time: 16:57
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO.Ports;

namespace KvControl
{
	/// <summary>
	/// Description of KvSerialPort.
	/// </summary>
	public sealed class KvSerialPort:SerialPort
	{		
		public KvSerialPort()
		{
		}
		
		public void Start(){
			KVSerialPortManager.Instance.Init(this);
		}
	}
}
