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

namespace MemoryWorker
{
    public partial class Form1 : Form
    {
        private List<Process> AllProcess = new List<Process>();
        private Dictionary <int,Process> _filerProc = new Dictionary<int,Process>();

        public Form1()
        {
            InitializeComponent();

            AllProcess.Clear();
            AllProcess.AddRange(Process.GetProcesses());
            listBox2.Items.AddRange(AllProcess.Select(x => x.ProcessName).ToArray());
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Program.TargetModule = Program.TargetProcess.Modules[listBox1.SelectedIndex];
            var bytes = MemoryHelper.ReadMemory(Program.TargetProcess, (long)Program.TargetModule.BaseAddress, 1000);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2") + " ");
            }
            textBox1.Text = builder.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            new InjectForm(Program.TargetProcess).Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Program.TargetModule = Program.TargetProcess.Modules[listBox1.SelectedIndex];
            List<byte> buf = new List<byte>();
            foreach (string s in textBox1.Text.Split(' '))
            {
                if (!string.IsNullOrWhiteSpace(s))
                    buf.Add(byte.Parse(s, System.Globalization.NumberStyles.HexNumber));
            }

            byte[] bytes = buf.ToArray();
            MemoryHelper.WriteMemory(Program.TargetProcess, bytes, (long)Program.TargetModule.BaseAddress);
        }



        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            Program.TargetProcess = AllProcess[_filerProc.ElementAt(listBox2.SelectedIndex).Key];
            listBox1.Items.Clear();
            foreach (ProcessModule module in Program.TargetProcess.Modules)
                listBox1.Items.Add(module.ModuleName);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            _filerProc.Clear();
            listBox2.Items.Clear();
            foreach (var proc in AllProcess)
            {
                if (proc.ProcessName.ToLower().Contains(textBox2.Text.ToLower()))
                    _filerProc.Add(AllProcess.IndexOf(proc),proc);
            }
            listBox2.Items.AddRange(_filerProc.Values.Select(x => x.ProcessName).ToArray());
        }
    }
}
