/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 2018/3/22
 * Time: 9:50
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Windows.Forms;

namespace KvControl
{
	/// <summary>
	/// Description of KVNumberUpDown.
	/// </summary>
	public class KVNumbericUpDown:NumericUpDown, IKvController
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

		private bool _autoSync;
		
		public bool KVAutoSync {
			get {return this._autoSync;}
			set {
				_autoSync = value;
			}
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
			if(this.Value==0){
				return string.Format("WRS {0} 2 {1} {2}\r\n",KVMemAddr,0,0);
			}
			
			var f = (float)Convert.ToDouble(this.Value);
			var bf = BitConverter.GetBytes(f);
			var l = BitConverter.ToInt32(new byte[]{bf[0],bf[1],0,0},0);
			var h = BitConverter.ToInt32(new byte[]{bf[2],bf[3],0,0},0);
			return string.Format("WRS {0} 2 {1} {2}\r\n",KVMemAddr,l,h);
		}

        public void UpdateUI(string resp)
        {
            var arrs = resp.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (arrs.Length == 0)
            {
                return;
            }

            if (arrs.Length == 1)
            {
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke((EventHandler)delegate
                    {

                        this.Value = Convert.ToInt32(arrs[0]);
                    });
                }
                else
                {
                    this.Value = Convert.ToInt32(arrs[0]);
                }
            }
            else if (arrs.Length == 2)
            {
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke((EventHandler)delegate
                    {
                        var l = BitConverter.GetBytes(Convert.ToInt32(arrs[0]));
                        var h = BitConverter.GetBytes(Convert.ToInt32(arrs[1]));
                        var wbutes = new[] { l[0], l[1], h[0], h[1] };
                        this.Value = (decimal)BitConverter.ToSingle(wbutes, 0);
                    });
                }
                else
                {
                    var l = BitConverter.GetBytes(Convert.ToInt32(arrs[0]));
                    var h = BitConverter.GetBytes(Convert.ToInt32(arrs[1]));
                    var wbutes = new[] { l[0], l[1], h[0], h[1] };
                    this.Value = (decimal)BitConverter.ToSingle(wbutes, 0);
                }
            }
        }
        #endregion

        public KVNumbericUpDown()
		{
			KVSerialPortManager.Instance.EnQueue(this);
			this.ValueChanged+=delegate{
				if(!this.KVAutoSync){
					KVSerialPortManager.Instance.AppendRequest(this.GetWriteCmd());
				}
			};
		}

        private void InitializeComponent()
        {
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
