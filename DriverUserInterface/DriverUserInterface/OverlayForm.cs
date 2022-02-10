using System;
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
    public class OverlayForm : Form
    {
        protected WindowRenderTarget device;
        protected HwndRenderTargetProperties renderProperties;
        protected SolidColorBrush solidColorBrush;
        private Factory factory;

        protected TextFormat font;
        protected FontFactory fontFactory;
        protected const string fontFamily = "Arial";
        protected const float fontSize = 12.0f;

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

        public OverlayForm()
        {
            int initialStyle = GetWindowLong(this.Handle, -20);
            SetWindowLong(this.Handle, -20, initialStyle | 0x80000 | 0x20);
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
            OnResize(null);

            this.SetStyle(ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
        }

        private void Overlay_SharpDX_Load(object sender, EventArgs e)
        {
            System.Drawing.Rectangle screen = Screen.FromControl(this).Bounds;
            this.Width = screen.Width;
            this.Height = screen.Height;
            this.TransparencyKey = System.Drawing.Color.Black;

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
            base.WndProc(ref m);
            //if (m.Msg == WM_MOUSEACTIVATE)
            //{
            //    m.Result = (IntPtr)MA_NOACTIVATEANDEAT;
            //    return;
            //}
            //if (m.Msg == WM_ACTIVATE)
            //{
            //    if (((int)m.WParam & 0xFFFF) != WA_INACTIVE)
            //        if (m.LParam != IntPtr.Zero)
            //            SetActiveWindow(m.LParam);
            //        else
            //            SetActiveWindow(IntPtr.Zero);
            //}
            //else
            //{
            //    base.WndProc(ref m);
            //}
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Overlay_SharpDX_Load(sender, e);
        }
    }
}
