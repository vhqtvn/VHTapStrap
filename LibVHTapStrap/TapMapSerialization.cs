using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core.Events;
using YamlDotNet.Core;

namespace LibVHTapStrap
{
    namespace Serialization
    {
        public static class Keys
        {
            static Dictionary<VK, TSModifiers> modifiers = new Dictionary<VK, TSModifiers>
            {
                { VK.LWIN,TSModifiers.LSuper},
                { VK.RWIN,TSModifiers.RSuper},

                { VK.CONTROL,TSModifiers.LControl},
                { VK.LCONTROL,TSModifiers.LControl},
                { VK.RCONTROL,TSModifiers.RControl},

                { VK.SHIFT,TSModifiers.LShift},
                { VK.LSHIFT,TSModifiers.LShift},
                { VK.RSHIFT,TSModifiers.RShift},

                { VK.MENU,TSModifiers.LAlt},
                { VK.LMENU,TSModifiers.LAlt},
                { VK.RMENU,TSModifiers.RAlt},
            };

            static Dictionary<VK, string[]> vk2names = new Dictionary<VK, string[]>()
            {
                {VK.LWIN, new string[]{"Win", "LWin"} },
                {VK.RWIN, new string[]{"RWin"} },

                {VK.CONTROL, new string[]{"Control", "Ctrl"} },
                {VK.LCONTROL, new string[]{"LControl", "LCtrl"} },
                {VK.RCONTROL, new string[]{"RControl", "RCtrl"} },

                {VK.SHIFT, new string[]{"Shift"} },
                {VK.LSHIFT, new string[]{"LShift"} },
                {VK.RSHIFT, new string[]{"RShift"} },

                {VK.MENU, new string[]{"Alt"} },
                {VK.LMENU, new string[]{"LAlt"} },
                {VK.RMENU, new string[]{"RAlt"} },

                {VK.ESCAPE, new string[]{"Escape", "ESC"} },
                {VK.BACKSPACE, new string[]{"Backspace"} },
                {VK.SPACE, new string[]{"Space"} },
                {VK.TAB, new string[]{"Tab"} },
                {VK.RETURN, new string[]{"Enter", "Return"} },

                {VK.LEFT, new string[]{"Left"} },
                {VK.UP, new string[]{"Up"} },
                {VK.RIGHT, new string[]{"Right"} },
                {VK.DOWN, new string[]{"Down"} },

                {VK.INSERT, new string[]{"Insert"} },
                {VK.DELETE, new string[]{"Delete"} },
                {VK.PRIOR, new string[]{"PageUp", "PgUp"} },
                {VK.NEXT, new string[]{"PageDown", "PgDown"} },

                {VK.HOME, new string[]{"Home"} },
                {VK.END, new string[]{"End"} },
            };

            static Dictionary<string, VK> name2vk = vk2names.SelectMany(
                kv => kv.Value.Select(
                    name => new KeyValuePair<string, VK>(name, kv.Key)
                )
            ).ToDictionary(kv => kv.Key, kv => kv.Value);

            public static VK? Parse(string name)
            {
                if (name2vk.TryGetValue(name, out var result))
                {
                    return result;
                }
                // parse from VK_xx where xx is hex
                if (name.StartsWith("VK_"))
                {
                    if (uint.TryParse(name.Substring(3), System.Globalization.NumberStyles.HexNumber, null, out var vk))
                    {
                        return (VK)vk;
                    }
                }
                return null;
            }

            internal struct VKWithModifiers(
                VK? key,
                TSModifiers modifiers
            )
            {
                internal VK? Key { get; } = key;
                internal TSModifiers Modifiers { get; } = modifiers;
            }

            internal static VKWithModifiers? ParseWithModifiers(string name)
            {
                var parts = name.Split('+');
                VK? key = null;
                TSModifiers modfiers = 0;
                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    var parsedKey = Parse(part);
                    if (parsedKey == null)
                    {
                        return null;
                    }
                    if (modifiers.TryGetValue(parsedKey.Value, out var modifier))
                    {
                        modfiers |= modifier;
                    }
                    else
                    {
                        if (key != null)
                        {
                            return null;
                        }
                        key = parsedKey.Value;
                    }
                }

                return new VKWithModifiers(key, modfiers);
            }

            public static string? GetName(VK vk)
            {
                if (vk2names.TryGetValue(vk, out var result))
                {
                    return result[0];
                }
                return string.Format("VK_{0:X}", vk);
            }
        }

