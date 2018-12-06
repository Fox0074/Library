using System;
using System.Collections.Generic;

using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace PortScanner
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            var task = Task.Factory.StartNew(() =>
            {
                List<PortInfo> pi = PortScanner.GetOpenPort();
                foreach (PortInfo current in pi)
                {
                    listView1.BeginInvoke(new Action(() => AddPortInfo(new string[] { current.PortNumber.ToString(), current.Local, current.Remote, current.State, current.PortEndPoint.ToString() })));
                }
            });

        }

        private void AddPortInfo(string[] data)
        {
            listView1.Items.Add(new ListViewItem(data));
        }
    }
}
