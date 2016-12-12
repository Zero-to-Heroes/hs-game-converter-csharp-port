﻿#region

using System.Collections.Generic;
using System.Xml.Serialization;

#endregion

namespace HearthstoneReplays.Parser.ReplayData.Entities
{
	[XmlRoot("FullEntity")]
	public class FullEntity : BaseEntity
	{
		[XmlAttribute("cardID")]
		public string CardId { get; set; }

		public bool ShouldSerializeCardId()
		{
			return !string.IsNullOrEmpty(CardId);
		}
	}
}