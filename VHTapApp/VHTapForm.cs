using LibVHTapStrap;
using System.Runtime.InteropServices;

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
            mode += tapLibInputState.IsComposing ? " [C]" : "";
            mode += " " + tapLibInputState.Modifiers.ToString();
            lblMode.Text = mode.TrimEnd();
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
        }

        private void VHTapForm_Load(object sender, EventArgs e)
        {
            VHTabLib.Start();
        }

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    }
}
