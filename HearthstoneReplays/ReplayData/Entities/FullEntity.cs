#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Events;
using HearthstoneReplays.Parser.ReplayData.GameActions;

#endregion

namespace HearthstoneReplays.Parser.ReplayData.Entities
{
	[XmlRoot("FullEntity")]
	public class FullEntity : BaseEntity, IEntityData
	{
        public static IList<string> MANUAL_DREDGE = new List<string>()
        {
            CardIds.FromTheDepths,
        };

		[XmlAttribute("cardID")]
		public string CardId { get; set; }

        [XmlIgnore]
        public int Entity
        {
            get
            {
                return Id;
            }

            set
            {
                Id = value;
            }
        }

        [XmlIgnore]
        public List<int> KnownEntityIds = new List<int>();

        [XmlIgnore]
        public List<int> PlayedWhileInHand = new List<int>();

        [XmlIgnore]
        public List<string> CardIdsToCreate = new List<string>();

        [XmlIgnore]
        public SubSpell SubSpellInEffect { get; set; }

        public bool ShouldSerializeCardId()
		{
			return !string.IsNullOrEmpty(CardId);
		}

        internal FullEntity Clone()
        {
            DataContractSerializer dcSer = new DataContractSerializer(this.GetType());
            MemoryStream memoryStream = new MemoryStream();

            dcSer.WriteObject(memoryStream, this);
            memoryStream.Position = 0;

            FullEntity newObject = (FullEntity)dcSer.ReadObject(memoryStream);
            return newObject;
        }

        public string GetPlayerClass()
        {
            var playerClass = GetTag(GameTag.CLASS);
            return ((CardClass)playerClass).ToString();
        }

        internal int GetController()
        {
            return GetTag(GameTag.CONTROLLER);
        }

        internal int GetZone()
        {
            return GetTag(GameTag.ZONE);
        }

        internal int GetZonePosition()
        {
            if (GetTag(GameTag.FAKE_ZONE_POSITION) != -1)
            {
                return GetTag(GameTag.FAKE_ZONE_POSITION);
            }
            return GetTag(GameTag.ZONE_POSITION);
        }

        internal int GetCardType()
        {
            return GetTag(GameTag.CARDTYPE);
        }

        internal bool IsHero()
        {
            return GetCardType() == (int)CardType.HERO;
        }

        internal bool IsInPlay()
        {
            return GetZone() == (int)Zone.PLAY;
        }

        internal bool HasDredge()
        {
            return GetTag(GameTag.DREDGE) == 1 || IsManualDredge();
        }

        private bool IsManualDredge()
        {
            return MANUAL_DREDGE.Contains(this.CardId);
        }

        internal bool IsBaconGhost()
        {
            return GetTag(GameTag.BACON_IS_KEL_THUZAD) == 1;
        }

        internal bool IsMinion()
        {
            return GetTag(GameTag.CARDTYPE) == (int)CardType.MINION;
        }

        internal bool IsSpell()
        {
            return GetTag(GameTag.CARDTYPE) == (int)CardType.SPELL;
        }
    }
}