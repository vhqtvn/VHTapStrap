using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibVHTapStrap
{
    interface ITSMapping
    {
    }

    abstract class NumericID
    {
        public int ID { get; }

        public abstract string Name { get; }

        public NumericID(int id)
        {
            ID = id;
        }

        public override string ToString()
        {
            return $"{Name} {ID}";
        }
    }

    class TSMappingID(int id) : NumericID(id)
    {
        public override string Name { get; } = "Mapping";
    }

    interface ITSMappingManager
    {
        TSMappingID AddMapping(string name, ITSMapping mapping);

        void SetMap(TSMappingID mappingID);
    }


    interface ITSHotKeyDefinition
    {
    }

    interface ITSKeyboard<IHotKeyDefinitionBase>
        where IHotKeyDefinitionBase : ITSHotKeyDefinition
    {
        void SendKey(IHotKeyDefinitionBase definition);
    }
}