        internal abstract class TapMapHotkeyYamlStruct
        {
            public static TapMapHotkeyYamlStruct Parse(IParser parser, bool allowList = true)
            {
                if (allowList && parser.Accept<SequenceStart>(out var _))
                {
                    return TapMapHotkeyMultiTapsYamlStruct.Parse(parser);
                }
                if (parser.TryConsume<Scalar>(out var text))
                {
                    if (text.Value == "null" && text.IsPlainImplicit)
                    {
                        return new TapMapHotkeyEmptyYamlStruct();
                    }
                    return new TapMapHotkeyTextYamlStruct()
                    {
                        Text = text.Value
                    };
                }
                parser.Consume<MappingStart>();
                TapMapHotkeyYamlStruct result;
                switch (parser.Consume<Scalar>().Value)
                {
                    case "modifier":
                        result = TapMapHotkeyKeyYamlStruct.Parse(parser, modifiersOnly: true);
                        break;
                    case "key":
                        result = TapMapHotkeyKeyYamlStruct.Parse(parser, modifiersOnly: false);
                        break;
                    case "mode":
                        result = TapMapHotkeyModeSwitchYamlStruct.Parse(parser);
                        break;
                    default:
                        throw new Exception($"Invalid hotkey type: {parser.Current}");
                }
                parser.Consume<MappingEnd>();
                return result;
            }

            public static bool ParseBoolean(IParser parser)
            {
                return bool.Parse(parser.Consume<Scalar>().Value);
            }

            public abstract TapMapHotkeyStruct Convert();
        }

        internal class TapMapHotkeyKeyYamlStruct(
            string[] keys,
            bool modifiersOnly
        ) : TapMapHotkeyYamlStruct
        {
            public string[] Hotkeys { get; } = keys;
            public bool ModifierOnly { get; } = modifiersOnly;

            public static TapMapHotkeyYamlStruct Parse(IParser parser, bool modifiersOnly = true)
            {
                if (parser.Accept<SequenceStart>(out var _))
                {
                    var result = new List<string>();
                    while (!parser.Accept<SequenceEnd>(out var _))
                    {
                        result.Add(parser.Consume<Scalar>().Value);
                    }
                    parser.Consume<SequenceEnd>();
                    return new TapMapHotkeyKeyYamlStruct(result.ToArray(), modifiersOnly);
                }
                else
                {
                    return new TapMapHotkeyKeyYamlStruct([parser.Consume<Scalar>().Value], modifiersOnly);
                }
            }
            public override TapMapHotkeyStruct Convert()
            {
                return new TapMapHotkeyKeyStruct(
                    Hotkeys.Select(x =>
                    {
                        var vk = Keys.ParseWithModifiers(x);
                        if (vk == null)
                        {
                            throw new Exception($"Unknown key: {x}");
                        }
                        TSHotkeyBase result;
                        if (ModifierOnly)
                        {
                            if (vk.Value.Key != null)
                            {
                                throw new Exception($"Only modifier allowed: {x}");
                            }
                            result = new TSKeyAddModifierDefinition(vk.Value.Modifiers, resetOnNextCompose: true);
                        }
                        else
                        {
                            if (vk.Value.Key == null)
                            {
                                throw new Exception($"Need a non-modifiera key: {x}");
                            }
                            result = new TSRawKeyEventDefinition(vk.Value.Key.Value, addModifiers: vk.Value.Modifiers);
                        }
                        return result;
                    }).ToArray()
                );
            }
        }

        internal class TapMapHotkeyEmptyYamlStruct : TapMapHotkeyYamlStruct
        {
            public override TapMapHotkeyStruct Convert()
            {
                return new TapMapHotkeyEmptyStruct();
            }
        }

        internal class TapMapHotkeyTextYamlStruct : TapMapHotkeyYamlStruct
        {
            public string Text;

            public override TapMapHotkeyStruct Convert()
            {
                return new TapMapHotkeyKeyStruct(Text.ToCharArray().Select(x => new TSCharKeyEventDefinition(x)).ToArray());
            }
        }

        internal class TapMapHotkeyModeSwitchYamlStruct : TapMapHotkeyYamlStruct
        {
            public string Target;
            public string Type;

