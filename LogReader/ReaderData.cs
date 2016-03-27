using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FNF.Utility;

namespace LogReader
{
    class ReaderData
    {
        private string characterName;
        private int type = 0;

        public string CharacterName
        {
            get
            {
                return characterName;
            }
            set
            {
                characterName = value;
            }
        }

        public int Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
            }
        }

        public string DisplayType
        {
            get
            {
                return Enum.GetName(typeof(VoiceType), type);
            }
        }

        public ReaderData(string characterName, int type)
        {
            this.CharacterName = characterName;
            this.Type = type;
        }
    }
}
