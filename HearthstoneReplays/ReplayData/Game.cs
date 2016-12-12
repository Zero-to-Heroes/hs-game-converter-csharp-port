﻿#region

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
	}
}