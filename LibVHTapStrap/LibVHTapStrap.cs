using TAPWin;

namespace LibVHTapStrap
{
    public class LibVHTapStrap
    {
        private static LibVHTapStrap instance = new LibVHTapStrap();
        private bool isRegistered = false;
        private Object lockObj = new Object();
        private TapInput tapInput = new TapInput();

        public event EventHandler<MapModeSwitchEvent> OnMapModeSwitch
        {
            add { tapInput.OnMapModeSwitch += value; }
            remove { tapInput.OnMapModeSwitch -= value; }
        }

        public event EventHandler<ComposingStateChanged>? OnComposeStateChanged
        {
            add { tapInput.OnComposeStateChanged += value; }
            remove { tapInput.OnComposeStateChanged -= value; }
        }

        public static LibVHTapStrap Instance
        {
            get
            {
                return instance;
            }
        }

        private LibVHTapStrap()
        {
        }

        ~LibVHTapStrap()
        {
            UnregisterTapHooks();
        }

        private void RegisterTapHooks()
        {
            lock (lockObj)
            {
                if (isRegistered)
                {
                    return;
                }
                TAPManager.Instance.OnMoused += TAP_OnMoused;
                TAPManager.Instance.OnTapped += TAP_OnTapped;
                TAPManager.Instance.OnTapConnected += TAP_OnTapConnected;
                TAPManager.Instance.OnTapDisconnected += TAP_OnTapDisconnected;
                TAPManager.Instance.SetDefaultInputMode(TAPInputMode.Controller(), true);
                TAPManager.Instance.Start();
                isRegistered = true;
            }
        }

        private void UnregisterTapHooks()
        {
            lock (lockObj)
            {
                if (!isRegistered)
                {
                    return;
                }
                TAPManager.Instance.OnMoused -= TAP_OnMoused;
                TAPManager.Instance.OnTapped -= TAP_OnTapped;
                TAPManager.Instance.OnTapConnected -= TAP_OnTapConnected;
                TAPManager.Instance.OnTapDisconnected -= TAP_OnTapDisconnected;
                isRegistered = false;
            }
        }

        private void TAP_OnTapDisconnected(string identifier)
        {
            Console.WriteLine("Disconnected: " + identifier);
            tapInput.Reset();
        }

        private void TAP_OnTapConnected(string identifier, string name, int fw)
        {
            Console.WriteLine("Connected: " + identifier + " " + name + " " + fw);
            TAPManager.Instance.SetTapInputMode(TAPInputMode.Controller());
            tapInput.Reset();
        }

        private void TAP_OnTapped(string identifier, int tapMask)
        {
            tapInput.OnTap(tapMask);
        }

        private void TAP_OnMoused(string identifier, int vx, int vy, bool isMouse)
        {

        }

        public void Start()
        {
            RegisterTapHooks();
            tapInput.Run();
        }

        public void Stop()
        {
            UnregisterTapHooks();
            tapInput.Stop();
        }

        public TapMap Map
        {
            set
            {
                tapInput.Map = value;
            }
        }
    }
}