            public static TapMapHotkeyModeSwitchYamlStruct Parse(IParser parser)
            {
                parser.Consume<MappingStart>();
                var kv = new Dictionary<string, string>();
                while (!parser.Accept<MappingEnd>(out var _))
                {
                    var key = parser.Consume<Scalar>().Value;
                    var value = parser.Consume<Scalar>().Value;
                    kv.Add(key, value);
                }
                parser.Consume<MappingEnd>();
                string? type;
                if (!kv.TryGetValue("type", out type))
                {
                    throw new Exception("Missing type");
                }
                kv.Remove("type");
                string? target = "";
                switch (type)
                {
                    case "reset":
                    case "pop":
                        break;
                    case "push":
                    case "once":
                        if (!kv.TryGetValue("to", out target))
                        {
                            throw new Exception("Missing 'to'");
                        }
                        kv.Remove("to");
                        break;
                    default:
                        throw new Exception($"Invalid type: {type}");
                }
                if (kv.Count != 0)
                {
                    throw new Exception($"Invalid hotkey configuration for {type}, unused keys {string.Join(", ", kv.Keys)}");
                }
                return new TapMapHotkeyModeSwitchYamlStruct()
                {
                    Target = target,
                    Type = type
                };
            }

            public override TapMapHotkeyStruct Convert()
            {
                switch (Type)
                {
                    case "reset":
                        return new TapMapHotkeyModeSwitchResolvingStruct(Target, TapMapHotkeyModeSwitchType.Reset);
                    case "push":
                        return new TapMapHotkeyModeSwitchResolvingStruct(Target, TapMapHotkeyModeSwitchType.Push);
                    case "pop":
                        return new TapMapHotkeyModeSwitchResolvingStruct(Target, TapMapHotkeyModeSwitchType.Pop);
                    case "once":
                        return new TapMapHotkeyModeSwitchResolvingStruct(Target, TapMapHotkeyModeSwitchType.Once);
                    default:
                        throw new Exception($"Invalid type: {Type}");
                }
            }
        }

        internal class TapMapHotkeyMultiTapsYamlStruct : TapMapHotkeyYamlStruct
        {
            public TapMapHotkeyYamlStruct[] Hotkeys;

            public static TapMapHotkeyMultiTapsYamlStruct Parse(IParser parser)
            {
                parser.Consume<SequenceStart>();
                var result = new List<TapMapHotkeyYamlStruct>();
                while (!parser.Accept<SequenceEnd>(out var _))
                {
                    result.Add(Parse(parser, false));
                }
                parser.Consume<SequenceEnd>();
                return new TapMapHotkeyMultiTapsYamlStruct()
                {
                    Hotkeys = result.ToArray()
                };
            }

            public override TapMapHotkeyStruct Convert()
            {
                return new TapMapHotkeyMultiTapsStruct(Hotkeys.Select(x => x.Convert()).ToArray());
            }
        }

        interface ITapSingleActionYamlStruct
        {
            ITapMapHotkeySingleActionStruct Convert();
        }
        internal class TapActionYamlStruct
        {
            public List<ITapSingleActionYamlStruct> Actions = new List<ITapSingleActionYamlStruct>();

            public ITapMapHotkeySingleActionStruct[] Convert()
            {
                return Actions.Select(x => x.Convert()).ToArray();
            }

            public static ITapSingleActionYamlStruct ParseSingle(IParser parser)
            {
                parser.Consume<MappingStart>();
                Dictionary<string, object> kv = new Dictionary<string, object>();
                while (!parser.Accept<MappingEnd>(out var _))
                {
                    var key = parser.Consume<Scalar>().Value;
                    switch (key)
                    {
                        case "action":
                        case "message":
                            {
                                var value = parser.Consume<Scalar>().Value;
                                kv.Add(key, value);
                                break;
                            }
                        case "duration":
                            {
                                var value = int.Parse(parser.Consume<Scalar>().Value);
                                kv.Add(key, value);
                                break;
                            }
                        case "vibrate":
                            {
                                parser.Consume<SequenceStart>();
                                var vibrate = new List<int>();
                                while (!parser.Accept<SequenceEnd>(out var _))
                                {
                                    vibrate.Add(int.Parse(parser.Consume<Scalar>().Value));
                                }
                                parser.Consume<SequenceEnd>();
                                kv.Add(key, vibrate.ToArray());
                                break;
                            }
                        default:
                            throw new Exception($"Invalid key: {key}");
                    }
                }
                parser.Consume<MappingEnd>();
                if (kv.TryGetValue("action", out var action))
                {
                    switch (action)
                    {
                        case "vibrate": return TapSingleActionVibrateYamlStruct.Parse(kv);
                        case "notify": return TapSingleActionNotifyYamlStruct.Parse(kv);
                    }
                }
                throw new Exception("Cannot parse TapSingleActionVibrateYamlStruct");
            }
            public static TapActionYamlStruct Parse(IParser parser)
            {
                var result = new TapActionYamlStruct();
                if (!parser.TryConsume<SequenceStart>(out var _))
                {
                    result.Actions.Add(ParseSingle(parser));
                }
                else
                {
                    while (!parser.Accept<SequenceEnd>(out var _))
                    {
                        result.Actions.Add(ParseSingle(parser));
                    }
                    parser.Consume<SequenceEnd>();
                }
                return result;
            }
        }

