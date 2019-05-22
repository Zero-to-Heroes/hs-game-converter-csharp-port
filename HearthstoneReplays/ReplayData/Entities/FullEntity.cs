#region

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using HearthstoneReplays.Parser.ReplayData.GameActions;

#endregion

namespace HearthstoneReplays.Parser.ReplayData.Entities
{
	[XmlRoot("FullEntity")]
	public class FullEntity : BaseEntity, IEntityData
	{
		[XmlAttribute("cardID")]
		public string CardId { get; set; }

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

        public bool ShouldSerializeCardId()
		{
			return !string.IsNullOrEmpty(CardId);
		}
	}
}