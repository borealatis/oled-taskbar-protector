// OledDimmer.cs — C# 5 compatible
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace OledDimmer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApp());
        }
    }

    class DimOverlay : Form
    {
        [DllImport("user32.dll")] static extern IntPtr FindWindow(string c, string t);
        [DllImport("user32.dll")] static extern bool   GetClientRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] static extern IntPtr SetParent(IntPtr child, IntPtr parent);
        [DllImport("user32.dll")] static extern bool   SetWindowPos(IntPtr h, IntPtr ins, int x, int y, int w, int ht, uint f);
        [DllImport("user32.dll")] static extern int    SetWindowLong(IntPtr h, int idx, int val);
        [DllImport("user32.dll")] static extern int    GetWindowLong(IntPtr h, int idx);
        [DllImport("user32.dll")] static extern bool   SetLayeredWindowAttributes(IntPtr h, uint key, byte alpha, uint flags);

        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int L, T, R, B; }

        const int  GWL_EXSTYLE      = -20;
        const int  GWL_STYLE        = -16;
        const int  WS_EX_LAYERED    = 0x80000;
        const int  WS_EX_TRANSPARENT= 0x20;
        const int  WS_EX_TOOLWINDOW = 0x80;
        const int  WS_EX_NOACTIVATE = 0x8000000;
        const int  WS_CHILD         = 0x40000000;
        const uint SWP_NOACTIVATE   = 0x10;
        const uint LWA_ALPHA        = 0x2;
        static readonly IntPtr HWND_TOP = IntPtr.Zero;

        private byte   _alpha  = 180;
        private Timer  _timer;
        private IntPtr _taskbarHwnd = IntPtr.Zero;

        public byte DimAlpha
        {
            get { return _alpha; }
            set { _alpha = value; if (this.IsHandleCreated) SetLayeredWindowAttributes(this.Handle, 0, _alpha, LWA_ALPHA); }
        }

        public bool UserActive
        {
            get { return this.Visible; }
            set
            {
                if (!this.IsHandleCreated) return;
                if (value) { SetLayeredWindowAttributes(this.Handle, 0, _alpha, LWA_ALPHA); this.Show(); }
                else         this.Hide();
            }
        }

        public DimOverlay()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar   = false;
            this.BackColor       = Color.Black;
            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Tick += new EventHandler(OnTick);
            _timer.Start();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            int ex = GetWindowLong(this.Handle, GWL_EXSTYLE);
            SetWindowLong(this.Handle, GWL_EXSTYLE,
                ex | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
            SetLayeredWindowAttributes(this.Handle, 0, _alpha, LWA_ALPHA);
            AttachToTaskbar();
        }

        void AttachToTaskbar()
        {
            _taskbarHwnd = FindWindow("Shell_TrayWnd", null);
            if (_taskbarHwnd == IntPtr.Zero) return;
            int style = GetWindowLong(this.Handle, GWL_STYLE);
            SetWindowLong(this.Handle, GWL_STYLE, style | WS_CHILD);
            SetParent(this.Handle, _taskbarHwnd);
            FitToTaskbar();
        }

        void FitToTaskbar()
        {
            if (_taskbarHwnd == IntPtr.Zero) return;
            RECT r;
            if (!GetClientRect(_taskbarHwnd, out r)) return;
            SetWindowPos(this.Handle, HWND_TOP, 0, 0, r.R - r.L, r.B - r.T, SWP_NOACTIVATE);
        }

        void OnTick(object s, EventArgs e)
        {
            IntPtr current = FindWindow("Shell_TrayWnd", null);
            if (current != _taskbarHwnd) { _taskbarHwnd = current; if (_taskbarHwnd != IntPtr.Zero) AttachToTaskbar(); }
            else FitToTaskbar();
        }

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override void Dispose(bool d) { if (d && _timer != null) _timer.Dispose(); base.Dispose(d); }
    }

    class TaskbarMouseWatcher : IDisposable
    {
        [DllImport("user32.dll")] static extern bool   GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] static extern bool   GetCursorPos(out POINT p);
        [DllImport("user32.dll")] static extern IntPtr FindWindow(string c, string t);
        [StructLayout(LayoutKind.Sequential)] struct RECT  { public int L, T, R, B; }
        [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }

        public event Action MouseEntered;
        public event Action MouseLeft;

        private Timer _timer;
        private bool  _wasOver = false;

        public TaskbarMouseWatcher()
        {
            _timer = new Timer();
            _timer.Interval = 200;
            _timer.Tick += new EventHandler(OnTick);
        }

        public void Start() { _timer.Start(); }
        public void Stop()  { _timer.Stop(); _wasOver = false; }

        void OnTick(object s, EventArgs e)
        {
            IntPtr tb = FindWindow("Shell_TrayWnd", null);
            if (tb == IntPtr.Zero) return;
            RECT r; if (!GetWindowRect(tb, out r)) return;
            POINT p; if (!GetCursorPos(out p)) return;
            bool isOver = p.X >= r.L && p.X <= r.R && p.Y >= r.T && p.Y <= r.B;
            if (isOver && !_wasOver)      { _wasOver = true;  if (MouseEntered != null) MouseEntered(); }
            else if (!isOver && _wasOver) { _wasOver = false; if (MouseLeft    != null) MouseLeft();    }
        }

        public void Dispose() { if (_timer != null) _timer.Dispose(); }
    }

    class SettingsForm : Form
    {
        public event Action<byte> AlphaChanged;
        public event Action<bool> ToggleChanged;
        public event Action<bool> MouseWatchChanged;
        public event Action       QuitRequested;

        private TrackBar _slider;
        private Label    _lblPct;
        private Button   _btnToggle;
        private CheckBox _chkMouseWatch;
        private CheckBox _chkAutoStart;
        private bool     _active;

        const string RUN_KEY  = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        const string APP_NAME = "OledDimmer";

        public SettingsForm(byte currentAlpha, bool active, bool mouseWatch)
        {
            _active = active;
            this.Text            = "OLED Taskbar Dimmer";
            this.Size            = new Size(430, 320);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.BackColor       = Color.FromArgb(18, 18, 18);
            this.ForeColor       = Color.FromArgb(220, 220, 220);
            this.ShowInTaskbar   = true;
            BuildUI(currentAlpha, mouseWatch);
        }

        void BuildUI(byte cur, bool mouseWatch)
        {
            Label lblTitle = new Label();
            lblTitle.Text      = "OLED Taskbar Dimmer";
            lblTitle.Font      = new Font("Segoe UI", 13, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location  = new Point(20, 20);
            lblTitle.AutoSize  = true;

            Label lblSlider = new Label();
            lblSlider.Text      = "Dimming Level";
            lblSlider.Font      = new Font("Segoe UI", 9, FontStyle.Bold);
            lblSlider.ForeColor = Color.FromArgb(255, 100, 30);
            lblSlider.Location  = new Point(22, 64);
            lblSlider.AutoSize  = true;

            _lblPct = new Label();
            _lblPct.Text      = ((int)(cur / 2.55)).ToString() + "%";
            _lblPct.Font      = new Font("Segoe UI", 11, FontStyle.Bold);
            _lblPct.ForeColor = Color.White;
            _lblPct.Location  = new Point(350, 60);
            _lblPct.Size      = new Size(54, 24);
            _lblPct.TextAlign = ContentAlignment.MiddleRight;

            _slider = new TrackBar();
            _slider.Minimum = 10; _slider.Maximum = 245; _slider.Value = cur;
            _slider.TickFrequency = 20;
            _slider.Location  = new Point(18, 84);
            _slider.Size      = new Size(390, 40);
            _slider.BackColor = Color.FromArgb(18, 18, 18);
            _slider.ValueChanged += new EventHandler(OnSlider);

            Panel sep1 = new Panel();
            sep1.Location  = new Point(20, 130); sep1.Size = new Size(384, 1);
            sep1.BackColor = Color.FromArgb(50, 50, 50);

            _btnToggle = new Button();
            _btnToggle.Font      = new Font("Segoe UI", 10, FontStyle.Bold);
            _btnToggle.FlatStyle = FlatStyle.Flat;
            _btnToggle.Location  = new Point(20, 142);
            _btnToggle.Size      = new Size(190, 44);
            _btnToggle.Click    += new EventHandler(OnToggle);
            RefreshToggle();

            Button btnQuit = new Button();
            btnQuit.Text      = "Quit";
            btnQuit.Font      = new Font("Segoe UI", 10);
            btnQuit.BackColor = Color.FromArgb(30, 30, 30);
            btnQuit.ForeColor = Color.FromArgb(180, 80, 80);
            btnQuit.FlatStyle = FlatStyle.Flat;
            btnQuit.Location  = new Point(220, 142);
            btnQuit.Size      = new Size(190, 44);
            btnQuit.FlatAppearance.BorderColor = Color.FromArgb(100, 40, 40);
            btnQuit.Click += new EventHandler(OnQuit);

            Panel sep2 = new Panel();
            sep2.Location  = new Point(20, 196); sep2.Size = new Size(384, 1);
            sep2.BackColor = Color.FromArgb(50, 50, 50);

            _chkMouseWatch = new CheckBox();
            _chkMouseWatch.Text      = "Hide overlay when mouse is on taskbar";
            _chkMouseWatch.Font      = new Font("Segoe UI", 10);
            _chkMouseWatch.ForeColor = Color.FromArgb(180, 180, 180);
            _chkMouseWatch.Location  = new Point(22, 208);
            _chkMouseWatch.AutoSize  = true;
            _chkMouseWatch.Checked   = mouseWatch;
            _chkMouseWatch.CheckedChanged += new EventHandler(OnMouseWatchChanged);

            Panel sep3 = new Panel();
            sep3.Location  = new Point(20, 242); sep3.Size = new Size(384, 1);
            sep3.BackColor = Color.FromArgb(50, 50, 50);

            _chkAutoStart = new CheckBox();
            _chkAutoStart.Text      = "Launch at Windows startup";
            _chkAutoStart.Font      = new Font("Segoe UI", 10);
            _chkAutoStart.ForeColor = Color.FromArgb(180, 180, 180);
            _chkAutoStart.Location  = new Point(22, 254);
            _chkAutoStart.AutoSize  = true;
            _chkAutoStart.Checked   = IsAutoStartEnabled();
            _chkAutoStart.CheckedChanged += new EventHandler(OnAutoStartChanged);

            this.Controls.AddRange(new Control[]
                { lblTitle, lblSlider, _lblPct, _slider,
                  sep1, _btnToggle, btnQuit,
                  sep2, _chkMouseWatch,
                  sep3, _chkAutoStart });
        }

        void OnSlider(object s, EventArgs e) { byte a = (byte)_slider.Value; _lblPct.Text = ((int)(a / 2.55)).ToString() + "%"; if (AlphaChanged != null) AlphaChanged(a); }
        void OnToggle(object s, EventArgs e) { _active = !_active; RefreshToggle(); if (ToggleChanged != null) ToggleChanged(_active); }
        void OnQuit(object s, EventArgs e) { if (QuitRequested != null) QuitRequested(); }
        void OnMouseWatchChanged(object s, EventArgs e) { if (MouseWatchChanged != null) MouseWatchChanged(_chkMouseWatch.Checked); }
        void OnAutoStartChanged(object s, EventArgs e) { if (_chkAutoStart.Checked) EnableAutoStart(); else DisableAutoStart(); }

        bool IsAutoStartEnabled()
        {
            try { RegistryKey k = Registry.CurrentUser.OpenSubKey(RUN_KEY); return k != null && k.GetValue(APP_NAME) != null; }
            catch { return false; }
        }
        void EnableAutoStart()
        {
            try { RegistryKey k = Registry.CurrentUser.OpenSubKey(RUN_KEY, true); k.SetValue(APP_NAME, "\"" + Application.ExecutablePath + "\""); }
            catch { MessageBox.Show("Could not enable startup.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); _chkAutoStart.Checked = false; }
        }
        void DisableAutoStart()
        {
            try { RegistryKey k = Registry.CurrentUser.OpenSubKey(RUN_KEY, true); if (k != null) k.DeleteValue(APP_NAME, false); }
            catch { }
        }

        void RefreshToggle()
        {
            if (_active) { _btnToggle.Text = "Disable Dimming"; _btnToggle.BackColor = Color.FromArgb(50, 20, 10); _btnToggle.ForeColor = Color.FromArgb(255, 110, 30); _btnToggle.FlatAppearance.BorderColor = Color.FromArgb(255, 110, 30); }
            else         { _btnToggle.Text = "Enable Dimming";  _btnToggle.BackColor = Color.FromArgb(15, 45, 15); _btnToggle.ForeColor = Color.FromArgb(80, 220, 100);  _btnToggle.FlatAppearance.BorderColor = Color.FromArgb(80, 220, 100); }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; this.Hide(); }
            else base.OnFormClosing(e);
        }
    }

    class TrayApp : ApplicationContext
    {
        private NotifyIcon          _tray;
        private DimOverlay          _overlay;
        private SettingsForm        _settings;
        private TaskbarMouseWatcher _watcher;
        private bool  _active     = true;
        private bool  _mouseWatch = false;
        private bool  _mouseOnBar = false;
        private byte  _alpha      = 180;

        const string SAVE_KEY = "SOFTWARE\\OledDimmer";

        void Save()
        {
            try
            {
                RegistryKey k = Registry.CurrentUser.CreateSubKey(SAVE_KEY);
                k.SetValue("Alpha",      (int)_alpha);
                k.SetValue("Active",     _active     ? 1 : 0);
                k.SetValue("MouseWatch", _mouseWatch ? 1 : 0);
            }
            catch { }
        }

        void Load()
        {
            try
            {
                RegistryKey k = Registry.CurrentUser.OpenSubKey(SAVE_KEY);
                if (k == null) return;
                object a  = k.GetValue("Alpha");
                object on = k.GetValue("Active");
                object mw = k.GetValue("MouseWatch");
                if (a  != null) _alpha      = (byte)(int)a;
                if (on != null) _active     = (int)on == 1;
                if (mw != null) _mouseWatch = (int)mw == 1;
            }
            catch { }
        }

        public TrayApp()
        {
            Load();

            _watcher = new TaskbarMouseWatcher();
            _watcher.MouseEntered += new Action(OnMouseEnter);
            _watcher.MouseLeft    += new Action(OnMouseLeave);
            if (_mouseWatch) _watcher.Start();

            BuildTray();
            _overlay = new DimOverlay();
            _overlay.DimAlpha   = _alpha;
            _overlay.UserActive = _active;
            _overlay.Show();
        }

        void OnMouseEnter() { if (!_active) return; _mouseOnBar = true;  _overlay.UserActive = false; }
        void OnMouseLeave() { if (!_active) return; _mouseOnBar = false; _overlay.UserActive = true;  }

        void BuildTray()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 80, 0)), 2, 2, 12, 12);
                g.DrawString("D", new Font("Arial", 7, FontStyle.Bold), Brushes.Black, new PointF(3, 2));
            }

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.BackColor = Color.FromArgb(25, 25, 25);
            menu.ForeColor = Color.White;
            menu.Renderer  = new DarkRenderer();

            ToolStripMenuItem mToggle = new ToolStripMenuItem(_active ? "Disable Dimming" : "Enable Dimming");
            mToggle.ForeColor = Color.FromArgb(255, 120, 40);
            mToggle.Click += new EventHandler(OnTrayToggle);

            ToolStripMenuItem mSettings = new ToolStripMenuItem("Settings...");
            mSettings.Click += new EventHandler(OnTraySettings);

            ToolStripMenuItem mQuit = new ToolStripMenuItem("Quit");
            mQuit.ForeColor = Color.FromArgb(200, 80, 80);
            mQuit.Click += new EventHandler(OnTrayQuit);

            menu.Items.Add(mToggle);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(mSettings);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(mQuit);

            _tray = new NotifyIcon();
            _tray.Icon             = Icon.FromHandle(bmp.GetHicon());
            _tray.Text             = _active ? "OLED Dimmer - Active" : "OLED Dimmer - Inactive";
            _tray.Visible          = true;
            _tray.ContextMenuStrip = menu;
            _tray.DoubleClick     += new EventHandler(OnTraySettings);
        }

        void OnTrayToggle(object s, EventArgs e)
        {
            _active = !_active;
            _overlay.UserActive = _active && !_mouseOnBar;
            ToolStripMenuItem mi = _tray.ContextMenuStrip.Items[0] as ToolStripMenuItem;
            if (_active) { if (mi != null) mi.Text = "Disable Dimming"; _tray.Text = "OLED Dimmer - Active"; }
            else         { if (mi != null) mi.Text = "Enable Dimming";  _tray.Text = "OLED Dimmer - Inactive"; }
            Save();
        }

        void OnTraySettings(object s, EventArgs e) { OpenSettings(); }
        void OnTrayQuit(object s, EventArgs e)      { Quit(); }

        void OpenSettings()
        {
            if (_settings == null || _settings.IsDisposed)
            {
                _settings = new SettingsForm(_alpha, _active, _mouseWatch);
                _settings.AlphaChanged      += new Action<byte>(OnAlpha);
                _settings.ToggleChanged     += new Action<bool>(OnToggle);
                _settings.MouseWatchChanged += new Action<bool>(OnMouseWatchChanged);
                _settings.QuitRequested     += new Action(Quit);
            }
            _settings.Show();
            _settings.BringToFront();
        }

        void OnAlpha(byte a) { _alpha = a; _overlay.DimAlpha = a; Save(); }

        void OnToggle(bool on)
        {
            _active = on;
            _overlay.UserActive = on && !_mouseOnBar;
            ToolStripMenuItem mi = _tray.ContextMenuStrip.Items[0] as ToolStripMenuItem;
            if (_active) { if (mi != null) mi.Text = "Disable Dimming"; _tray.Text = "OLED Dimmer - Active"; }
            else         { if (mi != null) mi.Text = "Enable Dimming";  _tray.Text = "OLED Dimmer - Inactive"; }
            Save();
        }

        void OnMouseWatchChanged(bool on)
        {
            _mouseWatch = on;
            if (on)  _watcher.Start();
            else   { _watcher.Stop(); _mouseOnBar = false; if (_active) _overlay.UserActive = true; }
            Save();
        }

        void Quit()
        {
            _watcher.Stop();
            _watcher.Dispose();
            _tray.Visible = false;
            if (_overlay  != null) _overlay.Close();
            if (_settings != null) _settings.Dispose();
            Application.Exit();
        }
    }

    class DarkRenderer : ToolStripProfessionalRenderer
    {
        public DarkRenderer() : base(new DarkColors()) { }
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            Color c = e.Item.Selected ? Color.FromArgb(50, 50, 50) : Color.FromArgb(25, 25, 25);
            e.Graphics.FillRectangle(new SolidBrush(c), e.Item.ContentRectangle);
        }
    }

    class DarkColors : ProfessionalColorTable
    {
        public override Color MenuBorder                  { get { return Color.FromArgb(55, 55, 55); } }
        public override Color ToolStripDropDownBackground { get { return Color.FromArgb(25, 25, 25); } }
        public override Color ImageMarginGradientBegin    { get { return Color.FromArgb(25, 25, 25); } }
        public override Color ImageMarginGradientMiddle   { get { return Color.FromArgb(25, 25, 25); } }
        public override Color ImageMarginGradientEnd      { get { return Color.FromArgb(25, 25, 25); } }
        public override Color SeparatorDark               { get { return Color.FromArgb(55, 55, 55); } }
        public override Color SeparatorLight              { get { return Color.FromArgb(40, 40, 40); } }
    }
}
