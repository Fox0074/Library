using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpDX.Direct2D1;
using Factory = SharpDX.Direct2D1.Factory;
using FontFactory = SharpDX.DirectWrite.Factory;
using Format = SharpDX.DXGI.Format;
using SharpDX;
using SharpDX.DirectWrite;
using System.Runtime.InteropServices;

namespace DriverUserInterface
{

    public partial class Form1 : Form
    {

        private WindowRenderTarget device;
        private HwndRenderTargetProperties renderProperties;
        private SolidColorBrush solidColorBrush;
        private Factory factory;

        private TextFormat font;
        private FontFactory fontFactory;
        private const string fontFamily = "Arial";
        private const float fontSize = 12.0f;

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("dwmapi.dll")]
        public static extern void DwmExtendFrameIntoClientArea(IntPtr hWnd, ref int[] pMargins);

        [DllImport("user32.dll")]
        private static extern IntPtr SetActiveWindow(IntPtr handle);

        //Styles
        public const UInt32 SWP_NOSIZE = 0x0001;
        public const UInt32 SWP_NOMOVE = 0x0002;
        public const UInt32 TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;
        public static IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WM_ACTIVATE = 6;
        private const int WA_INACTIVE = 0;
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATEANDEAT = 0x0004;


        public static KernalInterface _driver;
        public static long World;

        private long _baseAddres = -1;
       
        private Process _gameProcess;
        private Thread _drawingThread;

        private List<DrawingEntity> Entities = new List<DrawingEntity>();

        private bool _isActive = true;
        private bool _showPlayers = true;
        private bool _showZombie = true;
        private bool _showOther = true;
        private bool _showSettings = false;
        private bool _showNames = true;
        private bool _showItemsFilers = false;

        private int _drawDistance = 1200;

