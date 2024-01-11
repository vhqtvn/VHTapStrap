using System;
using System.Runtime.InteropServices;
using static LibVHTapStrap.User32Interop;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace LibVHTapStrap
{
    interface ITSKeyboardEventsQueue
    {
        void AddEvent(long delayMS, TSHotkeyBase definition);
    }

    interface ITSKeyboardState
    {
        TSModifiers HoldingModifiers { get; set; }
        TSModifiers Modifiers { get; set; }
        TSModifiers ComposingModifiers { get; set; }

        void ComposingStart();
        void ComposingFinished();

        void AddInput(INPUT input);
    }

    [Flags]
    public enum TSModifiers : uint
    {
        LShift = 1,
        RShift = 2,
        LControl = 4,
        RControl = 8,
        LAlt = 16,
        RAlt = 32,
        LSuper = 64,
        RSuper = 128,
    };

    static class TSModifiersExtension
    {

        static Dictionary<TSModifiers, VK> TSModifiersToVK = new Dictionary<TSModifiers, VK>
        {
            { TSModifiers.LShift, VK.LSHIFT },
            { TSModifiers.RShift, VK.RSHIFT },
            { TSModifiers.LControl, VK.LCONTROL },
            { TSModifiers.RControl, VK.RCONTROL },
            { TSModifiers.LAlt, VK.MENU },
            { TSModifiers.RAlt, VK.RMENU },
            { TSModifiers.LSuper, VK.LWIN },
            { TSModifiers.RSuper, VK.RWIN },
        };

        public static List<VK> ToVKs(this TSModifiers modifiers)
        {
            var result = new List<VK>();
            foreach (var (tsModifier, vk) in TSModifiersToVK)
            {
                if (modifiers.HasFlag(tsModifier))
                {
                    result.Add(vk);
                }
            }
            return result;
        }
    }

    abstract class TSHotkeyBase : ITSHotKeyDefinition
    {
        public virtual void Register(ITSKeyboardEventsQueue queue)
        {
            queue.AddEvent(0, this);
        }


        public static INPUT CreateKeyInput(VK keyCode, bool up, uint time = 0) => new INPUT
        {
            Type = (uint)InputType.Keyboard,
            Data =
                {
                    Keyboard = new KEYBDINPUT
                    {
                        KeyCode = keyCode.Into(),
                        Scan = (ushort)(MapVirtualKey(keyCode.Into(), 0) & 0xFFu),
                        Flags = keyCode.MaybeKeyboardExtendedFlag().WithKeyUp(up).Into(),
                        Time = time,
                        ExtraInfo = IntPtr.Zero,
                    }
                }
        };

        public static INPUT CreateCharInput(char ch, bool up, uint time = 0) => new INPUT
        {
            Type = (uint)InputType.Keyboard,
            Data =
            {
                    Keyboard = new KEYBDINPUT
                    {
                        KeyCode = 0,
                        Scan = (ushort)ch,
                        Flags = KeyboardFlag.Unicode.WithKeyUp(up).Into(),
                        Time = time,
                        ExtraInfo = IntPtr.Zero,
                    }
                }
        };

        public abstract void Invoke(ITSKeyboardState state);
    }

    class TSRawKeyEventDefinition(
        VK keyCode,
        bool up = true,
        bool down = true,
        TSModifiers? modifiers = null,
        TSModifiers? addModifiers = null
    ) : TSHotkeyBase
    {
        VK KeyCode { get; } = keyCode;

        bool Up { get; } = up;
        bool Down { get; } = down;

        // add modifiers if not null, for this key only
        TSModifiers? Modifiers { get; } = modifiers;
        TSModifiers? AddModifiers { get; } = addModifiers;

        public override void Invoke(ITSKeyboardState state)
        {
            var modifiers = Modifiers ?? state.ComposingModifiers;
            if (AddModifiers.HasValue)
            {
                modifiers |= AddModifiers.Value;
            }
            var neededModifiers = modifiers & ~state.HoldingModifiers;
            var modifierVKs = neededModifiers.ToVKs();
            if (Down)
            {
                modifierVKs.ForEach(vk => state.AddInput(CreateKeyInput(vk, up: false)));
                state.AddInput(CreateKeyInput(KeyCode, up: false));
            }
            if (Up)
            {
                state.AddInput(CreateKeyInput(KeyCode, up: true));
                modifierVKs.ForEach(vk => state.AddInput(CreateKeyInput(vk, up: true)));
            }
        }

        public override string ToString()
        {
            return $"TSRawKeyEventDefinition({KeyCode}, up: {Up}, down: {Down}, modifiers: {Modifiers}, addModifiers: {AddModifiers})";
        }
    }

    class TSCharKeyEventDefinition(
        char ch,
        bool up = true,
        bool down = true,
        TSModifiers? modifiers = null,
        TSModifiers? addModifiers = null
    ) : TSHotkeyBase
    {
        char Char { get; } = ch;
        bool Up { get; } = up;
        bool Down { get; } = down;


        // add modifiers if not null, for this key only
        TSModifiers? Modifiers { get; } = modifiers;
        TSModifiers? AddModifiers { get; } = addModifiers;


        public override void Invoke(ITSKeyboardState state)
        {
            var modifiers = Modifiers ?? state.ComposingModifiers;
            if (AddModifiers.HasValue)
            {
                modifiers |= AddModifiers.Value;
            }
            var neededModifiers = modifiers & ~state.HoldingModifiers;
            var modifierVKs = neededModifiers.ToVKs();
            bool handled = false;
            if (modifiers != 0)
            {
                var vk = Char.ToVK();
                if (vk != null)
                {
                    handled = true;
                    var vkValue = vk.Value;
                    if (Down)
                    {
                        modifierVKs.ForEach(vk => state.AddInput(CreateKeyInput(vk, up: false)));
                        state.AddInput(CreateKeyInput(vkValue, up: false));
                    }
                    if (Up)
                    {
                        state.AddInput(CreateKeyInput(vkValue, up: true));
                        modifierVKs.ForEach(vk => state.AddInput(CreateKeyInput(vk, up: true)));
                    }
                }
            }


            if (!handled)
            {
                if (Down)
                {
                    modifierVKs.ForEach(vk => state.AddInput(CreateKeyInput(vk, up: false)));
                    state.AddInput(CreateCharInput(Char, up: false));
                }
                if (Up)
                {
                    state.AddInput(CreateCharInput(Char, up: true));
                    modifierVKs.ForEach(vk => state.AddInput(CreateKeyInput(vk, up: true)));
                }
            }

            state.ComposingFinished();
        }

        public override string ToString()
        {
            return $"TSCharKeyEventDefinition({Char}, up: {Up}, down: {Down}, modifiers: {Modifiers}, addModifiers: {AddModifiers})";
        }
    }

    class TSKeyAddModifierDefinition(
        TSModifiers addModifiers,
        bool resetOnNextCompose
    ) : TSHotkeyBase
    {
        TSModifiers AddModifiers { get; } = addModifiers;
        bool ResetOnNextCompose { get; } = resetOnNextCompose;

        public override void Invoke(ITSKeyboardState state)
        {
            state.ComposingModifiers |= AddModifiers;
            if (!ResetOnNextCompose)
            {
                state.Modifiers |= AddModifiers;
            }
        }

        public override string ToString()
        {
            return $"TSKeyAddModifierDefinition({AddModifiers}, resetOnNextCompose: {ResetOnNextCompose})";
        }
    }

    class TSKeyToggleModifierDefinition(
        TSModifiers toggleModifiers,
        bool sendKeys = false
    ) : TSHotkeyBase
    {
        TSModifiers ToggleModifiers { get; } = toggleModifiers;
        bool SendKeys { get; } = sendKeys;

        public override void Invoke(ITSKeyboardState state)
        {
            state.ComposingModifiers ^= ToggleModifiers;
            state.Modifiers ^= ToggleModifiers;

            if (SendKeys)
            {
                var NewClearModifier = state.HoldingModifiers & ~state.Modifiers;
                var NewSetModifier = ~state.HoldingModifiers & state.Modifiers;

                var clearVKs = NewClearModifier.ToVKs();
                var setVKs = NewSetModifier.ToVKs();

                clearVKs.ForEach(vk => state.AddInput(CreateKeyInput(vk, up: true)));
                setVKs.ForEach(vk => state.AddInput(CreateKeyInput(vk, up: false)));

                state.HoldingModifiers = state.Modifiers;
            }
        }

        public override string ToString()
        {
            return $"TSKeyToggleModifierDefinition({ToggleModifiers}, sendKeys: {SendKeys})";
        }
    }

    class TSHotkeySequence(
        List<(int delay, TSHotkeyBase hotkey)> sequence
    ) : TSHotkeyBase
    {
        List<(int delay, TSHotkeyBase hotkey)> Sequence { get; } = sequence;

        public override void Register(ITSKeyboardEventsQueue queue)
        {
            foreach (var (delay, hotkey) in Sequence)
            {
                queue.AddEvent(delay, hotkey);
            }
        }

        public override void Invoke(ITSKeyboardState state)
        {
            // nothing to do for this one
        }

        public override string ToString()
        {
            return $"TSHotkeySequence({Sequence})";
        }
    }

    public class ComposingStateChanged(
        TSModifiers modifiers,
        bool composing
    )
    {
        public TSModifiers Modifiers { get; } = modifiers;
        public bool Composing { get; } = composing;
    }

    class TSKeyboard : ITSKeyboard<TSHotkeyBase>, ITSKeyboardEventsQueue, ITSKeyboardState
    {
        public event EventHandler<ComposingStateChanged>? ComposeStateChanged;

        // ITSKeyboardState
        private TSModifiers? composingModifiers;
        public TSModifiers HoldingModifiers { get; set; } = 0;
        public TSModifiers Modifiers { get; set; } = 0;
        public TSModifiers ComposingModifiers
        {
            get => composingModifiers.GetValueOrDefault(Modifiers);
            set => composingModifiers = value;
        }

        public void ComposingStart()
        {
            composingModifiers = null;
        }

        public void ComposingFinished()
        {
            composingModifiers = null;
        }

        // ITSKeyboardEventsQueue
        private ReaderWriterLockSlim eventsQueueLock = new ReaderWriterLockSlim();
        private SortedDictionary<long, List<TSHotkeyBase>> eventsQueue = new SortedDictionary<long, List<TSHotkeyBase>>();

        public void AddEvent(long delayMS, TSHotkeyBase definition)
        {
            eventsQueueLock.EnterWriteLock();
            try
            {
                long targetTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() + delayMS;
                if (!eventsQueue.TryGetValue(targetTime, out var list))
                {
                    list = new List<TSHotkeyBase>();
                    eventsQueue.Add(targetTime, list);
                }
                list.Add(definition);
            }
            finally
            {
                eventsQueueLock.ExitWriteLock();
            }
        }

        List<INPUT> inputs = new List<INPUT>();
        public void AddInput(INPUT input)
        {
            inputs.Add(input);
        }

        public long? ProcessEvents()
        {
            ProcesstoInput();

            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            long? nextTime = null;

            for (; ; )
            {
                KeyValuePair<long, List<TSHotkeyBase>> toProcess;
                eventsQueueLock.EnterReadLock();
                try
                {
                    using var enumerator = eventsQueue.GetEnumerator();
                    if (!enumerator.MoveNext())
                    {
                        break;
                    }
                    var first = enumerator.Current;
                    if (first.Key > now)
                    {
                        nextTime = first.Key;
                        break;
                    }
                    toProcess = first;
                }
                finally
                {
                    eventsQueueLock.ExitReadLock();
                }
                eventsQueueLock.EnterWriteLock();
                try
                {
                    eventsQueue.Remove(toProcess.Key);
                }
                finally
                {
                    eventsQueueLock.ExitWriteLock();
                }
                foreach (var definition in toProcess.Value)
                {
                    definition.Invoke(this);
                }

                ProcesstoInput();
            }

            MaybeNotifyComposeStateChanged();

            return nextTime.HasValue ? nextTime - now : null;
        }

        private TSModifiers? lastNotifiedModifiers = null;
        private void MaybeNotifyComposeStateChanged()
        {
            if (composingModifiers == lastNotifiedModifiers)
            {
                return;
            }
            lastNotifiedModifiers = composingModifiers;
            ComposeStateChanged?.Invoke(this, new ComposingStateChanged(ComposingModifiers, composingModifiers.HasValue));
        }

        private void ProcesstoInput()
        {
            if (inputs.Count > 0)
            {
                var inputsArray = inputs.ToArray();
                inputs.Clear();

                var sent = SendInput((uint)inputsArray.Length, inputsArray, Marshal.SizeOf(typeof(INPUT)));
                if (sent != inputs.Count)
                {
                    //throw new Exception("Failed to send all inputs");
                }
            }
        }

        public void SendKey(TSHotkeyBase definition)
        {
            definition.Register(this);
        }
    }

}
