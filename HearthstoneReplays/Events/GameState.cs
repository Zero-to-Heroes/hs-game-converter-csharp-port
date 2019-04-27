#region
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using HearthstoneReplays.Parser.ReplayData.GameActions;
#endregion

namespace HearthstoneReplays.Parser.ReplayData.Entities
{
	public class GameState
	{
		public ParserState ParserState;

		public Dictionary<int, FullEntity> CurrentEntities = new Dictionary<int, FullEntity>();
        public Dictionary<string, int> EntityNames = new Dictionary<string, int>();
        public bool MulliganOver = false;

		public void Reset(ParserState state)
		{
			CurrentEntities = new Dictionary<int, FullEntity>();
			CurrentEntities.Add(1, new FullEntity { Id = 1, Tags = new List<Tag>() });
            MulliganOver = false;
            ParserState = state;
		}

        public FullEntity GetGameEntity()
        {
            return CurrentEntities[1];
        }

        public void UpdateEntityName(string rawEntity)
        {
            var match = Regexes.EntityWithNameAndId.Match(rawEntity);
            if (match.Success)
            {
                var entityName = match.Groups[1].Value;
                var entityId = match.Groups[2].Value;
                //Console.WriteLine("Adding entity mapping " + entityName + " " + entityId);
                EntityNames[entityName] = int.Parse(entityId);
            }
        }

        public GameStateReport BuildGameStateReport()
        {
            return new GameStateReport
            {
                LocalPlayer = PlayerReport.BuildPlayerReport(this, ParserState.LocalPlayer.Id),
                OpponentReport = PlayerReport.BuildPlayerReport(this, ParserState.OpponentPlayer.Id)
            };
        }

        public void PlayerEntity(PlayerEntity entity)
		{
			if (CurrentEntities.ContainsKey(entity.Id))
			{
				Logger.Log("error while parsing, playerEntity already present in memory", "" + entity.Id);
				return;
            }
            var newTags = new List<Tag>();
            foreach (var oldTag in entity.Tags)
            {
                newTags.Add(new Tag { Name = oldTag.Name, Value = oldTag.Value });
            }
            var fullEntity = new FullEntity { Id = entity.Id, Tags = newTags, TimeStamp = entity.TimeStamp };
			CurrentEntities.Add(entity.Id, fullEntity);
		}

		public void FullEntity(FullEntity entity, bool updating, string initialLog = null)
		{
			if (updating || CurrentEntities.ContainsKey(entity.Id))
			{
				// This actually happens in a normal scenario, so we just ignore it
				return;
			}

            var newTags = new List<Tag>();
            foreach (var oldTag in entity.Tags)
            {
                newTags.Add(new Tag { Name = oldTag.Name, Value = oldTag.Value });
            }
			var fullEntity = new FullEntity { CardId = entity.CardId, Id = entity.Id, Tags = newTags, TimeStamp = entity.TimeStamp };
			CurrentEntities.Add(entity.Id, fullEntity);
        }

        // Used in case of reconnt
        public void UpdateTagsForFullEntity(FullEntity entity)
        {
            if (!CurrentEntities.ContainsKey(entity.Id))
            {
                Logger.Log("No entity in memory when calling UpdateTagsForFullEntity, creating it", entity.Id);
                FullEntity(entity, false);
            }

            CurrentEntities[entity.Id].CardId = entity.CardId;
            List<int> newTagIds = entity.Tags.Select(tag => tag.Name).ToList();
            List<Tag> oldTagsToKeep = CurrentEntities[entity.Id].Tags
                .Where(tag => !newTagIds.Contains(tag.Name))
                .Select(tag => new Tag { Name = tag.Name, Value = tag.Value })
                .ToList();
            var newTags = new List<Tag>();
            foreach (var oldTag in entity.Tags)
            {
                newTags.Add(new Tag { Name = oldTag.Name, Value = oldTag.Value });
            }
            oldTagsToKeep.AddRange(newTags);
            CurrentEntities[entity.Id].Tags = oldTagsToKeep;
        }

        public void ShowEntity(ShowEntity entity)
        {
            if (!CurrentEntities.ContainsKey(entity.Entity))
            {
                Logger.Log("error while parsing, showentity doesn't have an entity in memory yet", "" + entity.Entity);
                return;
            }

            CurrentEntities[entity.Entity].CardId = entity.CardId;
            List<int> newTagIds = entity.Tags.Select(tag => tag.Name).ToList();
            List<Tag> oldTagsToKeep = CurrentEntities[entity.Entity].Tags
                .Where(tag => !newTagIds.Contains(tag.Name))
                .Select(tag => new Tag { Name = tag.Name, Value = tag.Value })
                .ToList();
            var newTags = new List<Tag>();
            foreach (var oldTag in entity.Tags)
            {
                newTags.Add(new Tag { Name = oldTag.Name, Value = oldTag.Value });
            }
            oldTagsToKeep.AddRange(newTags);
            CurrentEntities[entity.Entity].Tags = oldTagsToKeep;
        }

