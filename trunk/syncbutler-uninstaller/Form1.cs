using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SyncButler.Uninstaller
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            //MessageBox.Show("Current Working Directory: " + UninstallUtils.GetRunningDirectory());
            //MessageBox.Show("Current EXE Name: " + UninstallUtils.GetCurrentExeName());

            PopulateLists();
        }

        public void PopulateLists()
        {
            this.listBox1.Items.Clear();

            foreach (string entry in UninstallUtils.DELETE_REG_LIST)
                this.listBox1.Items.Add(entry);

            this.listBox2.Items.Clear();

            foreach (string entry in UninstallUtils.DELETE_FILE_LIST)
                this.listBox2.Items.Add(entry);

            this.listBox3.Items.Clear();

            foreach (string entry in UninstallUtils.KEEP_FILE_LIST)
                this.listBox3.Items.Add(entry);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            UninstallUtils.Uninstall();
        }
    }
}
