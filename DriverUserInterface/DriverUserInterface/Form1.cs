using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DriverUserInterface
{
    public partial class Form1 : Form
    {
        long address = 0;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var driver = new KernalInterface("\\\\.\\guideeh");
            address = driver.GetClientAddress();
            MessageBox.Show(address.ToString());
        }

        private unsafe void button2_Click(object sender, EventArgs e)
        {
            var allProc = Process.GetProcesses();
            var driver = new KernalInterface("\\\\.\\guideeh");
            var proc = allProc.FirstOrDefault(x => x.ProcessName == "PlayerMove");
            if (proc != null)
            {
                var id = (long)proc.Id;
                var value = driver.ReadVirtualMemory(id, address, 100);
            }

        }
    }
}
