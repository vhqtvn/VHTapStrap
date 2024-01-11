using LibVHTapStrap;
using System.Windows;
using Windows.ApplicationModel.SocialInfo;

namespace TestGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LibVHTapStrap.LibVHTapStrap lib = LibVHTapStrap.LibVHTapStrap.Instance;
        string mapName = "default";
        uint onceLevel = 0;
        bool isComposing = false;
        TSModifiers modifiers = 0;

        public MainWindow()
        {
            InitializeComponent();

            StartLib();
        }

        private void StartLib()
        {
            var map = TapMap.findDefaultMap();
            Console.WriteLine(map.DebugDump());
            lib.Map = map;

            lib.OnMapModeSwitch += VHTapLib_OnMapModeSwitch;
            lib.OnComposeStateChanged += VHTapLib_OnComposeStateChanged;

            lib.Start();
        }

        private void VHTapLib_OnComposeStateChanged(object? sender, ComposingStateChanged e)
        {
            isComposing = e.Composing;
            modifiers = e.Modifiers;
            Dispatcher.InvokeAsync(() =>
            {
                UpdateMode();
            });
        }

        private void VHTapLib_OnMapModeSwitch(object? sender, MapModeSwitchEvent e)
        {
            mapName = e.Map;
            onceLevel = e.OnceLevel;
            Dispatcher.InvokeAsync(() =>
            {
                UpdateMode();
            });
        }

        private void UpdateMode()
        {
            string mode = mapName;
            if (onceLevel > 0)
            {
                mode += $" (once {onceLevel})";
            }
            if (isComposing)
            {
                mode += " (composing)";
            }
            if (modifiers != 0)
            {
                mode += $" ({modifiers})";
            }
            lblMode.Content = mode;
        }
    }
}