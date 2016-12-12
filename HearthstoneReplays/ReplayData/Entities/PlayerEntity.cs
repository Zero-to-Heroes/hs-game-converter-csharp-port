﻿#region

using System.Xml.Serialization;

#endregion

namespace HearthstoneReplays.Parser.ReplayData.Entities
{
	public class PlayerEntity : BaseEntity
	{
		[XmlAttribute("accountHi")]
		public string AccountHi { get; set; }

		[XmlAttribute("accountLo")]
		public string AccountLo { get; set; }

		[XmlAttribute("playerID")]
		public int PlayerId { get; set; }

		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlAttribute("rank")]
		public string Rank { get; set; }

		[XmlAttribute("legendRank")]
		public string LegendRank { get; set; }

		[XmlAttribute("cardback")]
		public string Cardback { get; set; }
	}
}