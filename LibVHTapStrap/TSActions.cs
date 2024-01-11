using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibVHTapStrap
{
    internal interface ITSAction
    {
        void Invoke();
    }

    internal class TSActionChangeMapping(
        ITSMappingManager mappingManager,
        TSMappingID MappingID
    ) : ITSAction
    {
        private ITSMappingManager mappingManager = mappingManager;
        public TSMappingID MappingID { get; } = MappingID;

        public void Invoke()
        {
            mappingManager.SetMap(MappingID);
        }
    }

    internal class TSActionHotkey<IHotKeyDefinitionBase, HotkeyDefinitionT>(
        ITSKeyboard<IHotKeyDefinitionBase> keyboard,
        HotkeyDefinitionT definition
    ) : ITSAction
        where IHotKeyDefinitionBase : ITSHotKeyDefinition
        where HotkeyDefinitionT : IHotKeyDefinitionBase
    {
        private ITSKeyboard<IHotKeyDefinitionBase> keyboard = keyboard;
        public HotkeyDefinitionT KeyDefinition { get; } = definition;

        public void Invoke()
        {
            keyboard.SendKey(KeyDefinition);
        }
    }
}
