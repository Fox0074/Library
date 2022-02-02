using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
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
            var allProc = Process.GetProcesses();
            var y = allProc.FirstOrDefault(x => x.ProcessName == "DayZ_x64").Id;
            address = driver.GetClientAddress(y);
            //MessageBox.Show(address.ToString());
        }

        int PosOffs1 = 67663568;
        //nEXTvECTOR3 - dIR

        int PosOffs2 = 67673944;
        Vector3 playerPos;
        Vector2 MaxCoords = new Vector2(15360, 15360);
        private unsafe void button2_Click(object sender, EventArgs e)
        {
            var offset = PosOffs1;
            var allProc = Process.GetProcesses();
            var driver = new KernalInterface("\\\\.\\guideeh");
            var proc = allProc.FirstOrDefault(x => x.ProcessName == "DayZ_x64");
            if (proc != null)
            {
                var id = (long)proc.Id;
                var X = driver.ReadVirtualMemoryFloat(id, address + offset, 4);
                var Z = driver.ReadVirtualMemoryFloat(id, address + offset + 4, 4);
                var Y = driver.ReadVirtualMemoryFloat(id, address + offset + 8, 4);

                
                playerPos = new Vector3(X,Y,Z);
                button3_Click(sender,e);
                listBox1.Items.Add("PlayerPos " + playerPos.ToString());

            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            var bitMap = new Bitmap(pictureBox1.Image);
            Vector2 size = new Vector2(bitMap.Width, bitMap.Height);
            Vector2 otnPlayerPos = new Vector2(playerPos.X / MaxCoords.X, playerPos.Y / MaxCoords.Y);
            Vector2 ScreenPlayerPos = new Vector2(size.X * otnPlayerPos.X, size.Y - size.Y * otnPlayerPos.Y);

            for (int i = (int)ScreenPlayerPos.X; i < (int)ScreenPlayerPos.X + 5; i++)
                for (int j = (int)ScreenPlayerPos.Y; j < (int)ScreenPlayerPos.Y + 5; j++)
                    bitMap.SetPixel(i, j, Color.Red);

            pictureBox1.Image = bitMap;
        }
    }
}
