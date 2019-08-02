#region

using System;
using System.Collections.Generic;
using System.Linq;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using Type = System.Type;

#endregion

namespace HearthstoneReplays.Parser
{
	public class Helper
	{
        private readonly List<string> innkeeperNames = new List<string>() { "The Innkeeper", "Aubergiste", "Gastwirt",
            "El tabernero", "Locandiere", "酒場のオヤジ", "여관주인",  "Karczmarz", "O Estalajadeiro", "Хозяин таверны",
            "เจ้าของโรงแรม", "旅店老板", "旅店老闆" };


        private readonly Dictionary<GameTag, Type> TagTypes = new Dictionary<GameTag, Type>
		{
			{GameTag.CARDTYPE, typeof(CardType)},
			{GameTag.CLASS, typeof(CardClass)},
			{GameTag.FACTION, typeof(Faction)},
			{GameTag.PLAYSTATE, typeof(PlayState)},
			{GameTag.RARITY, typeof(Rarity)},
			{GameTag.MULLIGAN_STATE, typeof(Mulligan)},
			{GameTag.NEXT_STEP, typeof(Step)},
			{GameTag.STATE, typeof(State)},
            {GameTag.STEP, typeof(Step)},
            {GameTag.CARDRACE, typeof(Race)},
            {GameTag.ZONE, typeof(Zone)}
		};

		public int ParseEntity(string data, ParserState state)
		{
		    if (string.IsNullOrEmpty(data))
		        return 0;
            var match = Regexes.EntityRegex.Match(data);
			if(match.Success)
				return int.Parse(match.Groups[1].Value);
			if(data == "GameEntity")
				return 1; 
			int numeric;
			if(int.TryParse(data, out numeric))
				return numeric;
			return GetPlayerIdFromName(data, state);
		}

		public int GetPlayerIdFromName(string data, ParserState state)
		{
			var firstPlayer = (PlayerEntity)state.CurrentGame.Data.FirstOrDefault(x => (x is PlayerEntity) && ((PlayerEntity)x).Id == state.FirstPlayerId);
			if(firstPlayer == null) throw new Exception("Could not find first player " + data);

            var secondPlayer = (PlayerEntity)state.CurrentGame.Data.FirstOrDefault(x => (x is PlayerEntity) && ((PlayerEntity)x).Id != state.FirstPlayerId);
            if(secondPlayer == null) throw new Exception("Could not find second player " + data);

            if (firstPlayer.Name == data) return firstPlayer.Id;
            if (secondPlayer.Name == data) return secondPlayer.Id;
            if (string.IsNullOrEmpty(firstPlayer.Name))
		    {
		        firstPlayer.Name = data;
                return firstPlayer.Id;
            }
            // Sometimes we register the player name with the full battletag, 
            // and get only the short name afterwards
            if (firstPlayer.Name.IndexOf(data) != -1) return firstPlayer.Id;
            if (data != null && data.IndexOf(firstPlayer.Name) != -1) return firstPlayer.Id;
            if (string.IsNullOrEmpty(secondPlayer.Name))
		    {
		        secondPlayer.Name = data;
                return secondPlayer.Id;
            }
            // And the opposite
            if (secondPlayer.Name.IndexOf(data) != -1) return secondPlayer.Id;
            if (data != null && data.IndexOf(secondPlayer.Name) != -1) return secondPlayer.Id;

            if (firstPlayer.Name == "UNKNOWN HUMAN PLAYER" 
                || innkeeperNames.Select(x => x.ToLower()).Contains(firstPlayer.Name.ToLower()))
			{
				firstPlayer.Name = data;
				return firstPlayer.Id;
			}
			if(secondPlayer.Name == "UNKNOWN HUMAN PLAYER" 
                || innkeeperNames.Select(x => x.ToLower()).Contains(secondPlayer.Name.ToLower()))
			{
				secondPlayer.Name = data;
				return secondPlayer.Id;
			}
            // Case where the entity itself is replaced by a new hero, like in the Crooked Pete / Beastly Pete case
            var idFromState = state.GameState.PlayerIdFromEntityName(data);
            if (idFromState != 0)
            {
                return idFromState;
            }
            // Fringe case, but I saw it happen from time to time
            // We assume this happens in a game vs AI, so the human player is always the first
            // and if that's not the case, then we have no way to know which unknown human player this is
            if (data == "UNKNOWN HUMAN PLAYER")
            {
                return firstPlayer.Id;
            }
            throw new Exception("Could not get id from player name: " + data 
                + " // " + firstPlayer.Name + " // " + secondPlayer.Name);
		}

		public void setName(ParserState state, int playerId, String playerName)
		{
			List<PlayerEntity> players = state.getPlayers();
			String oldName = null;
			foreach (PlayerEntity entity in players)
			{
				if (entity.PlayerId == playerId && playerName != entity.Name)
				{
					oldName = entity.Name;
					entity.Name = playerName;
				}
			}

			foreach (PlayerEntity entity in players)
			{
				if (entity.PlayerId != playerId)
				{
					if (playerName == entity.Name)
					{
						entity.Name = null;
					}
					else if (oldName != null)
					{
						entity.Name = oldName;
					}
				}
			}
		}

		public Tag ParseTag(string tagName, string value)
		{
			Type tagType;
			int tagValue;

			var tag = new Tag();
			tag.Name = ParseEnum<GameTag>(tagName);

			if(TagTypes.TryGetValue((GameTag)tag.Name, out tagType))
				tag.Value = ParseEnum(tagType, value);
			else if(int.TryParse(value, out tagValue))
				tag.Value = tagValue;
			else
				throw new Exception(string.Format("Unhandled tag value: {0}={1}", tagName, value));
			return tag;
		}

		public int ParseEnum(Type type, string tag)
		{
			int value;
			if(int.TryParse(tag, out value))
				return value;
			var index = type.GetEnumNames().ToList().IndexOf(tag);
			if(index > -1)
				return (int)type.GetEnumValues().GetValue(index);
			throw new Exception("Enum not found: " + tag);
		}

		public int ParseEnum<T>(string tag)
		{
			return ParseEnum(typeof(T), tag);
		}
	}
}