﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace hmitest0720
{
    public partial class Form1 : Form
    {
        public Form ParaSetting { get; set; }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ParaSetting = new ParaSetting() { FromForm = this };
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            this.ParaSetting.Show();
            this.Hide();
        }
    }
}
