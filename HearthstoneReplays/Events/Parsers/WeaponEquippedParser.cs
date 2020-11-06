using HearthstoneReplays.Parser;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class WeaponEquippedParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public WeaponEquippedParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.ZONE
                && (node.Object as TagChange).Value == (int)Zone.PLAY
                && GameState.CurrentEntities.ContainsKey((node.Object as TagChange).Entity)
                && GameState.CurrentEntities[(node.Object as TagChange).Entity].GetTag(GameTag.CARDTYPE) == (int)CardType.WEAPON;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return node.Type == typeof(FullEntity)
                && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.PLAY
                && (node.Object as FullEntity).GetTag(GameTag.CARDTYPE) == (int)CardType.WEAPON
                && !ParserState.ReconnectionOngoing;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState, tagChange, null);
            var creatorEntityId = entity.GetTag(GameTag.CREATOR);
            var creatorEntityCardId = GameState.CurrentEntities.ContainsKey(creatorEntityId)
                ? GameState.CurrentEntities[creatorEntityId].CardId
                : null;
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                "WEAPON_EQUIPPED",
                GameEvent.CreateProvider(
                    "WEAPON_EQUIPPED",
                    cardId,
                    controllerId,
                    entity.Id,
                    ParserState,
                    GameState,
                    gameState,
                    new {
                        CreatorCardId = creatorEntityCardId,
                    }
                ),
                true,
                node) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var fullEntity = node.Object as FullEntity;
            var cardId = fullEntity.CardId;
            var controllerId = fullEntity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState, null, null);
            var creatorEntityId = fullEntity.GetTag(GameTag.CREATOR);
            var creatorEntityCardId = GameState.CurrentEntities.ContainsKey(creatorEntityId) 
                ? GameState.CurrentEntities[creatorEntityId].CardId
                : null;
            return new List<GameEventProvider> { GameEventProvider.Create(
                fullEntity.TimeStamp,
                "WEAPON_EQUIPPED",
                GameEvent.CreateProvider(
                    "WEAPON_EQUIPPED",
                    cardId,
                    controllerId,
                    fullEntity.Id,
                    ParserState,
                    GameState,
                    gameState,
                    new {
                        CreatorCardId = creatorEntityCardId,
                    }
                ),
                true,
                node) };
        }
    }
}
