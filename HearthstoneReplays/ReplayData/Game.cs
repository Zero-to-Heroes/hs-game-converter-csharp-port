#region

using System.Linq;
using System.Collections.Generic;
using System.Xml.Serialization;
using HearthstoneReplays.Parser.ReplayData.Entities;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using HearthstoneReplays.Parser.ReplayData.Meta;
using HearthstoneReplays.Parser.ReplayData.Meta.Options;

#endregion

namespace HearthstoneReplays.Parser.ReplayData
{
	public class Game
	{
		[XmlAttribute("ts")]
		public string TimeStamp { get; set; }

        [XmlAttribute("type")]
        public int Type { get; set; }

		[XmlElement("Block", typeof(Action))]
		[XmlElement("Choices", typeof(Choices))]
		[XmlElement("FullEntity", typeof(FullEntity))]
		[XmlElement("GameEntity", typeof(GameEntity))]
		[XmlElement("ChangeEntity", typeof(ChangeEntity))]
		[XmlElement("ShowEntity", typeof(ShowEntity))]
		[XmlElement("HideEntity", typeof(HideEntity))]
		[XmlElement("Options", typeof(Options))]
		[XmlElement("Player", typeof(PlayerEntity))]
		[XmlElement("SendChoices", typeof(SendChoices))]
		[XmlElement("SendOption", typeof(SendOption))]
		[XmlElement("TagChange", typeof(TagChange))]
		[XmlElement("MetaData", typeof(MetaData))]
		[XmlElement("ChosenEntities", typeof(ChosenEntities))]
		public List<GameData> Data { get; set; }

		internal List<GameData> FilterGameData(params System.Type[] types)
		{
			// Build the list - could probably be built incrementally instead of rebuilding it completely every time
			List<GameData> result = new List<GameData>();
			foreach (GameData data in Data)
			{
				result.Add(data);
				ExtractData(result, data);
			}

			// Now filter it
			return result.Where(data => types.Contains(data.GetType())).ToList();
		}

		internal void ExtractData(List<GameData> result, GameData data)
		{
			if (data.GetType() == typeof(Action))
			{
				foreach (GameData gameData in ((Action) data).Data)
				{
					result.Add(gameData);
					ExtractData(result, gameData);
				}
			}
		}
	}
}