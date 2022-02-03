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

        private Vector2 MaxCoords = new Vector2(15360, 15360);
        private const int PosOffs1 = 67663568; //Camera?? + 56 ->Player
        private const int PosOffs2 = 67673944;

        private long _baseAddres = -1;
        private Vector3 _playerPos;
        private Vector3 _playerDir;
        private KernalInterface _driver;
        private Process _gameProcess;
        private Thread _drawingThread;

        public Form1()
        {
            InitializeComponent();
            _driver = new KernalInterface("\\\\.\\guideeh");
            g = pictureBox1.CreateGraphics();
        }

        private unsafe void button2_Click(object sender, EventArgs e)
        {
            _gameProcess = Process.GetProcesses().FirstOrDefault(x => x.ProcessName == "EscapeFromTarkov.exe");
            _baseAddres = GetBaseAddres(_gameProcess);
        }

        private Vector3 GetPlayerPos()
        {
            var offset = PosOffs1;

            if (_gameProcess != null)
            {
                var id = (long)_gameProcess.Id;
                var X = _driver.ReadVirtualMemory<float>(id, _baseAddres + offset);
                var Z = _driver.ReadVirtualMemory<float>(id, _baseAddres + offset + 4);
                var Y = _driver.ReadVirtualMemory<float>(id, _baseAddres + offset + 8);

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
                var X = _driver.ReadVirtualMemory<float>(id, _baseAddres + offset + 12);
                var Z = _driver.ReadVirtualMemory<float>(id, _baseAddres + offset + 16);
                var Y = _driver.ReadVirtualMemory<float>(id, _baseAddres + offset + 20);


                return new Vector3(X, Y, Z);

            }
            return Vector3.Zero;
        }

        private long GetBaseAddres(Process proc)
        {
            return _driver.GetClientAddress(proc.Id);
        }

        
        private void DrawPlayerPoint()
        {
            var bitMap = new Bitmap(Properties.Resources.Screenshot_1);
            Vector2 size = new Vector2(bitMap.Width, bitMap.Height);
            Vector2 otnPlayerPos = new Vector2(_playerPos.X / MaxCoords.X, _playerPos.Y / MaxCoords.Y);
            Vector2 ScreenPlayerPos = new Vector2(g.VisibleClipBounds.Width * otnPlayerPos.X, g.VisibleClipBounds.Height - g.VisibleClipBounds.Height * otnPlayerPos.Y);
            var normDirVector = Vector2.Normalize(new Vector2(_playerDir.X, _playerDir.Y));

            g.DrawEllipse(new Pen(Color.Red, 1), new RectangleF(new Point((int)ScreenPlayerPos.X - 4 , (int)ScreenPlayerPos.Y - 4) , new SizeF(8,8)));
            g.DrawLine(new Pen(Color.Red, 2), new Point((int)ScreenPlayerPos.X, (int)ScreenPlayerPos.Y),
                new Point((int)ScreenPlayerPos.X + (int)(normDirVector.X * 20), (int)ScreenPlayerPos.Y + (int)(-normDirVector.Y * 20)));


            listBox1.BeginInvoke(new Action(() => { listBox1.Items.Clear(); listBox1.Items.Add(_playerPos.ToString()); listBox1.Items.Add(_playerDir.ToString()); }));;
            pictureBox1.BeginInvoke(new Action(() => g = pictureBox1.CreateGraphics()));
            pictureBox1.BeginInvoke(new Action(()=> pictureBox1.Image = bitMap));

        }

        private void DrawThread()
        {
            _gameProcess = Process.GetProcesses().FirstOrDefault(x => x.ProcessName == "DayZ_x64");
            _baseAddres = GetBaseAddres(_gameProcess);

            while (true)
            {
                _playerPos = GetPlayerPos();
                _playerDir = GetPlayerDir();
                DrawPlayerPoint();
                Thread.Sleep(13);//tick 13ms
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _drawingThread = new Thread(new ThreadStart(DrawThread));
            _drawingThread.Start();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            _drawingThread.Abort();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
