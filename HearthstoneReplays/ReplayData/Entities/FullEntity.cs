#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.GameActions;

#endregion

namespace HearthstoneReplays.Parser.ReplayData.Entities
{
	[XmlRoot("FullEntity")]
	public class FullEntity : BaseEntity, IEntityData
	{
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
        public List<string> KnownCardIds = new List<string>();

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
    }
}