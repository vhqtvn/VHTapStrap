using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static LibVHTapStrap.User32Interop;

namespace LibVHTapStrap
{
    internal class TapInput : ITapMapHotkeyRunner<bool>, IMutitapPendingDecider
    {
        private MultitapComposer multiTapComposer = new MultitapComposer();
        private MapStateMachine mapStateMachine = new MapStateMachine();
        private TSKeyboard keyboard = new TSKeyboard();

        public event EventHandler<MapModeSwitchEvent> OnMapModeSwitch
        {
            add { mapStateMachine.OnModeSwitch += value; }
            remove { mapStateMachine.OnModeSwitch -= value; }
        }
        public event EventHandler<ComposingStateChanged>? OnComposeStateChanged
        {
            add { keyboard.ComposeStateChanged += value; }
            remove { keyboard.ComposeStateChanged -= value; }
        }

        internal TapInput()
        {
            multiTapComposer.OnCommitInput += MultiTapComposer_OnCommitInput;
            multiTapComposer.OnPendingInput += MultiTapComposer_OnPendingInput;
        }

        private int runId = 0;
        internal async void Run()
        {
            mapStateMachine.OnRun();
            var id = ++runId;
            while (id == runId)
            {
                keyboard.ProcessEvents();
                await Task.Delay(10);
            }
        }

        internal void Stop()
        {
            ++runId;
        }

        internal void OnTap(int tapMask)
        {
            multiTapComposer.OnTap(tapMask);
        }

        private void MultiTapComposer_OnPendingInput(object? sender, MultitapPendingInputArgs e)
        {
            RunHotkey(e.Mask, e.TapCount, false);
        }

        private void MultiTapComposer_OnCommitInput(object? sender, MultitapInputArgs e)
        {
            RunHotkey(e.Mask, e.TapCount, true);
        }

        private void RunHotkey(int mask, uint tapCount, bool final)
        {
            var map = mapStateMachine.CurrentMap;
            var hotkey = map.Hotkeys[mask];

            if (hotkey == null)
            {
                return;
            }


            hotkey.Invoke(this, tapCount, final);
        }

        internal void Reset()
        {
            multiTapComposer.Reset();
            mapStateMachine.Reset();
        }

        public void MapHotkeyRun(TSHotkeyBase[] keys, bool final)
        {
            if (final)
            {
                foreach (var key in keys)
                {
                    key.Invoke(keyboard);
                }
                mapStateMachine.MaybePopOnce();
            }
        }

        public void MapSwitchMode(TapMapHotkeyModeSwitchStruct modeSwitchStruct, bool final)
        {
            if (final)
            {
                mapStateMachine.Apply(modeSwitchStruct);
            }
            else
            {

            }
        }

        public bool MultiTapShouldBePending(int mask, uint tapCnt)
        {
            var map = mapStateMachine.CurrentMap;
            var hotkey = map.Hotkeys[mask];
            if (hotkey == null)
            {
                return false;
            }
            return hotkey.ShouldBePending(tapCnt);
        }

        internal TapMap Map
        {
            get => mapStateMachine.Map;
            set
            {
                mapStateMachine.Map = value;
                Reset();
            }
        }
    }
}