        public string GetCardIdForEntity(int id)
        {
            var entity = CurrentEntities[id];
            if (entity.CardId != null)
            {
                return entity.CardId;
            }
            var test = CurrentEntities.Values
                .Where((e) => e.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where((e) => e.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .ToList();
            return CurrentEntities.Values
                .Where((e) => e.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where((e) => e.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .Where((e) => e.GetTag(GameTag.CONTROLLER) == entity.GetTag(GameTag.CONTROLLER))
                .First()
                .CardId;
        }

        public FullEntity GetPlayerHeroEntity(int entityId)
        {
            var entity = CurrentEntities[entityId];
            if (entity.CardId != null)
            {
                return entity;
            }
            var heroesForController = CurrentEntities.Values
                .Where((e) => e.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where((e) => e.GetTag(GameTag.CONTROLLER) == entity.GetTag(GameTag.CONTROLLER))
                // If there are several, we take the most recent one
                .OrderByDescending((e) => e.TimeStamp);
            return heroesForController.First();
        }

        public void ChangeEntity(ChangeEntity entity)
        {
            if (!CurrentEntities.ContainsKey(entity.Entity))
            {
                Logger.Log("error while parsing, changeEntity doesn't have an entity in memory yet", "" + entity.Entity);
                return;
            }
            CurrentEntities[entity.Entity].CardId = entity.CardId;
            List<int> newTagIds = entity.Tags.Select(tag => tag.Name).ToList();
            List<Tag> oldTagsToKeep = CurrentEntities[entity.Entity].Tags
                .Where(tag => !newTagIds.Contains(tag.Name))
                .Select(tag => new Tag { Name = tag.Name, Value = tag.Value })
                .ToList();
            var newTags = new List<Tag>();
            foreach (var oldTag in entity.Tags)
            {
                newTags.Add(new Tag { Name = oldTag.Name, Value = oldTag.Value });
            }
            oldTagsToKeep.AddRange(newTags);
            CurrentEntities[entity.Entity].Tags = oldTagsToKeep;
        }

        public void TagChange(TagChange tagChange, string defChange, string initialLog = null)
		{
			if (!CurrentEntities.ContainsKey(tagChange.Entity))
			{
				//Logger.Log("error while parsing, tagchange doesn't have an entity in memory yet", "" + tagChange.Entity);
				return;
			}
			var existingTag = CurrentEntities[tagChange.Entity].Tags.Find((tag) => tag.Name == tagChange.Name);
			if (existingTag == null)
			{
				existingTag = new Tag { Name = tagChange.Name };
				CurrentEntities[tagChange.Entity].Tags.Add(existingTag);
			}
			RaiseTagChangeEvents(tagChange, existingTag.Value, defChange, initialLog);
			existingTag.Value = tagChange.Value;
		}

        // Should only be used for ChangeEntity, and probably even this will be removed later on
		public void Tag(Tag tag, int entityId)
		{
			if (!CurrentEntities.ContainsKey(entityId))
			{
				Logger.Log("error while parsing, tag doesn't have an entity in memory yet", "" + entityId);
				return;
			}
			var existingTag = CurrentEntities[entityId].Tags.Find((t) => tag.Name == t.Name);
			if (existingTag == null)
			{
				existingTag = new Tag { Name = tag.Name };
				CurrentEntities[entityId].Tags.Add(existingTag);
			}
			existingTag.Value = tag.Value;
		}

        public int PlayerIdFromEntityName(string data)
        {
            //Logger.Log("Gettingg player Id from EntityName", data);
            int entityId;
            EntityNames.TryGetValue(data, out entityId);
            if (entityId != 0)
            {
                //Logger.Log("Found matching entity id", entityId);
                // Now find the player this entity is attached to
                int playerId = CurrentEntities.Values
                    .Where(e => e.Tags.Find(x => (x.Name == (int)GameTag.HERO_ENTITY && x.Value == entityId)) != null)
                    .Select(e => e.Id)
                    .FirstOrDefault();
                if (playerId == 0)
                {
                    var entity = CurrentEntities.Values.Where(x => x.Id == entityId).FirstOrDefault();
                    var entityControllerId = entity.Tags.Find(x => x.Name == (int)GameTag.CONTROLLER).Value;
                    //Console.WriteLine("Controller ID = " + entityControllerId);
                    playerId = ParserState.getPlayers().Find(x => x.PlayerId == entityControllerId).Id;
                }
                //Logger.Log("Found matching player entity id for " + data, playerId);
                return playerId;
            }
            return 0;
        }

        private async void RaiseTagChangeEvents(TagChange tagChange, int previousValue, string defChange, string initialLog = null)
		{
			// Wait until we have all the necessary data
			while (ParserState.CurrentGame.FormatType == 0 || ParserState.CurrentGame.GameType == 0 || ParserState.LocalPlayer == null)
			{
				await Task.Delay(100);
            }

            if (tagChange.Name == (int)GameTag.GOLD_REWARD_STATE)
            {
                ParserState.EndCurrentGame();
            }

            if (tagChange.Name == (int)GameTag.MULLIGAN_STATE && tagChange.Value == (int)Mulligan.DONE)
            {
                MulliganOver = true;
            }
        }
	}
}