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
        public static KernalInterface _driver;
        public static long World;

        private static Graphics g;
        private static PictureBox p;

        private System.Numerics.Vector2 MaxCoords = new System.Numerics.Vector2(15360, 15360);
        private const int PosOffs1 = 67663568;
        private const int PosOffs2 = 67673944;

        private long _baseAddres = -1;
        private System.Numerics.Vector3 _playerPos;
        private System.Numerics.Vector3 _playerDir;
       
        private Process _gameProcess;
        private Thread _drawingThread;

        List<_player_t> entities = new List<_player_t>();
        List<Vector3> ItemsPos = new List<Vector3>();
        Bitmap bitMap;
        private float _mapScale = 1;
        public Form1()
        {
            InitializeComponent();
            _driver = new KernalInterface("\\\\.\\guideeh");
            this.SetStyle(ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            g = pictureBox1.CreateGraphics();
            bitMap = new Bitmap(Properties.Resources.Screenshot_1);
            p = pictureBox1;
        }

        private unsafe void button2_Click(object sender, EventArgs e)
        {
            _gameProcess = Process.GetProcesses().FirstOrDefault(x => x.ProcessName == "EscapeFromTarkov.exe");
            _baseAddres = GetBaseAddres(_gameProcess);
        }

        private System.Numerics.Vector3 GetPlayerPos()
        {
            var offset = PosOffs1;

            if (_gameProcess != null)
            {
                var id = (long)_gameProcess.Id;
                var X = _driver.ReadVirtualMemory<float>(id, _baseAddres + offset);
                var Z = _driver.ReadVirtualMemory<float>(id, _baseAddres + offset + 4);
                var Y = _driver.ReadVirtualMemory<float>(id, _baseAddres + offset + 8);

                return new System.Numerics.Vector3(X, Y, Z);
                
            }
            return System.Numerics.Vector3.Zero;
        }

        private System.Numerics.Vector3 GetPlayerDir()
        {
            var offset = PosOffs1;

            if (_gameProcess != null)
            {
                var id = (long)_gameProcess.Id;
                var X = _driver.ReadVirtualMemory<float>(id, _baseAddres + offset + 12);
                var Z = _driver.ReadVirtualMemory<float>(id, _baseAddres + offset + 16);
                var Y = _driver.ReadVirtualMemory<float>(id, _baseAddres + offset + 20);


                return new System.Numerics.Vector3(X, Y, Z);

            }
            return System.Numerics.Vector3.Zero;
        }

        private long GetBaseAddres(Process proc)
        {
            return _driver.GetClientAddress(proc.Id);
        }

        
        private void DrawPlayerPoint()
        {
            g = pictureBox1.CreateGraphics();
            System.Numerics.Vector2 otnPlayerPos = new System.Numerics.Vector2(_playerPos.X / MaxCoords.X, _playerPos.Y / MaxCoords.Y);
            System.Numerics.Vector2 ScreenPlayerPos = new System.Numerics.Vector2(g.VisibleClipBounds.Width * otnPlayerPos.X, g.VisibleClipBounds.Height - g.VisibleClipBounds.Height * otnPlayerPos.Y);
            var normDirVector = System.Numerics.Vector2.Normalize(new System.Numerics.Vector2(_playerDir.X, _playerDir.Y));


            g.DrawEllipse(new Pen(Color.Red, 1), new RectangleF(new Point((int)ScreenPlayerPos.X - 4 , (int)ScreenPlayerPos.Y - 4) , new SizeF(8,8)));
            g.DrawLine(new Pen(Color.Red, 2), new Point((int)ScreenPlayerPos.X, (int)ScreenPlayerPos.Y),
                new Point((int)ScreenPlayerPos.X + (int)(normDirVector.X * 20), (int)ScreenPlayerPos.Y + (int)(-normDirVector.Y * 20)));
        }

        private void DrawThread()
        {
            _gameProcess = Process.GetProcesses().FirstOrDefault(x => x.ProcessName == "DayZ_x64");
            _driver._processId = _gameProcess.Id;
            _baseAddres = GetBaseAddres(_gameProcess);
            World = _driver.ReadVirtualMemory<long>(_baseAddres + DayzOffs.World);

            while (true)
            {
                //pictureBox1.Invalidate();
                //pictureBox1.BeginInvoke(new Action(() => pictureBox1.Image = bitMap));
                if (_mapScale > 1)
                {
                    Vector2 newSize = new Vector2((int)(bitMap.Width / _mapScale), (int)(bitMap.Height / _mapScale));
                    System.Numerics.Vector2 otnPlayerPos = new System.Numerics.Vector2(_playerPos.X / MaxCoords.X, _playerPos.Y / MaxCoords.Y);

                    var left = (bitMap.Width - newSize.x) * otnPlayerPos.X;
                    var top = (bitMap.Height - newSize.y) * (1 - otnPlayerPos.Y);

                    var newBitMap = bitMap.Clone(new Rectangle((int)left, (int)top, (int)newSize.x, (int)newSize.y), bitMap.PixelFormat);
                    p.Image = newBitMap;
                }
                else
                    p.Image = bitMap;
                _playerPos = GetPlayerPos();
                _playerDir = GetPlayerDir();
                ReadData();
                Test();
                DrawPlayerPoint();
                Thread.Sleep(60);//tick 13ms

            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _drawingThread = new Thread(new ThreadStart(DrawThread));
            _drawingThread.Start();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_drawingThread != null)
                _drawingThread.Abort();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }


        private void ReadData()
        {
            List<_player_t> tmp = new List<_player_t>();
            ItemsPos.Clear();

            int NearTableSize = _driver.ReadVirtualMemory<int>(World + DayzOffs.NearEntityTable + 0x8);
            int FarTableSize = _driver.ReadVirtualMemory<int>(World + DayzOffs.FarEntityTable + 0x8);
            int ItemsTableSize = _driver.ReadVirtualMemory<int>(World + DayzOffs.ItemTables + 0x8);

            long EntityTable = _driver.ReadVirtualMemory<long>(World + DayzOffs.NearEntityTable);
            for (int i = 0; i < NearTableSize; i++)
            {
                if (EntityTable == 0) continue;

                long Entity = _driver.ReadVirtualMemory<long>(EntityTable + (i * 0x8));
                if (Entity == 0) continue;

                // checking if player even networked
                int networkId = _driver.ReadVirtualMemory<int>(Entity + DayzOffs.Network);
                //if (networkId == 0) continue;

                _player_t Player = new _player_t();
                Player.NetworkID = networkId;
                Player.TableEntry = EntityTable;
                Player.EntityPtr = Entity;

                // adds info to the vector
                tmp.Add(Player);
            }

            EntityTable = _driver.ReadVirtualMemory<long>(World + DayzOffs.FarEntityTable);
            for (int i = 0; i < FarTableSize; i++)
            {
                
                if (EntityTable == 0) continue;

                long Entity = _driver.ReadVirtualMemory<long>(EntityTable + (i * 0x8));
                if (Entity == 0) continue;

                int networkId = _driver.ReadVirtualMemory<int>(Entity + DayzOffs.Network);
                //if (networkId == 0) continue;
                

                _player_t Player = new _player_t();
                Player.NetworkID = networkId;
                Player.TableEntry = EntityTable;
                Player.EntityPtr = Entity;
                tmp.Add(Player);
            }

            EntityTable = _driver.ReadVirtualMemory<long>(World + DayzOffs.ItemTables);
            for (int i = 0; i < ItemsTableSize; i++)
            {
                if (EntityTable == 0) continue;

                long Entity = _driver.ReadVirtualMemory<long>(EntityTable + (i * 0x10));
                long Entity2 = _driver.ReadVirtualMemory<long>(EntityTable + (i * 0x10));
                if (Entity < 10) continue;

                // checking if player even networked
                long visualStatePointer = _driver.ReadVirtualMemory<long>(Entity + 0x198);
                //if (networkId == 0) continue;
                var pos = _driver.ReadVirtualMemory<Vector3>(visualStatePointer + 0x2C);
                var posSystem = new System.Numerics.Vector3(pos.x, pos.y, pos.z);
                if (Math.Abs(posSystem.Length()) > 10 && Math.Abs(posSystem.Length() - _playerPos.Length()) > 2)
                    ItemsPos.Add(pos);
            }

            entities = tmp;
        }

        private void Test()
        {
           listBox1.BeginInvoke(new Action(() => listBox1.Items.Clear()));
            

            for (int i = 0; i < entities.Count; i++)
            {
                var Entities = entities[i];

                Vector3 worldPosition = DayzSDK.GetCoordinate(Entities.EntityPtr);
                Vector3 screenPosition = new Vector3(0,0,0);
                if (worldPosition.x > 17000 || worldPosition.y > 17000)
                    continue;
                DayzSDK.WorldToScreen(worldPosition, ref screenPosition);

                //if (screenPosition.z < 1.0f) continue;

                var distance = DayzSDK.GetDistanceToMe(worldPosition);
                string entity = DayzSDK.GetEntityTypeName(Entities.EntityPtr);
                entity = entity.Replace('\0', ' ');
                var scalModifler = new System.Numerics.Vector2(worldPosition.x - _playerPos.X, worldPosition.y - _playerPos.Y);
                worldPosition.x += (scalModifler * _mapScale - scalModifler).X;
                worldPosition.y += (scalModifler * _mapScale - scalModifler).Y;
                System.Numerics.Vector2 otnPlayerPos = new System.Numerics.Vector2(worldPosition.x / MaxCoords.X, worldPosition.y / MaxCoords.Y) ;
                System.Numerics.Vector2 ScreenPlayerPos = new System.Numerics.Vector2(g.VisibleClipBounds.Width * otnPlayerPos.X, g.VisibleClipBounds.Height - g.VisibleClipBounds.Height * otnPlayerPos.Y);
                g.DrawEllipse(new Pen(entity == "dayzplayer " ? Color.Red : Color.Blue, entity == "dayzplayer " ? 5 : 1), new RectangleF(new Point((int)ScreenPlayerPos.X - 1, (int)ScreenPlayerPos.Y - 1), new SizeF(2, 2)));


                listBox1.BeginInvoke(new Action(() => 
                {
                    //listBox1.Items.Add(entity + ": " + worldPosition.x.ToString() + " " + worldPosition.y.ToString() + " " + worldPosition.z.ToString());
                    //listBox1.Items.Add(worldPosition.x.ToString() + " " + worldPosition.y.ToString() + " " + worldPosition.z.ToString());
                })); ;
            }

            for (int i = 0; i < ItemsPos.Count; i++)
            {
                try
                {
                    Vector3 worldPosition = ItemsPos[i];
                    var scalModifler = new System.Numerics.Vector2(worldPosition.x - _playerPos.X, worldPosition.y - _playerPos.Y);
                    worldPosition.x += (scalModifler * _mapScale - scalModifler).X;
                    worldPosition.y += (scalModifler * _mapScale - scalModifler).Y;
                    System.Numerics.Vector2 otnPlayerPos = new System.Numerics.Vector2(worldPosition.x / MaxCoords.X, worldPosition.y / MaxCoords.Y);
                    System.Numerics.Vector2 ScreenPlayerPos = new System.Numerics.Vector2(g.VisibleClipBounds.Width * otnPlayerPos.X, g.VisibleClipBounds.Height - g.VisibleClipBounds.Height * otnPlayerPos.Y);
                    g.DrawEllipse(new Pen(Color.Yellow, 1), new RectangleF(new Point((int)ScreenPlayerPos.X - 1, (int)ScreenPlayerPos.Y - 1), new SizeF(2, 2)));
                    listBox1.BeginInvoke(new Action(() =>
                    {
                        listBox1.Items.Add(worldPosition.x.ToString() + " " + worldPosition.y.ToString() + " " + worldPosition.z.ToString());
                    }));
                }
                catch
                { 
                }
            }
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            _mapScale = 1 + (trackBar1.Value / 10f);
        }
    }
}