        internal class TapSingleActionVibrateYamlStruct : ITapSingleActionYamlStruct
        {
            public int[] Vibrate;
            public static TapSingleActionVibrateYamlStruct Parse(Dictionary<string, object> kv)
            {
                if (!kv.TryGetValue("vibrate", out var vibrate))
                {
                    throw new Exception("Missing 'vibrate'");
                }
                return new TapSingleActionVibrateYamlStruct()
                {
                    Vibrate = (int[])vibrate
                };
            }
            public ITapMapHotkeySingleActionStruct Convert()
            {
                return new TapMapHotkeySingleActionVibrateStruct(Vibrate);
            }
        }

        internal class TapSingleActionNotifyYamlStruct : ITapSingleActionYamlStruct
        {
            public string Message;
            public int Duration;
            public static TapSingleActionNotifyYamlStruct Parse(Dictionary<string, object> kv)
            {
                if (!kv.TryGetValue("message", out var message))
                {
                    throw new Exception("Missing 'message'");
                }
                if (!kv.TryGetValue("duration", out var duration))
                {
                    throw new Exception("Missing 'duration'");
                }
                return new TapSingleActionNotifyYamlStruct()
                {
                    Message = (string)message,
                    Duration = (int)duration
                };
            }
            public ITapMapHotkeySingleActionStruct Convert()
            {
                return new TapMapHotkeySingleActionNotifyStruct(Message, Duration);
            }
        }

        internal class TapMapYamlStruct
        {
            public Dictionary<uint, TapMapHotkeyYamlStruct> Hotkeys = new Dictionary<uint, TapMapHotkeyYamlStruct>();
            public bool IsDefault = false;
            public bool KeepInStack = true;
            public string? Extends = null;
            public TapActionYamlStruct? EnterAction = null;
            public TapActionYamlStruct? ExitAction = null;

            public static uint ParseTapHotkeyAnnotation(string annotation)
            {
                if (annotation.Length != 5) throw new Exception($"Invalid annotation: {annotation}");
                var result = 0u;
                for (int i = 0; i < 5; i++)
                {
                    result <<= 1;
                    if (annotation[i] == '.')
                    {
                        result |= 1;
                    }
                }
                return result;
            }

            public static TapMapYamlStruct Parse(IParser parser)
            {
                parser.Consume<MappingStart>();
                var result = new TapMapYamlStruct();
                while (parser.TryConsume<Scalar>(out var key))
                {
                    var name = key.Value;
                    switch (name)
                    {
                        case ":enter":
                            result.EnterAction = TapActionYamlStruct.Parse(parser);
                            break;
                        case ":exit":
                            result.ExitAction = TapActionYamlStruct.Parse(parser);
                            break;
                        case ":extends":
                            result.Extends = parser.Consume<Scalar>().Value;
                            break;
                        case ":default":
                            result.IsDefault = TapMapHotkeyYamlStruct.ParseBoolean(parser);
                            break;
                        case ":in-stack":
                            result.KeepInStack = TapMapHotkeyYamlStruct.ParseBoolean(parser);
                            break;
                        default:
                            var tapHotkey = ParseTapHotkeyAnnotation(name);
                            var value = TapMapHotkeyYamlStruct.Parse(parser);
                            result.Hotkeys.Add(tapHotkey, value);
                            break;
                    }
                }
                parser.Consume<MappingEnd>();
                return result;
            }

            public TapMapStruct Convert(string name)
            {
                var result = new TapMapStruct(name,
                        extends: Extends,
                        isDefault: IsDefault,
                        keepInStack: KeepInStack,
                        enterAction: EnterAction?.Convert(),
                        exitAction: ExitAction?.Convert()
                  );

                foreach (var kv in Hotkeys)
                {
                    if (kv.Key <= 0 || kv.Key >= 32) throw new Exception($"Invalid tap hotkey: {kv.Key}");
                    result.Hotkeys[kv.Key] = kv.Value.Convert();
                }
                return result;
            }
        }

        internal class TapMapDocumentYamlStruct : Dictionary<string, TapMapYamlStruct>
        {
            public static TapMapDocumentYamlStruct Parse(IParser parser)
            {
                parser.Consume<MappingStart>();
                var result = new TapMapDocumentYamlStruct();
                while (parser.TryConsume<Scalar>(out var key))
                {
                    var name = key.Value;
                    var value = TapMapYamlStruct.Parse(parser);
                    result.Add(name, value);
                }
                parser.Consume<MappingEnd>();
                return result;
            }
        }
    }

}
