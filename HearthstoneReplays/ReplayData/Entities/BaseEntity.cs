#region

using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using HearthstoneReplays.Enums;
using System;
using Newtonsoft.Json;

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

        [XmlIgnore]
        [JsonIgnore]
        public List<Tag> TagsHistory { get; set; } = new List<Tag>();

        [XmlIgnore]
        [JsonIgnore]
        public List<Tag> AllPreviousTags { get; set; } = new List<Tag>();

        public override bool Equals(object obj)
        {
            var other = obj as BaseEntity;
            if (other == null)
                return false;
            return Id == other.Id && Tags.All(tag => other.Tags.Any(t1 => t1.Name == tag.Name && t1.Value == tag.Value));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public int GetTag(GameTag tag, int defaultValue = -1)
        {
            // Prevent concurrent access, though this does seem to be expensive...
            var match = Tags.FirstOrDefault(t => t.Name == (int)tag);
            return match == null ? defaultValue : match.Value;
        }

        public int GetTagSecure(GameTag tag, int defaultValue = -1)
        {
            // Prevent concurrent access, though this does seem to be expensive...
            var match = Tags.ToList().FirstOrDefault(t => t.Name == (int)tag);
            return match == null ? defaultValue : match.Value;
        }

        public bool TakesBoardSpace()
        {
            return GetTag(GameTag.CARDTYPE) == (int)CardType.MINION 
                || GetTag(GameTag.CARDTYPE) == (int)CardType.LOCATION
                || GetTag(GameTag.CARDTYPE) == (int)CardType.BATTLEGROUND_SPELL;
        }

        public BaseEntity SetTag(GameTag tag, int value)
        {
            if (Tags.FirstOrDefault(t => t.Name == (int)tag) == null)
            {
                Tags.Add(new Tag() { Name = (int)tag, Value = value });
            }
            Tags.FirstOrDefault(t => t.Name == (int)tag).Value = value;
            return this;
        }

        public int GetCost()
        {
            return GetTag(GameTag.COST, 0);
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

        public List<Tag> GetTagsCopy(TagChange tagChange = null)
        {            
            var tagsCopy = this.Tags
                .Select(tag => tagChange != null && tagChange.Name == tag.Name 
                ? new Tag()
                    {
                        Name = tag.Name,
                        Value = tagChange.Value,
                    } 
                : new Tag()
                    {
                        Name = tag.Name,
                        Value = tag.Value,
                    })
                .ToList();
            if (tagChange != null && !tagsCopy.Any(tag => tag.Name == tagChange.Name))
            {
                tagsCopy.Add(new Tag()
                {
                    Name = tagChange.Name,
                    Value = tagChange.Value,
                });
            }
            return tagsCopy;
        }
    }
}