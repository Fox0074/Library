using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
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

        private const int PosOffs1 = 67663568;
        private const int PosOffs2 = 67673944;

        private long _baseAddres = -1;
        private System.Numerics.Vector3 _playerPos;
        private System.Numerics.Vector3 _playerDir;
       
        private Process _gameProcess;
        private Thread _drawingThread;

        List<_player_t> entities = new List<_player_t>();
        List<Vector3> ItemsPos = new List<Vector3>();
        List<string> ItemsNames = new List<string>();

        private bool _showPlayers = true;
        private bool _showZombie = true;
        private bool _showOther = true;
        private bool _showSettings = false;
        private bool _showNames = true;
        private bool _showItemsFilers = false;

        private int _drawDistance = 1200;
        public Form1()
        {
            TransparencyKey = BackColor;
            TopMost = true;
            InitializeComponent();
            _driver = new KernalInterface("\\\\.\\guideeh");
            this.SetStyle(ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            g = this.CreateGraphics();
            SetCheckListBoxData();
        }

        private void SetCheckListBoxData()
        {
            checkedListBox1.Items.Add("ToggleAll");
            checkedListBox1.Items.AddRange(DyzAllItems.Weapons.ToArray());
            checkedListBox2.Items.Add("ToggleAll");
            checkedListBox2.Items.AddRange(DyzAllItems.Ammo.ToArray());
            checkedListBox3.Items.Add("ToggleAll");
            checkedListBox3.Items.AddRange(DyzAllItems.Food.ToArray());
            checkedListBox4.Items.Add("ToggleAll");
            checkedListBox4.Items.AddRange(DyzAllItems.Medical.ToArray());
            checkedListBox5.Items.Add("ToggleAll");
            checkedListBox5.Items.AddRange(DyzAllItems.Loot.ToArray());
            checkedListBox6.Items.Add("ToggleAll");
            checkedListBox6.Items.AddRange(DyzAllItems.Tools.ToArray());
            checkedListBox7.Items.Add("ToggleAll");
            checkedListBox7.Items.AddRange(DyzAllItems.Сlothes.ToArray());
            checkedListBox8.Items.Add("ToggleAll");
            checkedListBox8.Items.AddRange(DyzAllItems.WeaponDetails.ToArray());
            checkedListBox9.Items.Add("ToggleAll");
            checkedListBox9.Items.AddRange(DyzAllItems.CarDetails.ToArray());
            checkedListBox10.Items.Add("ToggleAll");
            checkedListBox10.Items.AddRange(DyzAllItems.Trash.ToArray());
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

        

        private void DrawThread()
        {
            _gameProcess = Process.GetProcesses().FirstOrDefault(x => x.ProcessName == "DayZ_x64");
            _driver._processId = _gameProcess.Id;
            _baseAddres = GetBaseAddres(_gameProcess);
            World = _driver.ReadVirtualMemory<long>(_baseAddres + DayzOffs.World);
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;

            while (true)
            {
                ReadData();
                ShowEntity();
                Thread.Sleep(26);//tick 13ms
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //this.Enabled = false;
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
            ItemsNames.Clear();

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
                //long Entity2 = _driver.ReadVirtualMemory<long>(EntityTable + (i * 0x10));
                if (Entity < 10) continue;

                // checking if player even networked
                long visualStatePointer = _driver.ReadVirtualMemory<long>(Entity + 0x198);
                //if (networkId == 0) continue;
                var pos = _driver.ReadVirtualMemory<Vector3>(visualStatePointer + 0x2C);
                var posSystem = new System.Numerics.Vector3(pos.x, pos.y, pos.z);
                if (Math.Abs(posSystem.Length()) > 10 && Math.Abs(posSystem.Length() - _playerPos.Length()) > 2)
                {
                    ItemsPos.Add(pos);
                    var firstPointer = _driver.ReadVirtualMemory<long>(Entity + 0x8);
                    var secondPointer = _driver.ReadVirtualMemory<long>(firstPointer + 0x10);
                    var name = _driver.ReadVirtualMemoryString(secondPointer, 20).ToString(); 
                    ItemsNames.Add(name);
                }
            }

            entities = tmp;
        }



        private void ShowEntity()
        {
           this.BeginInvoke(new Action(() => this.Invalidate()));

            List<KeyValuePair<Rectangle, float>> drawingZ = new List<KeyValuePair<Rectangle, float>>();
            List<KeyValuePair<Rectangle, float>> drawingP = new List<KeyValuePair<Rectangle, float>>();
            List<KeyValuePair<Rectangle, float>> drawingO = new List<KeyValuePair<Rectangle, float>>();

            for (int i = 0; i < entities.Count; i++)
            {
                var Entities = entities[i];

                Vector3 worldPosition = DayzSDK.GetCoordinate(Entities.EntityPtr);
                Vector3 screenPosition = new Vector3(0,0,0);
                if (worldPosition.x > 17000 || worldPosition.y > 17000)
                    continue;
                DayzSDK.WorldToScreen(worldPosition, ref screenPosition);

                if (screenPosition.z < 1.0f ) 
                    continue;

                var distance = DayzSDK.GetDistanceToMe(worldPosition);

                if (distance < 1.0f)
                    continue;

                string entity = DayzSDK.GetEntityTypeName(Entities.EntityPtr);
                entity = entity.Replace('\0', ' ');
                //g.DrawRectangle(new Pen(entity == "dayzplayer " ? Color.Red : entity == "dayzinfected " ? Color.Blue : Color.Violet, entity == "dayzplayer " ? 5 : 3), new Rectangle(new Point((int)screenPosition.x, (int)screenPosition.y), new Size(50, 50)));
                
                switch(entity)
                {
                    case "dayzplayer ":
                        drawingP.Add( new KeyValuePair<Rectangle, float>( new Rectangle(new Point((int)screenPosition.x - 25, (int)screenPosition.y - 50), new Size(Math.Max(1,(int)(25 * ((1000 - distance) / 1000))), Math.Max(1, (int)(50 * ((1000 - distance) / 1000))))), distance));
                        break;
                    case "dayzinfected ":
                        if (distance > _drawDistance)
                            continue;
                        drawingZ.Add(new KeyValuePair<Rectangle, float>(new Rectangle(new Point((int)screenPosition.x - 25, (int)screenPosition.y - 50), new Size(Math.Max(1, (int)(25 * ((1000 - distance) / 1000))), Math.Max(1, (int)(50 * ((1000 - distance) / 1000))))), distance));
                        break;
                    default:
                        if (distance > _drawDistance)
                            continue;
                        drawingO.Add(new KeyValuePair<Rectangle, float>(new Rectangle(new Point((int)screenPosition.x - 25, (int)screenPosition.y - 50), new Size(Math.Max(1, (int)(25* ((1000 - distance) / 1000))), Math.Max(1, (int)(50 * ((1000 - distance) / 1000))))), distance));
                        break;
                }
            }

            SolidBrush drawBrush = new SolidBrush(Color.White);
            if (drawingP.Count > 0 && _showPlayers)
            {
                this.BeginInvoke(new Action(() => g.DrawRectangles(new Pen(Color.Red, 1), drawingP.Select(x => x.Key).ToArray())));
                for (int i = 0 ; i < drawingP.Count; i++)
                {
                    var k = i;
                    this.BeginInvoke(new Action(() => g.DrawString(((int)drawingP[k].Value).ToString(), new Font("Arial", 10), drawBrush, drawingP[k].Key.X, drawingP[k].Key.Y - 20)));
                }
            }
            if (drawingZ.Count > 0 && _showZombie)
            {
                this.BeginInvoke(new Action(() => g.DrawRectangles(new Pen(Color.Blue, 1), drawingZ.Select(x => x.Key).ToArray())));
                for (int i = 0; i < drawingZ.Count; i++)
                {
                    var k = i;
                    this.BeginInvoke(new Action(() => g.DrawString(((int)drawingZ[k].Value).ToString(), new Font("Arial", 10), drawBrush, drawingZ[k].Key.X, drawingZ[k].Key.Y - 20)));
                }
            }
            if (drawingO.Count > 0 && _showOther)
            {
                this.BeginInvoke(new Action(() => g.DrawRectangles(new Pen(Color.Violet, 1), drawingO.Select(x => x.Key).ToArray())));
                for (int i = 0; i < drawingO.Count; i++)
                {
                    var k = i;
                    this.BeginInvoke(new Action(() => g.DrawString(((int)drawingO[k].Value).ToString(), new Font("Arial", 10), drawBrush, drawingO[k].Key.X, drawingO[k].Key.Y - 20)));
                }
            }

            List<Rectangle> isemsPos = new List<Rectangle>();
            for (int i = 0; i < ItemsPos.Count; i++)
            {
                try
                {
                    Vector3 worldPosition = ItemsPos[i];
                    var name = ItemsNames[i].Replace("\0","");
                    Vector3 screenPosition = new Vector3(0, 0, 0);
                    DayzSDK.WorldToScreen(worldPosition, ref screenPosition);

                    if (screenPosition.z < 1.0f)
                        continue;

                    var distance = DayzSDK.GetDistanceToMe(worldPosition);
                    if (distance <= _drawDistance)
                    {
                        if (_showNames)
                            this.BeginInvoke(new Action(() => g.DrawString((name + " " + (int)distance).ToString(), new Font("Arial", 10), drawBrush, screenPosition.x, screenPosition.y - 20)));
                        isemsPos.Add(new Rectangle(new Point((int)screenPosition.x - 25, (int)screenPosition.y - 50), new Size(Math.Min(25, Math.Max(1, (int)(25 * ((1000 - distance) / 1000)))), Math.Min(25, Math.Max(1, (int)(50 * ((1000 - distance) / 1000)))))));
                    }
                }
                catch
                {
                }
            }

            if (isemsPos.Count > 0)
                this.BeginInvoke(new Action(() => g.DrawRectangles(new Pen(Color.Yellow, 1), isemsPos.ToArray())));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _showPlayers = !_showPlayers;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            _showZombie = !_showZombie;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            _showSettings = !_showSettings;
            button1.Visible = _showSettings;
            button4.Visible = _showSettings;
            button7.Visible = _showSettings;
            button8.Visible = _showSettings;
            button9.Visible = _showSettings;
            trackBar2.Visible = _showSettings;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            _showOther = !_showOther;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            _showNames = !_showNames;
        }

        private void trackBar2_ValueChanged(object sender, EventArgs e)
        {
            _drawDistance = trackBar2.Value;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            _showItemsFilers = !_showItemsFilers;
            checkedListBox1.Visible = _showItemsFilers;
            checkedListBox2.Visible = _showItemsFilers;
            checkedListBox3.Visible = _showItemsFilers;
            checkedListBox4.Visible = _showItemsFilers;
            checkedListBox5.Visible = _showItemsFilers;
            checkedListBox6.Visible = _showItemsFilers;
            checkedListBox7.Visible = _showItemsFilers;
            checkedListBox8.Visible = _showItemsFilers;
            checkedListBox9.Visible = _showItemsFilers;
            checkedListBox10.Visible = _showItemsFilers;
        }
    }
}
