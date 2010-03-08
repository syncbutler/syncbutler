using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SyncButlerConsole
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        public void WriteLine(string text)
        {
            this.outputBox.AppendText("\r\n" + text);
            this.outputBox.SelectionStart = this.outputBox.Text.Length;
            this.outputBox.ScrollToCaret();
        }

        public void ClearScreen()
        {
            this.outputBox.Text = "";
        }
    }
}
