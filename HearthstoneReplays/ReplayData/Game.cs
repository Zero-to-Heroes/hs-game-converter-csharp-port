#region

using System.Linq;
using System.Collections.Generic;
using System.Xml.Serialization;
using HearthstoneReplays.Parser.ReplayData.Entities;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using HearthstoneReplays.Parser.ReplayData.Meta;
using HearthstoneReplays.Parser.ReplayData.Meta.Options;
using System;

#endregion

namespace HearthstoneReplays.Parser.ReplayData
{
	public class Game
	{
        public readonly object listLock = new object();

        [XmlIgnore]
		public DateTime TimeStamp { get; set; }

        [XmlAttribute("ts")]
        public string TsForXml
        {
            get
            {
                var hours = TimeStamp.Hour;
                if (hours < ReplayParser.start.Hour)
                {
                    hours += 24;
                }
                var timestampString = this.TimeStamp.ToString("HH:mm:ss.ffffff");
                var split = timestampString.Split(':');
                return ("" + hours).PadLeft(2, '0') + ":" + split[1] + ":" + split[2];
            }
            set => this.TimeStamp = DateTime.Parse(value);
        }

        [XmlAttribute("buildNumber")]
		public int BuildNumber { get; set; }

		[XmlAttribute("type")]
		public int Type { get; set; }

		[XmlAttribute("gameType")]
		public int GameType { get; set; }

		[XmlAttribute("formatType")]
		public int FormatType { get; set; }

		[XmlAttribute("scenarioID")]
		public int ScenarioID { get; set; }

		[XmlAttribute("gameSeed")]
		public int GameSeed { get; set; }

		[XmlElement("Block", typeof(GameActions.Action))]
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
		[XmlElement("ShuffleDeck", typeof(ShuffleDeck))]
		public List<GameData> Data { get; set; }



        public Game()
		{
			Data = new List<GameData>();
            FormatType = -1;
            GameType = -1;
        }

        public void AddData(GameData data)
        {
            lock (listLock)
            {
                Data.Add(data);
            }
        }

		internal List<GameData> FilterGameData(params System.Type[] types)
		{
			// Build the list - could probably be built incrementally instead of rebuilding it completely every time
			List<GameData> result = new List<GameData>();
            lock (listLock)
            {
			    foreach (GameData data in Data)
			    {
				    result.Add(data);
				    ExtractData(result, data);
			    }

			    // Now filter it
			    return result.Where(data => types.Contains(data.GetType())).ToList();
            }
		}

		internal void ExtractData(List<GameData> result, GameData data)
		{
			if (data.GetType() == typeof(GameActions.Action))
			{
				foreach (GameData gameData in ((GameActions.Action) data).Data)
				{
					result.Add(gameData);
					if (data != gameData)
					{
						gameData.InternalParent = data;
					}
					ExtractData(result, gameData);
				}
			}
		}
	}
}