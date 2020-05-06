using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData.Meta;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HearthstoneReplays.Parser.ReplayData.GameActions;

namespace HearthstoneReplays
{
    public class GameEvent
    {
        public string Type { get; set; }
        public Object Value { get; set; }

        public override string ToString()
        {
            return "GameEvent: " + Type + " (" + Value + ")";
        }

        public static Func<GameEvent> CreateProvider(
            string type,
            string cardId,
            int controllerId,
            int entityId,
            ParserState parserState,
            GameState fullGameState,
            object gameState,
            object additionalProps = null)
        {
            return () => new GameEvent
            {
                Type = type,
                Value = new
                {
                    CardId = cardId,
                    ControllerId = controllerId,
                    LocalPlayer = parserState.LocalPlayer,
                    OpponentPlayer = parserState.OpponentPlayer,
                    EntityId = entityId,
                    // We do it now so that the zone positions should have been resolved, while 
                    // if we compute it when the event is built, there is no guarantee of that
                    // BUT if we compute it now, we have no guarantee that the state matches what 
                    // the state looked like when we built the event, so building it beforehand
                    // is the way to go
                    GameState = gameState, //fullGameState.BuildGameStateReport(),// gameState,
                    AdditionalProps = additionalProps
                }
            };
        }

        // It needs to be built beforehand, as the game state we pass is not immutable
        public static dynamic BuildGameState(ParserState parserState, GameState gameState, TagChange tagChange = null)
        {
            if (parserState == null || parserState.LocalPlayer == null || parserState.OpponentPlayer == null)
            {
                //Logger.Log("Can't build game state", "");
                return new { };
            }

            var result = new
            {
                ActivePlayerId = gameState.GetActivePlayerId(),
                Player = new
                {
                    Hero = GameEvent.BuildHero(gameState, parserState.LocalPlayer.PlayerId, tagChange),
                    Hand = GameEvent.BuildZone(gameState, Zone.HAND, parserState.LocalPlayer.PlayerId, tagChange),
                    Board = GameEvent.BuildBoard(gameState, parserState.LocalPlayer.PlayerId, tagChange),
                    Deck = GameEvent.BuildZone(gameState, Zone.DECK, parserState.LocalPlayer.PlayerId, tagChange),
                },
                Opponent = new
                {
                    Hero = GameEvent.BuildHero(gameState, parserState.OpponentPlayer.PlayerId, tagChange),
                    Hand = GameEvent.BuildZone(gameState, Zone.HAND, parserState.OpponentPlayer.PlayerId, tagChange),
                    Board = GameEvent.BuildBoard(gameState, parserState.OpponentPlayer.PlayerId, tagChange),
                    Deck = GameEvent.BuildZone(gameState, Zone.DECK, parserState.OpponentPlayer.PlayerId, tagChange),
                }
            };
            return result;
        }

        private static object BuildHero(GameState gameState, int playerId, TagChange tagChange = null)
        {
            try
            {
                var hero = gameState.CurrentEntities.Values
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                    .Where(entity => entity.GetTag(GameTag.CONTROLLER) == playerId)
                    .OrderBy(entity => entity.GetTag(GameTag.ZONE_POSITION))
                    .Select(entity => BuildSmallEntity(entity, tagChange))
                    .FirstOrDefault();
                return hero != null ? hero : new
                {

                };
            }
            catch (Exception e)
            {
                Logger.Log("Warning: issue when trying to build hero " + e.Message, e.StackTrace);
                return BuildHero(gameState, playerId);
            }
        }

        private static List<object> BuildZone(GameState gameState, Zone zone, int playerId, TagChange tagChange)
        {
            try
            {
                return gameState.CurrentEntities.Values
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)zone)
                    .Where(entity => entity.GetTag(GameTag.CONTROLLER) == playerId)
                    .OrderBy(entity => entity.GetTag(GameTag.ZONE_POSITION))
                    .Select(entity => BuildSmallEntity(entity, tagChange))
                    .ToList();
            }
            catch (Exception e)
            {
                Logger.Log("Warning: issue when trying to build zone " + e.Message, e.StackTrace);
                return BuildZone(gameState, zone, playerId, tagChange);
            }
        }

        private static List<object> BuildBoard(GameState gameState, int playerId, TagChange tagChange)
        {
            try
            {
                return gameState.CurrentEntities.Values
                    .Where(entity => (entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY && !RemovedFromPlay(entity, tagChange))
                        || PutInPlay(entity, tagChange))
                    .Where(entity => entity.GetTag(GameTag.CONTROLLER) == playerId)
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.MINION)
                    .OrderBy(entity => entity.GetTag(GameTag.ZONE_POSITION))
                    .Select(entity => BuildSmallEntity(entity, tagChange))
                    .ToList();
            }
            catch (Exception e)
            {
                Logger.Log("Warning: issue when trying to build zone " + e.Message, e.StackTrace);
                return BuildBoard(gameState, playerId, tagChange);
            }
        }

        private static bool RemovedFromPlay(FullEntity entity, TagChange tagChange = null)
        {
            if (tagChange == null)
            {
                return false;
            }
            return tagChange.Entity == entity.Entity
                && tagChange.Name == (int)GameTag.ZONE
                && tagChange.Value != (int)Zone.PLAY;
        }

        private static bool PutInPlay(FullEntity entity, TagChange tagChange = null)
        {
            if (tagChange == null)
            {
                return false;
            }
            return tagChange.Entity == entity.Entity
                && tagChange.Name == (int)GameTag.ZONE
                && tagChange.Value == (int)Zone.PLAY;
        }

        private static object BuildSmallEntity(BaseEntity entity, TagChange tagChange)
        {
            string cardId = null;
            if (entity.GetType() == typeof(FullEntity))
            {
                cardId = (entity as FullEntity).CardId;
            }
            return new
            {
                entityId = entity.Id,
                cardId = cardId,
                attack = tagChange?.Name == (int)GameTag.ATK && tagChange?.Entity == entity.Id 
                    ? tagChange.Value 
                    : entity.GetTag(GameTag.ATK),
                health = entity.GetTag(GameTag.HEALTH),
                tags = entity.GetTagsCopy()
            };
        }

        private static object BuildSmallHeroEntity(BaseEntity entity)
        {
            string cardId = null;
            if (entity.GetType() == typeof(FullEntity))
            {
                cardId = (entity as FullEntity).CardId;
            }
            return new
            {
                entityId = entity.Id,
                cardId = cardId,
                attack = entity.GetTag(GameTag.ATK),
                health = entity.GetTag(GameTag.HEALTH),
                tags = entity.GetTagsCopy()
            };
        }
    }
}
