/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 2018/3/21
 * Time: 15:53
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Windows.Forms;

namespace KvControl
{
	/// <summary>
	/// Description of KVLabel.
	/// </summary>
	public class KVLabel:Label,IKvController
	{
		#region IKvBox implementation
		
		public string KVMemAddr {
			get ;
			set ;
		}

		public int KVByteLen {
			get ;
			set ;
		}

		public bool KVAutoSync {
			get ;
			set ;
		}

		public bool KVReadOnly {
			get ;
			set ;
		}
		
		public string GetReadCmd()
		{
			if(string.IsNullOrEmpty(KVMemAddr)){
				return string.Empty;
			}
			switch(KVByteLen){
				case 32:
					return "RDS "+KVMemAddr +" 2\r\n";
				default:
					return "RD "+KVMemAddr + "\r\n";
			}
		}

		public string GetWriteCmd()
		{
			if(string.IsNullOrEmpty(KVMemAddr)){
				return string.Empty;
			}
			switch(KVByteLen){
				case 32:
					var f = (float)Convert.ToDouble(this.Text);
					var bf = BitConverter.GetBytes(f);
					var l = BitConverter.ToInt32(new byte[]{bf[0],bf[1],0,0},0);
					var h = BitConverter.ToInt32(new byte[]{bf[2],bf[3],0,0},0);
					return string.Format("WRS {0} 2 {1} {2}\r\n",KVMemAddr,l,h);
				default:
					var i = Convert.ToInt32(this.Text);
					return string.Format("RD {0} {1}\r\n",KVMemAddr,i);
			}
		}
		
		public void UpdateUI(string resp){
			var arrs = resp.Split(new[]{" "},StringSplitOptions.RemoveEmptyEntries);
			if(arrs.Length==0){
				return;
			}
			
			if(arrs.Length==1){
				if(this.IsHandleCreated){
					this.BeginInvoke((EventHandler)delegate{
					                 	this.Text = Convert.ToInt32(arrs[0]).ToString();
					                 });
				}else{
					this.Text = Convert.ToInt32(arrs[0]).ToString();
				}
			}else if(arrs.Length==2){
				if(this.IsHandleCreated){
					this.BeginInvoke((EventHandler)delegate{
					                 	var l = BitConverter.GetBytes(Convert.ToInt32(arrs[0]));
					                 	var h = BitConverter.GetBytes(Convert.ToInt32(arrs[1]));
					                 	var wbutes = new[]{l[0],l[1],h[0],h[1]};
					                 	this.Text = BitConverter.ToSingle(wbutes,0).ToString();
					                 });
				}else{
					var l = BitConverter.GetBytes(Convert.ToInt32(arrs[0]));
					var h = BitConverter.GetBytes(Convert.ToInt32(arrs[1]));
					var wbutes = new[]{l[0],l[1],h[0],h[1]};
					this.Text = BitConverter.ToSingle(wbutes,0).ToString();
				}
			}
		}
		#endregion
		
		public KVLabel()
		{
			KVSerialPortManager.Instance.EnQueue(this);
		}
	}
}
