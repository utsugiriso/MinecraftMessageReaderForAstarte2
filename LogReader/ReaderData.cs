using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FNF.Utility;
using System.Xml.Serialization;

namespace LogReader
{
    class ReaderData
    {
        public const int TONE_DEFAULT = 100;
        public const int SPEED_DEFAULT = 100;
        public const int VOLUME_DEFAULT = 100;

        private string characterName;
        private VoiceType voiceType = VoiceType.Default;
        private int tone;
        private int volume;

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

        public int Tone
        {
            get
            {
                return tone;
            }
            set
            {
                tone = value;
            }
        }

        public int Volume
        {
            get
            {
                return volume;
            }
            set
            {
                volume = value;
            }
        }

        public VoiceType VoiceType
        {
            get
            {
                return voiceType;
            }
            set
            {
                voiceType = value;
            }
        }

        public int NumberVoiceType
        {
            get
            {
                return (int)voiceType;
            }
            set
            {
                voiceType = (VoiceType)Enum.ToObject(typeof(VoiceType), value);
            }
        }

        public string DisplayVoiceType
        {
            get
            {
                return Enum.GetName(typeof(VoiceType), voiceType);
            }
            set
            {
                voiceType = (VoiceType)Enum.Parse(typeof(VoiceType), value);
            }
        }

        public ReaderData(string characterName)
        {
            this.CharacterName = characterName;
            this.VoiceType = VoiceType.Default;
            this.Tone = TONE_DEFAULT;
            this.Volume = VOLUME_DEFAULT;
        }

        public ReaderData(string characterName, VoiceType voiceType, int tone)
        {
            this.CharacterName = characterName;
            this.VoiceType = voiceType;
            this.Tone = tone;
            this.Volume = VOLUME_DEFAULT;
        }

        public ReaderData(string characterName, VoiceType voiceType, int tone, int volume)
        {
            this.CharacterName = characterName;
            this.VoiceType = voiceType;
            this.Tone = tone;
            this.Volume = volume;
        }

        public int GetAdjustedSpeed()
        {
            return SPEED_DEFAULT - (Tone - TONE_DEFAULT);
        }
    }
}
