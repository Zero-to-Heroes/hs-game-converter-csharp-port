﻿#region

using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using HearthstoneReplays.Enums;
using System;

#endregion

namespace HearthstoneReplays.Parser.ReplayData.Entities
{
	[XmlInclude(typeof(GameEntity))]
	[XmlInclude(typeof(PlayerEntity))]
	[XmlInclude(typeof(FullEntity))]
	public abstract class BaseEntity : GameData
	{
		[XmlAttribute("id")]
		public int Id { get; set; }

		[XmlElement("Tag", typeof(Tag))]
		public List<Tag> Tags { get; set; }

		public override bool Equals(object obj)
		{
			var other = obj as BaseEntity;
			if(other == null)
				return false;
			return Id == other.Id && Tags.All(tag => other.Tags.Any(t1 => t1.Name == tag.Name && t1.Value == tag.Value));
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

        public int GetTag(GameTag tag, int defaultValue = -1)
        {
            var match = Tags.FirstOrDefault(t => t.Name == (int)tag);
            return match == null ? defaultValue : match.Value;
        }

        public List<Tag> GetTagsCopy()
        {
			return this.Tags
				.Select(tag => new Tag
				{
					Name = tag.Name,
					Value = tag.Value,
				})
				.ToList();
        }
    }
}