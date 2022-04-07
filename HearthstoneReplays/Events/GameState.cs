#region
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Events;
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
        public int CurrentTurn = 0;
        public GameMetaData MetaData;
        // Because for BGS the first opponent player id is set as a tag of the player, instead 
        // of a tag change. At the time it is revealed, we don't have the opponent's entity yet
        // so we can't emit the event
        public int NextBgsOpponentPlayerId;
        public bool BattleResultSent;
        //public bool SimulationTriggered;
        public int LastCardPlayedEntityId;
        public int LastCardDrawnEntityId;
        // Most recent card is last, and grouped by turn
        public Dictionary<int, Dictionary<int, List<int>>> CardsPlayedByPlayerEntityId = new Dictionary<int, Dictionary<int, List<int>>>();
        public Dictionary<int, List<int>> SpellsPlayedByPlayerOnFriendlyEntityIds = new Dictionary<int, List<int>>();
        public string BgsCurrentBattleOpponent;
        public bool BgsHasSentNextOpponent;

        public Dictionary<int, List<FullEntity>> EntityIdsOnBoardWhenPlayingPotionOfIllusion = null;

        private int gameEntityId;
        private Dictionary<int, int> controllerEntity = new Dictionary<int, int>();

        public void Reset(ParserState state)
        {
            ParserState = state;
            CurrentEntities = new Dictionary<int, FullEntity>();
            EntityNames = new Dictionary<string, int>();
            CurrentTurn = 0;
            MetaData = null;
            NextBgsOpponentPlayerId = -1;
            BattleResultSent = false;
            LastCardPlayedEntityId = -1;
            // Stored by turn as well
            CardsPlayedByPlayerEntityId = new Dictionary<int, Dictionary<int, List<int>>>();
            SpellsPlayedByPlayerOnFriendlyEntityIds = new Dictionary<int, List<int>>();
            LastCardDrawnEntityId = -1;
            BgsCurrentBattleOpponent = null;
            BgsHasSentNextOpponent = false;
            EntityIdsOnBoardWhenPlayingPotionOfIllusion = null;
            gameEntityId = -1;
            controllerEntity = new Dictionary<int, int>();
        }

        //public void StartTurn()
        //{
        //    if (CurrentTurn % 2 == 1)
        //    {
        //        BgCombatStarted = false;
        //    }
        //}

        public void GameEntity(GameEntity entity)
        {
            if (CurrentEntities.ContainsKey(entity.Id))
            {
                Logger.Log("error while parsing GameEntity, playerEntity already present in memory", "" + entity.Id);
                return;
            }
            var newTags = new List<Tag>();
            var fullEntity = new FullEntity { Id = entity.Id, Tags = newTags, TimeStamp = entity.TimeStamp };
            CurrentEntities.Add(entity.Id, fullEntity);
            gameEntityId = entity.Id;
        }

        public FullEntity GetGameEntity()
        {
            if (CurrentEntities.ContainsKey(gameEntityId))
            {
                return CurrentEntities[gameEntityId];
            }
            return null;
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

        public GameStateReport BuildGameStateReport(StateFacade helper)
        {
            // Cna happen when joining a BG game as spectate
            if (helper.LocalPlayer == null)
            {
                return null;
            }
            return new GameStateReport
            {
                LocalPlayer = PlayerReport.BuildPlayerReport(this, helper.LocalPlayer.Id),
                OpponentReport = PlayerReport.BuildPlayerReport(this, helper.OpponentPlayer.Id),
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
            controllerEntity.Add(entity.PlayerId, entity.Id);
        }

        public FullEntity GetController(int controllerId)
        {
            return CurrentEntities[controllerEntity[controllerId]];
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
            // We need to do a copy because this otherwise we could mutate the entity from the log parser
            // Why is this a problem again? Is there a disconnect between the time the entity is
            // modified in the logs parser and when it's modified here?
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
                .Where((e) => e.GetEffectiveController() == entity.GetEffectiveController())
                .First()
                .CardId;
        }

        public FullEntity GetPlayerHeroEntity(int entityId)
        {
            var entity = CurrentEntities[entityId];
            // No cardID is assigned to the player in Mercenaries, since there is no hero
            if (ParserState.IsMercenaries() && entity != null)
            {
                return entity;
            }

            if (entity.CardId != null)
            {
                return entity;
            }
            var heroesForController = CurrentEntities.Values
                .Where((e) => e.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where((e) => e.GetEffectiveController() == entity.GetEffectiveController())
                .Where((e) => e.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                // If there are several, we take the most recent one
                .OrderByDescending((e) => e.TimeStamp)
                .ToList();
            // Happens if the remaining hero is dead for instance
            if (heroesForController.Count() == 0)
            {
                heroesForController = CurrentEntities.Values
                .Where((e) => e.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where((e) => e.GetEffectiveController() == entity.GetEffectiveController())
                // If there are several, we take the oldest one (as the most recent can be a hero card in hand)
                .OrderBy((e) => e.TimeStamp)
                .ToList();
            }
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

        public void TagChange(TagChange tagChange, string defChange)
        {
            if (!CurrentEntities.ContainsKey(tagChange.Entity))
            {
                return;
            }
            var existingTag = CurrentEntities[tagChange.Entity].Tags.Find((tag) => tag.Name == tagChange.Name);
            if (existingTag == null)
            {
                existingTag = new Tag { Name = tagChange.Name };
                CurrentEntities[tagChange.Entity].Tags.Add(existingTag);
            }
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
            int entityId;
            EntityNames.TryGetValue(data, out entityId);
            if (entityId != 0)
            {
                // Now find the player this entity is attached to
                int playerId = CurrentEntities.Values
                    .Where(e => e.Tags.Find(x => (x.Name == (int)GameTag.HERO_ENTITY && x.Value == entityId)) != null)
                    .Select(e => e.Id)
                    .FirstOrDefault();
                if (playerId == 0)
                {
                    var entity = CurrentEntities.Values.Where(x => x.Id == entityId).FirstOrDefault();
                    var entityControllerId = entity.GetEffectiveController();
                    //Console.WriteLine("Controller ID = " + entityControllerId);
                    playerId = ParserState.getPlayers().Find(x => x.PlayerId == entityControllerId).Id;
                }
                return playerId;
            }
            return 0;
        }

        public int GetActivePlayerId()
        {
            var activePlayer = CurrentEntities.Values
                    .Where(e => e.Tags.Find(x => (x.Name == (int)GameTag.CURRENT_PLAYER && x.Value == 1)) != null)
                    .FirstOrDefault();
            var activePlayerEntityId = activePlayer?.Id;
            var activePlayerEntity = ParserState.CurrentGame.FilterGameData(typeof(PlayerEntity))
                .Select(player => (PlayerEntity)player)
                .FirstOrDefault(player => player.Id == activePlayerEntityId);
            return activePlayerEntity?.PlayerId ?? -1;
        }

        public void OnCardPlayed(int entityId, int? targetEntityId = null)
        {
            LastCardPlayedEntityId = entityId;
            if (CurrentEntities.ContainsKey(entityId))
            {
                // Add it to each owner
                var playedEntity = CurrentEntities[entityId];
                var cardsForPlayer = !CardsPlayedByPlayerEntityId.ContainsKey(playedEntity.GetController()) ? null : CardsPlayedByPlayerEntityId[playedEntity.GetController()];
                if (cardsForPlayer == null)
                {
                    cardsForPlayer = new Dictionary<int, List<int>>();
                    CardsPlayedByPlayerEntityId[playedEntity.GetController()] = cardsForPlayer;
                }
                var currentTurn = GetGameEntity().GetTag(GameTag.TURN);
                var cardsForTurn = !cardsForPlayer.ContainsKey(currentTurn) ? null : cardsForPlayer[currentTurn];
                if (cardsForTurn == null)
                {
                    cardsForTurn = new List<int>();
                    cardsForPlayer[currentTurn] = cardsForTurn;
                }
                cardsForTurn.Add(entityId);

                // Plagiarize
                var plagiarizes = CurrentEntities.Values
                    //.Where(e => e.CardId == CardIds.Collectible.Rogue.Plagiarize) // We don't know it's plagiarize, so add it to all secrets
                    .Where(e => e.GetTag(GameTag.ZONE) == (int)Zone.SECRET)
                    .ToList();
                if (plagiarizes.Count > 0)
                {
                    plagiarizes.ForEach(plagia => plagia.KnownEntityIds.Add(entityId));
                }

                // Potion of Illusion
                if (playedEntity.CardId == CardIds.PotionOfIllusion)
                {
                    this.EntityIdsOnBoardWhenPlayingPotionOfIllusion = CurrentEntities.Values
                        .Where(entity => (entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY))
                        .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.MINION)
                        .OrderBy(entity => entity.GetTag(GameTag.ZONE_POSITION))
                        .GroupBy(entity => entity.GetEffectiveController())
                        .ToDictionary(g => g.Key, g => g.ToList());
                }
                else
                {
                    this.EntityIdsOnBoardWhenPlayingPotionOfIllusion = null;
                }

                // Lady Liadrin
                // The spells are created in random order, so we can't flag them
                //if (playedEntity.GetCardType() == (int)CardType.SPELL)
                //{
                //    if (targetEntityId != null && CurrentEntities.ContainsKey((int)targetEntityId))
                //    {
                //        var targetEntity = CurrentEntities[(int)targetEntityId];
                //        var playedControllerId = playedEntity.GetController();
                //        var targetControllerId = targetEntity.GetController();
                //        if (playedControllerId == targetControllerId)
                //        {
                //            var spellsPlayedOnMinions = !SpellsPlayedByPlayerOnFriendlyEntityIds.ContainsKey(playedControllerId) 
                //                ? null 
                //                : SpellsPlayedByPlayerOnFriendlyEntityIds[playedControllerId];
                //            if (spellsPlayedOnMinions == null)
                //            {
                //                spellsPlayedOnMinions = new List<int>();
                //                SpellsPlayedByPlayerOnFriendlyEntityIds[playedControllerId] = spellsPlayedOnMinions;
                //            }
                //            spellsPlayedOnMinions.Add(entityId);
                //        }
                //    }
                //}
                //if (playedEntity.CardId == CardIds.Collectible.Paladin.LadyLiadrin)
                //{
                //    playedEntity.KnownEntityIds = SpellsPlayedByPlayerOnFriendlyEntityIds[playedEntity.GetController()];
                //}

            }
        }

        public void OnCardDrawn(int entityId)
        {
            LastCardDrawnEntityId = entityId;
        }

        public void OnNewTurn()
        {
            if (CurrentTurn % 2 == 1)
            {
                BgsCurrentBattleOpponent = null;
            }
        }

        public void ClearPlagiarize()
        {
            // If there are secrets that work over several turns, this won't work
            var plagiarizes = CurrentEntities.Values
                    //.Where(e => e.CardId == CardIds.Collectible.Rogue.Plagiarize)
                    .Where(e => e.GetTag(GameTag.ZONE) == (int)Zone.SECRET)
                    .ToList();
            plagiarizes.ForEach(plagia => plagia.KnownEntityIds.Clear());
        }

        internal List<FullEntity> FindEnchantmentsAttachedTo(int entity)
        {
            if (!CurrentEntities.ContainsKey(entity))
            {
                return new List<FullEntity>();
            }
            return CurrentEntities.Values.Where(e => e.GetTag(GameTag.ATTACHED) == entity).ToList();
        }
    }
}