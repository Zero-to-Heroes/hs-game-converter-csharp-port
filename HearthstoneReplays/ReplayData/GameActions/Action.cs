﻿#region

using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using HearthstoneReplays.Parser.ReplayData.Entities;
using HearthstoneReplays.Parser.ReplayData.Meta;
using HearthstoneReplays.Parser.ReplayData.Meta.Options;

#endregion

namespace HearthstoneReplays.Parser.ReplayData.GameActions
{
	public class Action : GameAction
	{
		[XmlAttribute("index"), DefaultValue(-1)]
		public int Index { get; set; }

		[XmlAttribute("effectIndex"), DefaultValue(-1)]
		public int EffectIndex { get; set; }

		[XmlAttribute("target"), DefaultValue(0)]
		public int Target { get; set; }

		[XmlAttribute("type")]
		public int Type { get; set; }

		[XmlElement("Block", typeof(Action))]
		[XmlElement("Choices", typeof(Choices))]
		[XmlElement("FullEntity", typeof(FullEntity))]
		[XmlElement("ShowEntity", typeof(ShowEntity))]
		[XmlElement("HideEntity", typeof(HideEntity))]
		[XmlElement("GameEntity", typeof(GameEntity))]
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