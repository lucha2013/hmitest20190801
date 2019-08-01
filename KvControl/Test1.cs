/*
 * Created by SharpDevelop.
 * User: admin
 * Date: 2018/3/18 星期日
 * Time: 13:14
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.IO.Ports;

namespace KvControl
{
	[TestFixture]
	public class Test1
	{
		
		[Test]
		public void TestMethod()
		{
			var users = new Queue<Box>();
			users.Enqueue(new Box(){ID="box1",Msg="apple"});
			users.Enqueue(new Box(){ID="box2",Msg="xiaomi"});
			users.Enqueue(new Box(){ID="box3",Msg="huawei"});
			users.Enqueue(new Box(){ID="box4",Msg="saxing"});
			
			
			var task1 = new Task(delegate{
			                     	Box newBox =null;
			                     	while(true){
			                     		if(users.Count>0){			                     			
			                     			newBox = users.Dequeue();
			                     			Thread.Sleep(100);
			                     			Console.WriteLine(newBox.ID);
			                     			newBox= null;
			                     		}else{
			                     			Thread.Sleep(10);
			                     		}
			                     	}
			                     });
			task1.Start();
			task1.Wait();
			
			
			
		}
		
		[Test]
		public void TestQueue(){
			var workingList = new Queue<string>(10);
			workingList.Enqueue("1");
			workingList.Enqueue("2");
			Console.WriteLine(workingList.Dequeue());
		}
		
		[Test]
		public void TestFloat()
		{
			var fs = "2.4";
			var f = (float)Convert.ToDouble(fs);
			var bf = BitConverter.GetBytes(f);
			var l = BitConverter.ToInt32(new byte[]{bf[0],bf[1],0,0},0);
			var h = BitConverter.ToInt32(new byte[]{bf[2],bf[3],0,0},0);
			Console.WriteLine(bf.Length);
			Console.WriteLine(l+" "+h);
			
		}
		
		[Test]
		public void TestString()
		{
			var str =" ";
			var arr= str.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
			Console.WriteLine(arr.Length);
		}
		
		[Test]
		public void TestSerialPort(){
			
			var port = new SerialPort("COM3",9600,Parity.Even);
			Console.WriteLine(port.IsOpen);
			port.Open();
			port.Write("CR\r\n");
			int i=0;
			while(i<5){
				if(port.BytesToRead>0){
					Console.WriteLine(port.ReadLine());
				}
				i++;
				Thread.Sleep(10);
			}
		}
	}
	
	public class Box{
		public string ID{get;set;}
		public string Msg{get;set;}
	}
}
