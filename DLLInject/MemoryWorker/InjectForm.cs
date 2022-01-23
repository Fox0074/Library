using Injector;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace MemoryWorker
{
    public partial class InjectForm : Form
    {
        private string _dllName;
        private Process _process;

        public InjectForm(Process process)
        {
            _process = process;
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_dllName))
            {
                DllInjectionResult result = Injection.Inject(_process, _dllName);
                MessageBox.Show(result.ToString());
                this.Close();
            }
            else
            {
                MessageBox.Show("Не выбрана библиотека");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;

            _dllName = openFileDialog1.FileName;
        }
    }
}
