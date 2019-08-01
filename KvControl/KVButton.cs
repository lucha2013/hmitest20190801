/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 2018/3/21
 * Time: 15:53
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Drawing;
using System.Windows.Forms;
namespace KvControl
{
	/// <summary>
	/// Description of KVButton.
	/// </summary>
	public class KVButton:Button,IKvController
	{
		#region IKvBox implementation

		public string KVMemAddr {
			get ;
			set ;
		}

		public int KVByteLen {
			get{return 1;}
			set {return;}
		}

		public bool KVAutoSync {
			get ;
			set ;
		}

		public bool KVReadOnly {
			get{return true;}
			set {return;}
		}
		
		public Color KVTrueColor{
			get;set;
		}
		
		public Color KVFalseColor{
			get;set;		}
		
		public string GetReadCmd()
		{
			if(string.IsNullOrEmpty(KVMemAddr)){
				return string.Empty;
			}
			return "RD "+KVMemAddr + "\r\n";
			
		}

		public string GetWriteCmd()
		{
			if(string.IsNullOrEmpty(KVMemAddr)){
				return string.Empty;
			}
			if(this.BackColor==this.KVFalseColor){
				return "ST "+this.KVMemAddr+"\r\n";//Set True
			}else{
				return "RS "+this.KVMemAddr+"\r\n";//Set False
			}
		}
		
		public void UpdateUI(string resp){
			if(resp.StartsWith("E1")){
				throw new Exception(this.GetReadCmd());
			}
			var arrs = resp.Split(new[]{" "},StringSplitOptions.RemoveEmptyEntries);
			if(arrs.Length==0){
				return;
			}
			
			if(arrs.Length==1){
				if(this.IsHandleCreated){
					this.BeginInvoke((EventHandler)delegate{
					                 	var bl = Convert.ToInt32(arrs[0]);
					                 	if(bl==1){
					                 		this.BackColor = this.KVTrueColor;
					                 	}else{
					                 		this.BackColor = this.KVFalseColor;
					                 	}
					                 });
				}else{
					var bl = Convert.ToInt32(arrs[0]);
					if(bl==1){
						this.BackColor = this.KVTrueColor;
					}else{
						this.BackColor = this.KVFalseColor;
					}
				}
				
			}
		}

		#endregion

		public KVButton()
		{
			KVSerialPortManager.Instance.EnQueue(this);
			this.Click+=(EventHandler)delegate{
				KVSerialPortManager.Instance.AppendRequest(this.GetWriteCmd());
				this.BackColor = (this.BackColor==KVFalseColor)?KVTrueColor:KVFalseColor;
			};
		}
	}
}
