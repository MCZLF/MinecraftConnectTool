using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinecraftConnectTool
{
    public partial class lanload : UserControl
    {
        public lanload()
        {
            InitializeComponent();
            this.Load += Lanload_Load;
        }

        private void lan1_Click(object sender, EventArgs e)
        {

        }
        private void Lanload_Load(object sender, EventArgs e)
        {
            bool datasaving = Form1.config.read<bool>("datasaving", false);
            if (datasaving)
            { label1.Visible = true; }
        }
    }
}
