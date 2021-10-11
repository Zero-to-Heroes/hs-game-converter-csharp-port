using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class PassiveBuffParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public PassiveBuffParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.ZONE
                && (node.Object as TagChange).Value == (int)Zone.PLAY;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return node.Type == typeof(ShowEntity)
                && (node.Object as ShowEntity).IsInPlay()
                && (node.Object as ShowEntity).GetTag(GameTag.DUNGEON_PASSIVE_BUFF) == 1
                && (node.Object as ShowEntity).GetTag(GameTag.CARDTYPE) == (int)CardType.SPELL;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetEffectiveController();
            if (ParserState.GetTag(entity.Tags, GameTag.DUNGEON_PASSIVE_BUFF) == 1 
                && ParserState.GetTag(entity.Tags, GameTag.CARDTYPE) != (int)CardType.ENCHANTMENT)
            {
                var gameState = GameEvent.BuildGameState(ParserState, GameState, tagChange, null);
                return new List<GameEventProvider> { GameEventProvider.Create(
                    tagChange.TimeStamp,
                    "PASSIVE_BUFF",
                    GameEvent.CreateProvider(
                        "PASSIVE_BUFF",
                        cardId,
                        controllerId,
                        entity.Id,
                        ParserState,
                        GameState,
                        gameState),
                    true,
                    node) };
            }
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var showEntity = node.Object as ShowEntity;
            var cardId = showEntity.CardId;
            var controllerId = showEntity.GetEffectiveController();
            var gameState = GameEvent.BuildGameState(ParserState, GameState, null, showEntity);
            return new List<GameEventProvider> { GameEventProvider.Create(
                showEntity.TimeStamp,
                "PASSIVE_BUFF",
                GameEvent.CreateProvider(
                    "PASSIVE_BUFF",
                    cardId,
                    controllerId,
                    showEntity.Entity,
                    ParserState,
                    GameState,
                    gameState),
                true,
                node) };
        }
    }
}
