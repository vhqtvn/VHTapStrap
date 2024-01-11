using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibVHTapStrap
{
    public struct MapModeSwitchEvent(
        string map,
        uint onceLevel
    )
    {
        public string Map { get; } = map;
        public uint OnceLevel { get; } = onceLevel;
    }

    public struct MapActionsEvent(
        ITapMapHotkeySingleActionStruct[] actions
    )
    {
        public ITapMapHotkeySingleActionStruct[] Actions { get; } = actions;
    }

    public struct MapResetEvent { }

    internal class MapStateMachine
    {
        internal TapMap Map { get; set; } = TapMap.Default();
        public TapMapStruct CurrentMap { get; internal set; } = new TapMapStruct("dummy", null, true, true);

        public event EventHandler<MapModeSwitchEvent>? OnModeSwitch;
        public event EventHandler<MapActionsEvent>? OnMapActionsRequested;
        public event EventHandler<MapResetEvent>? OnReset;

        private Stack<uint> onceStack = new Stack<uint>();
        private uint OnceLevel = 0;

        private uint __currentMapIndex = 0;
        private uint CurrentMapIndex
        {
            get => __currentMapIndex;
            set
            {
                if (__currentMapIndex != value)
                {
                    OnMapExit();
                    __currentMapIndex = value;
                    CurrentMap = Map.map[__currentMapIndex];
                    OnMapEnter();
                }
                OnModeSwitch?.Invoke(this, new MapModeSwitchEvent(CurrentMap.Name, OnceLevel));
            }
        }


        private void OnMapExit()
        {
            if (CurrentMap.ExitActions != null)
            {
                OnMapActionsRequested?.Invoke(this, new MapActionsEvent(CurrentMap.ExitActions));
            }
        }

        private void OnMapEnter()
        {
            if (CurrentMap.EnterActions != null)
            {
                OnMapActionsRequested?.Invoke(this, new MapActionsEvent(CurrentMap.EnterActions));
            }
        }

        private Stack<uint> mapStack = new Stack<uint>();

        public void Reset()
        {
            mapStack.Clear();
            onceStack.Clear();
            OnceLevel = 0;
            CurrentMapIndex = 0;
            CurrentMap = Map.map[0];
        }

        public void MaybePopOnce()
        {
            if (OnceLevel > 0)
            {
                uint newIndex = CurrentMapIndex;
                while (OnceLevel > 0)
                {
                    OnceLevel = onceStack.Pop();
                    newIndex = mapStack.Pop();
                }
                CurrentMapIndex = newIndex;
            }
        }

        public void Apply(TapMapHotkeyModeSwitchStruct spec)
        {
            switch (spec.Type)
            {
                case TapMapHotkeyModeSwitchType.Reset:
                    OnReset?.Invoke(this, new MapResetEvent());
                    break;
                case TapMapHotkeyModeSwitchType.Pop:
                    while (mapStack.Count > 0)
                    {
                        if (onceStack.Count > 0)
                            OnceLevel = onceStack.Pop();
                        CurrentMapIndex = mapStack.Pop();
                        if (OnceLevel > 0 || CurrentMap.KeepInStack)
                        {
                            break;
                        }
                    }
                    break;
                case TapMapHotkeyModeSwitchType.Once:
                    {
                        var nextMapIndex = spec.Target;
                        onceStack.Push(OnceLevel++);
                        mapStack.Push(CurrentMapIndex);
                        CurrentMapIndex = nextMapIndex;
                        break;
                    }
                case TapMapHotkeyModeSwitchType.Push:
                    {
                        var nextMapIndex = spec.Target;
                        onceStack.Push(OnceLevel);
                        OnceLevel = 0;
                        mapStack.Push(CurrentMapIndex);
                        CurrentMapIndex = nextMapIndex;
                        break;
                    }
            }
        }

        internal void OnRun()
        {
            OnModeSwitch?.Invoke(this, new MapModeSwitchEvent(CurrentMap.Name, OnceLevel));
        }
    }
}
