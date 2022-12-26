using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ytdownload
{
    public partial class ProgressWindow : Form
    {
        public System.Windows.Forms.Label label;
        public ProgressBar progressBar;
        public ProgressWindow(string title)
        {
            InitializeComponent();
            this.label = label1;
            this.progressBar = progressBar1;
            this.TopMost = true;
            this.Text = title;
        }

        private void ProgressWindow_Load(object sender, EventArgs e)
        {

        }
    }
}
