using LibVHTapStrap;
using System.Runtime.InteropServices;
using Accessibility;

namespace VHTapApp
{
    struct TapLibInputState
    {
        public bool IsComposing { get; set; }
        public TSModifiers Modifiers { get; set; }

        public string Mode { get; set; }

        public uint OnceLevel { get; set; }
    }
    public partial class VHTapForm : Form
    {
        private LibVHTapStrap.LibVHTapStrap VHTabLib = LibVHTapStrap.LibVHTapStrap.Instance;

        private TapLibInputState tapLibInputState = new TapLibInputState();

        private System.Windows.Forms.Timer timerFollowMouse = new System.Windows.Forms.Timer();

        private void StartLib()
        {
            var map = TapMap.findDefaultMap();
            if (map == null)
            {
                MessageBox.Show("No map found");
                Environment.Exit(1);
            }
            Console.WriteLine(map.DebugDump());
            VHTabLib.Map = map;

            VHTabLib.OnMapModeSwitch += VHTapLib_OnMapModeSwitch;
            VHTabLib.OnComposeStateChanged += VHTapLib_OnComposeStateChanged;
        }

        private void VHTapLib_OnComposeStateChanged(object? sender, ComposingStateChanged e)
        {
            tapLibInputState.IsComposing = e.Composing;
            tapLibInputState.Modifiers = e.Modifiers;
            BeginInvoke(UpdateLabel);
        }

        private void VHTapLib_OnMapModeSwitch(object? sender, MapModeSwitchEvent e)
        {
            tapLibInputState.Mode = e.Map;
            tapLibInputState.OnceLevel = e.OnceLevel;
            BeginInvoke(UpdateLabel);
        }

        private void UpdateLabel()
        {
            string mode = tapLibInputState.Mode;
            if (tapLibInputState.OnceLevel > 0)
            {
                mode += $" ({tapLibInputState.OnceLevel})";
            }
            mode += tapLibInputState.IsComposing ? "🅲" : "";
            if (tapLibInputState.Modifiers != 0)
            {
                mode += " " + tapLibInputState.Modifiers.ToString();
            }
            mode = mode.Trim();
            lblMode.Text = mode;
            if (mode.Length > 0)
            {
                lblMode.Visible = true;
                StartFollowMouse();
                Cursor.Hide();
            }
            else
            {
                lblMode.Visible = false;
                StopFollowMouse();
                Cursor.Show();
            }
        }

        public VHTapForm()
        {
            InitializeComponent();

            ConfigureStyle();

            StartLib();
        }

        private void ConfigureStyle()
        {
            FormBorderStyle = FormBorderStyle.None;
            SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, 0);
            Opacity = 0.8;
        }

        private void VHTapForm_Load(object sender, EventArgs e)
        {
            VHTabLib.Start();

            timerFollowMouse.Interval = 10;
            timerFollowMouse.Tick += TimerFollowMouse_Tick;
        }

        private void TimerFollowMouse_Tick(object? sender, EventArgs e)
        {
            SetPositionVisibleNearMouse();
            Cursor.Hide();
        }

        private void SetPositionVisibleNearMouse()
        {
            var pos = PositionToShow();
            var screen = Screen.FromPoint(pos);
            var screenBounds = screen.Bounds;
            var formBounds = Bounds;
            var formSize = formBounds.Size;
            var formPos = new Point(pos.X - formSize.Width / 2, pos.Y - formSize.Height / 2);
            if (formPos.X < screenBounds.Left)
            {
                formPos.X = screenBounds.Left;
            }
            if (formPos.Y < screenBounds.Top)
            {
                formPos.Y = screenBounds.Top;
            }
            if (formPos.X + formSize.Width > screenBounds.Right)
            {
                formPos.X = screenBounds.Right - formSize.Width;
            }
            if (formPos.Y + formSize.Height > screenBounds.Bottom)
            {
                formPos.Y = screenBounds.Bottom - formSize.Height;
            }
            Location = formPos;
        }

        private Point PositionToShow()
        {
            // try to find caret position
            var caretPos = GetCaretPos();
            if (caretPos != null)
            {
                return caretPos.Value;
            }
            return Cursor.Position;
        }

        private Point? GetCaretPos()
        {
            IntPtr hwndForeground = GetForegroundWindow();
            IntPtr processId;
            IntPtr threadId = GetWindowThreadProcessId(hwndForeground, out processId);

            GUITHREADINFO guiInfo = new GUITHREADINFO();
            guiInfo.cbSize = Marshal.SizeOf(guiInfo);

            if (GetGUIThreadInfo(threadId, ref guiInfo))
            {
                Guid guidIAccessible = new Guid("618736E0-3C3D-11CF-810C-00AA00389B71");
                IAccessible accessible = null;
                if (AccessibleObjectFromWindow(guiInfo.hwndCaret, OBJID_CARET, ref guidIAccessible, ref accessible) == 0 && accessible != null)
                {
                    int x, y, width, height;
                    accessible.accLocation(out x, out y, out width, out height, 0);
                    return new Point(x, y);
                }
            }
            return null;
        }

        private void StartFollowMouse()
        {
            timerFollowMouse.Enabled = true;
        }

        private void StopFollowMouse()
        {
            timerFollowMouse.Enabled = false;
        }

        const uint OBJID_CARET = 0xFFFFFFF8;
        [DllImport("oleacc.dll")]
        static extern int AccessibleObjectFromWindow(IntPtr hwnd, uint dwObjectID, ref Guid riid, ref IAccessible ppvObject);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetGUIThreadInfo(IntPtr hTreadID, ref GUITHREADINFO lpgui);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hwnd, out IntPtr lpdwProcessId);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int iLeft;
            public int iTop;
            public int iRight;
            public int iBottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct GUITHREADINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public RECT rectCaret;
        }

    }
}
