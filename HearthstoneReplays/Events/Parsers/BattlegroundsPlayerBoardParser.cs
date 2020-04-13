using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Linq;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;
using System;

namespace HearthstoneReplays.Events.Parsers
{
    public class BattlegroundsPlayerBoardParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public BattlegroundsPlayerBoardParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS
                && node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.NEXT_STEP
                && (node.Object as TagChange).Value == (int)Step.MAIN_START_TRIGGERS;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var opponent = ParserState.OpponentPlayer;
            var player = ParserState.LocalPlayer;
            return new List<GameEventProvider> { CreateProvider(node, player), CreateProvider(node, opponent) };
        }

        private GameEventProvider CreateProvider(Node node, Player player)
        {
            var tagChange = node.Object as TagChange;
            var heroes = GameState.CurrentEntities.Values
                .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .ToList();
            var potentialHeroes = GameState.CurrentEntities.Values
                .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .Where(entity => entity.GetTag(GameTag.CONTROLLER) == player.PlayerId)
                // Here we accept to face the ghost
                .Where(entity => entity.CardId != NonCollectible.Neutral.BobsTavernTavernBrawl)
                .ToList();
            var hero = potentialHeroes
                //.Where(entity => entity.CardId != NonCollectible.Neutral.KelthuzadTavernBrawl2)
                .FirstOrDefault();
            //Logger.Log("Trying to handle board", "" + ParserState.CurrentGame.GameType + " // " + hero?.CardId);
            //Logger.Log("Hero " + hero.CardId, hero.Entity);
            var cardId = hero?.CardId;
            if (cardId == NonCollectible.Neutral.KelthuzadTavernBrawl2)
            {
                //Logger.Log("Fighting the ghost", "Trying to assign the previous card id");
                // Take the last one
                var deadHero = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                    .Where(entity => entity.GetTag(GameTag.CONTROLLER) == player.PlayerId)
                    .Where(entity => entity.CardId != NonCollectible.Neutral.BobsTavernTavernBrawl)
                    .Where(entity => entity.CardId != NonCollectible.Neutral.KelthuzadTavernBrawl2)
                    .OrderBy(entity => entity.Id)
                    .LastOrDefault();
                cardId = deadHero?.CardId;
            }
            if (cardId != null)
            {
                // We don't use the game state builder here because we really need the full entities
                var board = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetTag(GameTag.CONTROLLER) == player.PlayerId)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.MINION)
                    .Select(entity => entity.Clone())
                    .ToList();
                var result = board.Select(entity => AddEchantments(GameState.CurrentEntities, entity)).ToList();
                //Logger.Log("board has " + board.Count + " entities", "");
                return GameEventProvider.Create(
                   tagChange.TimeStamp,
                   "BATTLEGROUNDS_PLAYER_BOARD",
                   () => ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS
                        ? new GameEvent
                        {
                            Type = "BATTLEGROUNDS_PLAYER_BOARD",
                            Value = new
                            {
                                Hero = hero,
                                CardId = cardId,
                                Board = result,
                            }
                        }
                        : null,
                   true,
                   node.CreationLogLine);
            }
            //else
            //{
            //    Logger.Log("Invalid hero", hero != null ? hero.CardId : "null hero");
            //}
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }

        private object AddEchantments(Dictionary<int, FullEntity> currentEntities, FullEntity fullEntity)
        {
            var enchantments = currentEntities.Values
                .Where(entity => entity.GetTag(GameTag.ATTACHED) == fullEntity.Id)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .Select(entity => new
                {
                    EntityId = entity.Id,
                    CardId = entity.CardId
                })
                .ToList();
            //var test = currentEntities.Values
            //    .Where(entity => entity.GetTag(GameTag.ATTACHED) > 0)
            //    .Select(entity => entity.CardId)
            //    .ToList();
            //if (test.Count > 0)
            //{
            //    Logger.Log("Eeenchantments", test);
            //}
            dynamic result = new
            {
                CardId = fullEntity.CardId,
                Entity = fullEntity.Entity,
                Id = fullEntity.Id,
                Tags = fullEntity.Tags,
                TimeStamp = fullEntity.TimeStamp,
                Enchantments = enchantments,
            };
            return result;
        }
    }
}
