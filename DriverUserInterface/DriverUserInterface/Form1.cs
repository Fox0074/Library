using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace DriverUserInterface
{
    public partial class Form1 : Form
    {
        private static Graphics g;

        private long baseAddres = -1;
        private int PosOffs1 = 67663568;
        private int PosOffs2 = 67673944;


        private Vector3 playerPos;
        private Vector3 playerDir;
        private Vector2 MaxCoords = new Vector2(15360, 15360);
        private KernalInterface driver;
        private Process _gameProcess;
        private Thread _thread;

        public Form1()
        {
            InitializeComponent();
            driver = new KernalInterface("\\\\.\\guideeh");
            g = pictureBox1.CreateGraphics();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _gameProcess = Process.GetProcesses().FirstOrDefault(x => x.ProcessName == "DayZ_x64");
            baseAddres = GetBaseAddres(_gameProcess);
        }

        private unsafe void button2_Click(object sender, EventArgs e)
        {
            _gameProcess = Process.GetProcesses().FirstOrDefault(x => x.ProcessName == "EscapeFromTarkov.exe");
            baseAddres = GetBaseAddres(_gameProcess);
        }

        private Vector3 GetPlayerPos()
        {
            var offset = PosOffs1;

            if (_gameProcess != null)
            {
                var id = (long)_gameProcess.Id;
                var X = driver.ReadVirtualMemoryAny<float>(id, baseAddres + offset);
                var Z = driver.ReadVirtualMemoryAny<float>(id, baseAddres + offset + 4);
                var Y = driver.ReadVirtualMemoryAny<float>(id, baseAddres + offset + 8);

                return new Vector3(X, Y, Z);
                
            }
            return Vector3.Zero;
        }

        private Vector3 GetPlayerDir()
        {
            var offset = PosOffs1;

            if (_gameProcess != null)
            {
                var id = (long)_gameProcess.Id;
                var X = driver.ReadVirtualMemoryAny<float>(id, baseAddres + offset + 12);
                var Z = driver.ReadVirtualMemoryAny<float>(id, baseAddres + offset + 16);
                var Y = driver.ReadVirtualMemoryAny<float>(id, baseAddres + offset + 20);


                return new Vector3(X, Y, Z);

            }
            return Vector3.Zero;
        }

        private long GetBaseAddres(Process proc)
        {
            return driver.GetClientAddress(proc.Id);
        }

        
        private void DrawPlayerPoint()
        {
            var bitMap = new Bitmap(Properties.Resources.Screenshot_1);
            Vector2 size = new Vector2(bitMap.Width, bitMap.Height);
            Vector2 otnPlayerPos = new Vector2(playerPos.X / MaxCoords.X, playerPos.Y / MaxCoords.Y);
            Vector2 ScreenPlayerPos = new Vector2(size.X * otnPlayerPos.X, size.Y - size.Y * otnPlayerPos.Y);

            for (int i = (int)ScreenPlayerPos.X; i < (int)ScreenPlayerPos.X + 5; i++)
                for (int j = (int)ScreenPlayerPos.Y; j < (int)ScreenPlayerPos.Y + 5; j++)
                    if (i < size.X && j < size.Y)
                    bitMap.SetPixel(i, j, Color.Red);

            var normDirVector = Vector2.Normalize(new Vector2(playerDir.X , playerDir.Y));
            ScreenPlayerPos = new Vector2(g.VisibleClipBounds.Width * otnPlayerPos.X, g.VisibleClipBounds.Height - g.VisibleClipBounds.Height * otnPlayerPos.Y);
            g.DrawLine(new Pen(Color.Red, 2), new Point((int)ScreenPlayerPos.X + 3, (int)ScreenPlayerPos.Y + 3),
                new Point((int)ScreenPlayerPos.X + (int)(normDirVector.X * 20), (int)ScreenPlayerPos.Y + (int)(-normDirVector.Y * 20)));


            listBox1.BeginInvoke(new Action(() => { listBox1.Items.Clear(); listBox1.Items.Add(playerPos.ToString()); listBox1.Items.Add(playerDir.ToString()); }));;
            pictureBox1.BeginInvoke(new Action(() => g = pictureBox1.CreateGraphics()));
            pictureBox1.BeginInvoke(new Action(()=> pictureBox1.Image = bitMap));

        }

        private void DrawThread()
        {
            while (true)
            {
                playerPos = GetPlayerPos();
                playerDir = GetPlayerDir();
                DrawPlayerPoint();
                Thread.Sleep(13);//tick 13ms
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _thread = new Thread(new ThreadStart(DrawThread));
            _thread.Start();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            _thread.Abort();
        }
    }
}
