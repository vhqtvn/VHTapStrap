using System.Diagnostics.CodeAnalysis;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace LibVHTapStrap
{
    internal interface ITapMapHotkeyRunner<T>
    {
        void MapHotkeyRun(TSHotkeyBase[] key, T param);
        void MapSwitchMode(TapMapHotkeyModeSwitchStruct modeSwitchStruct, T param);
    }

    internal abstract class TapMapHotkeyStruct
    {
        public abstract void Invoke<T>(ITapMapHotkeyRunner<T> runner, uint index, T param);
        public abstract bool ShouldBePending(uint tapCnt);
    }

    internal class TapMapHotkeyEmptyStruct(
    ) : TapMapHotkeyStruct
    {
        public override string ToString()
        {
            return $"Empty()";
        }

        public override void Invoke<T>(ITapMapHotkeyRunner<T> runner, uint index, T param)
        {
        }

        public override bool ShouldBePending(uint tapCnt)
        {
            return false;
        }
    }

    internal class TapMapHotkeyKeyStruct(
        TSHotkeyBase[] keys
    ) : TapMapHotkeyStruct
    {
        public TSHotkeyBase[] Keys { get; } = keys;

        public override string ToString()
        {
            return $"Key({string.Join(", ", Keys.Select(x => x.ToString()).ToArray())})";
        }

        public override void Invoke<T>(ITapMapHotkeyRunner<T> runner, uint index, T param)
        {
            runner.MapHotkeyRun(Keys, param);
        }

        public override bool ShouldBePending(uint tapCnt)
        {
            return false;
        }
    }

    enum TapMapHotkeyModeSwitchType
    {
        Reset,
        Push,
        Pop,
        Once,
    }
    internal class TapMapHotkeyModeSwitchResolvingStruct(
               string target,
               TapMapHotkeyModeSwitchType type
           ) : TapMapHotkeyStruct
    {
        public string Target { get; } = target;
        public TapMapHotkeyModeSwitchType Type { get; } = type;

        public override string ToString()
        {
            return $"ModeSwitch!Resolving!({Target}, {Type})";
        }

        public override void Invoke<T>(ITapMapHotkeyRunner<T> runner, uint index, T param)
        {
            throw new Exception("Should not be invoked");
        }

        public override bool ShouldBePending(uint tapCnt)
        {
            throw new Exception("Should not be invoked");
        }
    }

    internal class TapMapHotkeyModeSwitchStruct(
           uint target,
           TapMapHotkeyModeSwitchType type
       ) : TapMapHotkeyStruct
    {
        public uint Target { get; } = target;
        public TapMapHotkeyModeSwitchType Type { get; } = type;

        public override string ToString()
        {
            return $"ModeSwitch({Target}, {Type})";
        }

        public override void Invoke<T>(ITapMapHotkeyRunner<T> runner, uint index, T param)
        {
            runner.MapSwitchMode(this, param);
        }

        public override bool ShouldBePending(uint tapCnt)
        {
            return false;
        }
    }

    internal class TapMapHotkeyMultiTapsStruct : TapMapHotkeyStruct
    {
        public TapMapHotkeyStruct[] Hotkeys { get; }

        public TapMapHotkeyMultiTapsStruct(TapMapHotkeyStruct[] hotkeys)
        {
            if (hotkeys.Length == 0)
            {
                throw new Exception("Empty multi-taps configuration");
            }
            Hotkeys = hotkeys;
        }

        public override string ToString()
        {
            return $"MultiTaps({string.Join(", ", Hotkeys.Select(k => k.ToString()))})";
        }

        public override void Invoke<T>(ITapMapHotkeyRunner<T> runner, uint index, T param)
        {
            Hotkeys[index % Hotkeys.Length].Invoke(runner, 0, param);
        }

        public override bool ShouldBePending(uint tapCnt)
        {
            return tapCnt + 1 < Hotkeys.Length;
        }
    }

    public interface ITapMapHotkeySingleActionStruct
    {
    }

    public class TapMapHotkeySingleActionVibrateStruct(int[] pattern) : ITapMapHotkeySingleActionStruct
    {
        public int[] Pattern { get; } = pattern;
    }

    public class TapMapHotkeySingleActionNotifyStruct(string message, int duration) : ITapMapHotkeySingleActionStruct
    {
        public string Message { get; } = message;
        public int Duration { get; } = duration;
    }

    internal class TapMapStruct(
        string name,
        string? extends,
        bool isDefault = false,
        bool keepInStack = true,
        ITapMapHotkeySingleActionStruct[]? enterAction = null,
        ITapMapHotkeySingleActionStruct[]? exitAction = null
    )
    {
        public TapMapHotkeyStruct[] Hotkeys { get; } = new TapMapHotkeyStruct[32];

        public string Name { get; } = name;
        public bool IsDefault { get; } = isDefault;
        public bool KeepInStack { get; } = keepInStack;
        public string? Extends { get; } = extends;

        public ITapMapHotkeySingleActionStruct[]? EnterActions { get; } = enterAction;
        public ITapMapHotkeySingleActionStruct[]? ExitActions { get; } = exitAction;
    }


    public class TapMap
    {
        public const string DefaultMapFile = "default.tapmap.yaml";

        internal TapMapStruct[] map { get; }

        private TapMap(TapMapStruct[] map)
        {
            this.map = map;
        }

        public string DebugDump()
        {
            string[] tapStatesLeft = new string[32];
            string[] tapStatesRight = new string[32];
            for (int i = 0; i < 32; i++)
            {
                int j = i;
                var s = new StringBuilder();
                for (int k = 0; k < 5; k++)
                {
                    s.Append((j % 2 == 1) ? '.' : '-');
                    j /= 2;
                }
                tapStatesRight[i] = s.ToString();
                tapStatesLeft[i] = new string(s.ToString().Reverse().ToArray());
            }
            var sb = new StringBuilder();
            for (int i = 0; i < map.Length; i++)
            {
                sb.AppendLine($"Map {i}: {map[i].Name}");
                for (int j = 0; j < map[i].Hotkeys.Length; j++)
                {
                    if (map[i].Hotkeys[j] == null) continue;
                    sb.AppendLine($"  Hotkey {j.ToString("D2")} (Left: {tapStatesLeft[j]} Right: {tapStatesRight[j]}): {map[i].Hotkeys[j]}");
                }
            }
            return sb.ToString();
        }

        public static TapMap? findDefaultMap()
        {
            var env = Environment.GetEnvironmentVariable("TAPMAP");
            if (env != null)
            {
                return parseFromFile(env);
            }

            var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (currentDir != null)
            {
                var mapFile = Path.Combine(currentDir.FullName, DefaultMapFile);
                if (File.Exists(mapFile))
                {
                    return parseFromFile(mapFile);
                }
                currentDir = currentDir.Parent;
            }
            return null;
        }
        public static TapMap parseFromFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new Exception($"File {path} does not exist");
            }

            var scanner = new Scanner(new StreamReader(path));
            var parser = new Parser(scanner);
            parser.Consume<StreamStart>();
            parser.Consume<DocumentStart>();
            Serialization.TapMapDocumentYamlStruct document;
            try
            {
                document = Serialization.TapMapDocumentYamlStruct.Parse(parser);
                parser.Consume<DocumentEnd>();
                parser.Consume<StreamEnd>();
            }
            catch (Exception e)
            {
                var currentPosition = scanner.CurrentPosition;
                var message = $"Got exception while parsing {path}, before {currentPosition.Line}:{currentPosition.Column}";
                throw new Exception(message, e);
            }
            Dictionary<string, TapMapStruct> map = new Dictionary<string, TapMapStruct>();
            foreach (var kv in document)
            {
                map.Add(kv.Key, kv.Value.Convert(kv.Key));
            }

            Console.WriteLine("Parsed {0} tap maps", map.Count);

            return createFromRawMap(map);
        }

        private static TapMap createFromRawMap(Dictionary<string, TapMapStruct> map)
        {
            var names = new List<string>();
            var name2id = new Dictionary<string, int>();
            foreach (var kv in map)
            {
                name2id.Add(kv.Key, names.Count);
                names.Add(kv.Key);
            }

            if (name2id.Count != map.Count)
            {
                throw new Exception("Duplicate map name found");
            }

            var depMap = new int[map.Count];
            for (int i = 0; i < depMap.Length; i++)
            {
                depMap[i] = -1;
            }
            foreach (var kv in map)
            {
                if (kv.Value.Extends == null) continue;
                if (!name2id.ContainsKey(kv.Value.Extends))
                {
                    throw new Exception($"Unknown map '{kv.Value.Extends}' in extends");
                }
                depMap[name2id[kv.Key]] = name2id[kv.Value.Extends];
            }

            int[] order;
            if (!findResolveOrder(depMap, out order))
            {
                throw new Exception("Circular dependency detected");
            }

            foreach (var i in order)
            {
                var name = names[i];
                var extend = map[name].Extends;
                if (extend != null)
                {
                    extendsMap(map[name], map[extend]);
                }
            }

            var finalNames = names.Where(name => !name.StartsWith("abstract-")).ToList();
            var defaultMaps = finalNames.Where(name => map[name].IsDefault).ToList();
            if (defaultMaps.Count == 0)
            {
                throw new Exception("No default map found");
            }
            if (defaultMaps.Count > 1)
            {
                throw new Exception("Multiple default maps found");
            }

            var finalMaps = new List<TapMapStruct>();
            finalMaps.Add(map[defaultMaps[0]]);
            foreach (var name in finalNames)
            {
                if (name == defaultMaps[0]) continue;
                finalMaps.Add(map[name]);
            }

            var finalMap2Idx = new Dictionary<string, int>();
            finalMap2Idx.Add("", 0);
            for (var i = 0; i < finalMaps.Count; i++)
            {
                finalMap2Idx.Add(finalMaps[i].Name, i);
            }

            foreach (var m in finalMaps)
            {
                updateMapTarget(m, finalMap2Idx);
            }

            return new TapMap(finalMaps.ToArray());
        }

        private static void updateMapTarget(TapMapStruct m, Dictionary<string, int> finalMap2Idx)
        {
            {
                for (int i = 1; i < m.Hotkeys.Length; i++)
                {
                    if (m.Hotkeys[i] is TapMapHotkeyModeSwitchResolvingStruct resolving)
                    {
                        if (!finalMap2Idx.ContainsKey(resolving.Target))
                        {
                            throw new Exception($"Unknown map '{resolving.Target}' in mode switch");
                        }
                        m.Hotkeys[i] = new TapMapHotkeyModeSwitchStruct((uint)finalMap2Idx[resolving.Target], resolving.Type);
                    }
                    else if (m.Hotkeys[i] is TapMapHotkeyMultiTapsStruct multi)
                    {
                        for (int j = 0; j < multi.Hotkeys.Length; j++)
                        {
                            if (multi.Hotkeys[j] is TapMapHotkeyModeSwitchResolvingStruct resolving2)
                            {
                                if (!finalMap2Idx.ContainsKey(resolving2.Target))
                                {
                                    throw new Exception($"Unknown map '{resolving2.Target}' in mode switch");
                                }
                                multi.Hotkeys[j] = new TapMapHotkeyModeSwitchStruct((uint)finalMap2Idx[resolving2.Target], resolving2.Type);
                            }
                        }
                    }
                }
            }
        }

        private static void extendsMap(TapMapStruct map, TapMapStruct extend)
        {
            // IsDefault: not inherited
            // KeepInStack: not inherited
            // Extends: not inherited - already resolving
            // Hotkeys: inherited
            for (int i = 1; i < map.Hotkeys.Length; i++)
            {
                if (map.Hotkeys[i] == null)
                {
                    map.Hotkeys[i] = extend.Hotkeys[i];
                }
                else
                {
                    if (extend.Hotkeys[i] != null)
                    {
                        throw new Exception($"Cannot extend hotkey {i} from {extend.Name} to {map.Name}");
                    }
                }
            }
        }

        private static bool findResolveOrder(int[] depMap, [MaybeNullWhen(false)] out int[] order)
        {
            int n = depMap.Length;
            List<int> orderList = new List<int>();
            bool[] visited = new bool[n];
            bool[] inStack = new bool[n];
            bool dfs(int i)
            {
                if (visited[i]) return true;
                if (inStack[i]) return false;
                inStack[i] = true;
                if (depMap[i] != -1)
                {
                    if (!dfs(depMap[i])) return false;
                }
                visited[i] = true;
                inStack[i] = false;
                orderList.Add(i);
                return true;
            }
            for (int i = 0; i < n; i++)
            {
                if (!dfs(i))
                {
                    order = null;
                    return false;
                }
            }
            order = orderList.ToArray();
            return true;
        }

        internal static TapMap Default()
        {
            var map = new TapMapStruct[1];
            map[0] = new TapMapStruct("default", null, true, true);

            for (int i = 0; i < map[0].Hotkeys.Length; i++)
            {
                map[0].Hotkeys[i] = new TapMapHotkeyKeyStruct([new TSCharKeyEventDefinition('?')]);
            }

            return new TapMap(map);
        }
    }
}
