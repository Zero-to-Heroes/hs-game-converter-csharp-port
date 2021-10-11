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
		public SubSpell SubSpellInEffect { get; set; }

        public int GetTag(GameTag tag, int defaultValue = -1)
        {
            var match = Tags.FirstOrDefault(t => t.Name == (int)tag);
            return match == null ? defaultValue : match.Value;
        }

		public string GetPlayerClass()
		{
			var playerClass = GetTag(GameTag.CLASS);
			return ((CardClass)playerClass).ToString();
		}

		public int GetEffectiveController()
		{
			var lettuceControllerId = GetTag(GameTag.LETTUCE_CONTROLLER);
			if (lettuceControllerId != -1)
			{
				return lettuceControllerId;
			}
			return GetTag(GameTag.CONTROLLER);
		}

		public bool IsInPlay()
		{
			return GetTag(GameTag.ZONE) == (int)Zone.PLAY;
		}
    }
}