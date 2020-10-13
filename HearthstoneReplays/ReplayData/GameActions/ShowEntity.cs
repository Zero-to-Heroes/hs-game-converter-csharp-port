#region

using System.Collections.Generic;
using System.Xml.Serialization;
using HearthstoneReplays.Enums;
using System.Linq;

#endregion

namespace HearthstoneReplays.Parser.ReplayData.GameActions
{
	public class ShowEntity : GameData, IEntityData 

	{
		[XmlAttribute("cardID")]
		public string CardId { get; set; }

		[XmlAttribute("entity")]
		public int Entity { get; set; }

		[XmlElement("Tag", typeof(Tag))]
		public List<Tag> Tags { get; set; }

		[XmlIgnore]
		public string SubSpellInEffect { get; set; }

        public int GetTag(GameTag tag)
        {
            var match = Tags.FirstOrDefault(t => t.Name == (int)tag);
            return match == null ? -1 : match.Value;
        }

		public string GetPlayerClass()
		{
			var playerClass = GetTag(GameTag.CLASS);
			return ((CardClass)playerClass).ToString();
		}
    }
}