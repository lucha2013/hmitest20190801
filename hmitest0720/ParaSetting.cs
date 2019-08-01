using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using KvControl;

namespace hmitest0720
{
    public partial class ParaSetting : Form
    {
        public Form FromForm { get; set; }
        public ParaSetting()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (FromForm != null)
            {
                this.Hide();
                FromForm.Show();
            }
        }
    }
}
