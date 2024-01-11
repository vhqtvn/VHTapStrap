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

    internal class MapStateMachine
    {
        internal TapMap Map { get; set; } = TapMap.Default();
        public TapMapStruct CurrentMap { get; internal set; } = new TapMapStruct("dummy", null, true, true);

        public event EventHandler<MapModeSwitchEvent>? OnModeSwitch;

        private uint OnceLevel = 0;

        private uint __currentMapIndex = 0;
        private uint CurrentMapIndex
        {
            get => __currentMapIndex;
            set
            {
                if (__currentMapIndex != value)
                {
                    __currentMapIndex = value;
                    CurrentMap = Map.map[__currentMapIndex];
                }
                OnModeSwitch?.Invoke(this, new MapModeSwitchEvent(CurrentMap.Name, OnceLevel));
            }
        }
        private Stack<uint> mapStack = new Stack<uint>();

        public void Reset()
        {
            mapStack.Clear();
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
                    --OnceLevel;
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
                    Reset();
                    break;
                case TapMapHotkeyModeSwitchType.Pop:
                    if (mapStack.Count > 0)
                    {
                        if (OnceLevel > 0)
                            --OnceLevel;
                        CurrentMapIndex = mapStack.Pop();
                    }
                    break;
                case TapMapHotkeyModeSwitchType.Once:
                    {
                        var nextMapIndex = spec.Target;
                        if (nextMapIndex != CurrentMapIndex)
                        {
                            ++OnceLevel;
                            mapStack.Push(CurrentMapIndex);

                            CurrentMapIndex = nextMapIndex;
                        }
                        break;
                    }
                case TapMapHotkeyModeSwitchType.Push:
                    {
                        uint newIndex = CurrentMapIndex;
                        if (OnceLevel > 0)
                        {
                            // follow a once sequence and change mode without commiting, so we need to revert
                            while (OnceLevel > 0)
                            {
                                --OnceLevel;
                                newIndex = mapStack.Pop();
                            }
                        }
                        var nextMapIndex = spec.Target;
                        if (nextMapIndex != newIndex)
                        {
                            mapStack.Push(CurrentMapIndex);
                            newIndex = nextMapIndex;
                        }
                        CurrentMapIndex = newIndex;
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