        public Form1()
        {
            int initialStyle = GetWindowLong(this.Handle, -20);
            SetWindowLong(this.Handle, -20, initialStyle | 0x80000 | 0x20);
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
            OnResize(null);

            InitializeComponent();
            _driver = new KernalInterface("\\\\.\\guideeh");
            this.SetStyle(ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            SetCheckListBoxData();
        }

        private void Overlay_SharpDX_Load(object sender, EventArgs e)
        {
            System.Drawing.Rectangle screen = Screen.FromControl(this).Bounds;
            this.Width = screen.Width;
            this.Height = screen.Height;

            this.DoubleBuffered = true; // reduce the flicker
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |// reduce the flicker too
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.DoubleBuffer |
                ControlStyles.UserPaint |
                ControlStyles.Opaque |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);
            this.TopMost = true;
            this.Visible = true;

            factory = new Factory();
            fontFactory = new FontFactory();
            renderProperties = new HwndRenderTargetProperties()
            {
                Hwnd = this.Handle,
                PixelSize = new Size2(this.Width, this.Height),
                PresentOptions = PresentOptions.None
            };


            device = new WindowRenderTarget(factory, new RenderTargetProperties(new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied)), renderProperties);
            solidColorBrush = new SolidColorBrush(device, Color.Red);
            font = new TextFormat(fontFactory, fontFamily, fontSize);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            int[] marg = new int[] { 0, 0, Width, Height };
            DwmExtendFrameIntoClientArea(this.Handle, ref marg);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams pm = base.CreateParams;
                pm.ExStyle |= 0x80;
                pm.ExStyle |= WS_EX_TOPMOST; // make the form topmost
                pm.ExStyle |= WS_EX_NOACTIVATE; // prevent the form from being activated
                return pm;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (_isActive)
            {
                base.WndProc(ref m);
                return;
            }

            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)MA_NOACTIVATEANDEAT;
                return;
            }
            if (m.Msg == WM_ACTIVATE)
            {
                if (((int)m.WParam & 0xFFFF) != WA_INACTIVE)
                    if (m.LParam != IntPtr.Zero)
                        SetActiveWindow(m.LParam);
                    else
                        SetActiveWindow(IntPtr.Zero);
            }
            else
            {
                base.WndProc(ref m);
            }
        }

        private unsafe void button2_Click(object sender, EventArgs e)
        {
            _gameProcess = Process.GetProcesses().FirstOrDefault(x => x.ProcessName == "EscapeFromTarkov.exe");
            _baseAddres = GetBaseAddres(_gameProcess);
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

            while (true)
            {
                ReadData();
                device.BeginDraw();
                device.Clear(Color.Transparent);
                device.TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Aliased;

                device.DrawText("Overlay text using direct draw with DirectX", font, new SharpDX.Mathematics.Interop.RawRectangleF(5, 100, 500, 30), solidColorBrush);

                foreach (var entity in Entities)
                {
                    if (entity.Distance <= 1)
                        continue;

                    if (!IsFiltred(entity))
                    {
                        solidColorBrush.Color = GetColorByType(entity.EntityType);
                        device.DrawRectangle(entity.DrawRect, solidColorBrush, 1);
                        device.DrawText(entity.Name + " " + ((int)entity.Distance).ToString(), font, entity.TextRect, solidColorBrush);
                    }
                }


                device.EndDraw();
            }
        }

        private bool IsFiltred(DrawingEntity entity)
        {
            if (entity.Distance > _drawDistance && entity.EntityType != EntityType.Player)
                return true;

            if (entity.EntityType == EntityType.Player && !_showPlayers)
                return true;

            if (entity.EntityType == EntityType.Zombie && !_showZombie)
                return true;

            if (entity.EntityType == EntityType.None && !_showOther)
                return true;

            if ((entity.EntityType == EntityType.None || entity.EntityType == EntityType.Animal) && !_showOther)
                return true;

            if (entity.EntityType == EntityType.Loot)
            {
                if (checkedListBox1.CheckedItems.Contains("ToggleAll"))
                    return true;
            }

            return false;
        }

        private Color GetColorByType(EntityType type)
        {
            switch (type)
            {
                case EntityType.Player:
                    return Color.Red;
                case EntityType.Zombie:
                    return Color.Blue;
                case EntityType.Car:
                    return Color.LimeGreen;
                case EntityType.Animal:
                    return Color.Violet;
                case EntityType.Loot:
                    return Color.Yellow;
                default:
                    return Color.Turquoise;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _drawingThread = new Thread(new ThreadStart(DrawThread));
            _drawingThread.Start();
            //_isActive = false;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_drawingThread != null)
                _drawingThread.Abort();
        }


        private void ReadData()
        {
            Entities.Clear();

            int NearTableSize = _driver.ReadVirtualMemory<int>(World + DayzOffs.NearEntityTable + 0x8);
            int FarTableSize = _driver.ReadVirtualMemory<int>(World + DayzOffs.FarEntityTable + 0x8);
            int ItemsTableSize = _driver.ReadVirtualMemory<int>(World + DayzOffs.ItemTables + 0x8);

            long EntityTable = _driver.ReadVirtualMemory<long>(World + DayzOffs.NearEntityTable);
            for (int i = 0; i < NearTableSize; i++)
            {
                if (EntityTable == 0) continue;

                long Entity = _driver.ReadVirtualMemory<long>(EntityTable + (i * 0x8));
                if (Entity == 0) continue;

                int networkId = _driver.ReadVirtualMemory<int>(Entity + DayzOffs.Network);

                Vector3 worldPosition = DayzSDK.GetCoordinate(Entity);
                Vector3 screenPosition = new Vector3(0, 0, 0);
                if (worldPosition.x > 17000 || worldPosition.y > 17000)
                    continue;
                DayzSDK.WorldToScreen(worldPosition, ref screenPosition);

                if (screenPosition.z < 1.0f)
                    continue;

                var distance = DayzSDK.GetDistanceToMe(worldPosition);

                if (distance < 1.0f)
                    continue;

                string entityName = DayzSDK.GetEntityTypeName(Entity);
                entityName = entityName.Replace("\0", "");
                float size = (float)Math.Ceiling(25 * (1000 - distance) / 1000);

                Entities.Add(new DrawingEntity()
                {
                    DrawRect = new RectangleF(screenPosition.x - size / 2, screenPosition.y - size / 2, size, size),
                    TextRect = new RectangleF(screenPosition.x - size / 2, screenPosition.y + size / 2, 150, 18),
                    Distance = distance,
                    Position = worldPosition,
                    Name = entityName,
                    EntityType = entityName == "dayzplayer" ? EntityType.Player : EntityType.None
                });
            }

            EntityTable = _driver.ReadVirtualMemory<long>(World + DayzOffs.FarEntityTable);
            for (int i = 0; i < FarTableSize; i++)
            {
                if (EntityTable == 0) 
                    continue;

                long Entity = _driver.ReadVirtualMemory<long>(EntityTable + (i * 0x8));
                if (Entity == 0) 
                    continue;

                int networkId = _driver.ReadVirtualMemory<int>(Entity + DayzOffs.Network);
                if (networkId == 0)
                    continue;

                Vector3 worldPosition = DayzSDK.GetCoordinate(Entity);
                Vector3 screenPosition = new Vector3(0, 0, 0);
                if (worldPosition.x > 17000 || worldPosition.y > 17000)
                    continue;

                DayzSDK.WorldToScreen(worldPosition, ref screenPosition);
                if (screenPosition.z < 1.0f)
                    continue;

                var distance = DayzSDK.GetDistanceToMe(worldPosition);
                if (distance < 1.0f)
                    continue;

                string entityName = DayzSDK.GetEntityTypeName(Entity);
                entityName = entityName.Replace("\0", "");
                float size = (float)Math.Ceiling(25 * (1000 - distance) / 1000);
                Entities.Add(new DrawingEntity()
                {
                    DrawRect = new RectangleF(screenPosition.x - size/2, screenPosition.y- size/2, size, size),
                    TextRect = new RectangleF(screenPosition.x - size / 2, screenPosition.y + size / 2, 150, 18),
                    Distance = distance,
                    Position = worldPosition,
                    Name = entityName,
                    EntityType = entityName == "dayzplayer" ? EntityType.Player : entityName == "car" ? EntityType.Car : entityName == "dayzinfected" ? EntityType.Zombie : EntityType.Animal
                });
            }

            EntityTable = _driver.ReadVirtualMemory<long>(World + DayzOffs.ItemTables);
            for (int i = 0; i < ItemsTableSize; i++)
            {
                if (EntityTable == 0) 
                    continue;

                long Entity = _driver.ReadVirtualMemory<long>(EntityTable + (i * 0x10));
                if (Entity < 10) 
                    continue;

                long visualStatePointer = _driver.ReadVirtualMemory<long>(Entity + 0x198);
                var worldPosition = _driver.ReadVirtualMemory<Vector3>(visualStatePointer + 0x2C);

                Vector3 screenPosition = new Vector3(0, 0, 0);
                DayzSDK.WorldToScreen(worldPosition, ref screenPosition);
                var distance = DayzSDK.GetDistanceToMe(worldPosition);

                if (screenPosition.z < 1.0f || distance >= _drawDistance)
                    continue;

                var firstPointer = _driver.ReadVirtualMemory<long>(Entity + 0x8);
                var secondPointer = _driver.ReadVirtualMemory<long>(firstPointer + 0x10);
                var entityName = _driver.ReadVirtualMemoryString(secondPointer, 20).ToString();
                float size = (float)Math.Ceiling(25 * (1000 - distance) / 1000);

                Entities.Add(new DrawingEntity()
                {
                    DrawRect = new RectangleF(screenPosition.x - size / 2, screenPosition.y - size / 2, size, size),
                    TextRect = new RectangleF(screenPosition.x - size / 2, screenPosition.y + size / 2, 150, 18),
                    Distance = distance,
                    Position = worldPosition,
                    Name = entityName,
                    EntityType = EntityType.Loot
                });
            }
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

        private void Form1_Load(object sender, EventArgs e)
        {
            Overlay_SharpDX_Load(sender,e);
        }
    }
}
