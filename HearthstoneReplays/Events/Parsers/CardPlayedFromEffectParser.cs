﻿using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    // Used to handle spells played by cards like Servant of Yogg-Saron or Nagaling
    public class CardPlayedFromEffectParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }
        private StateFacade StateFacade { get; set; }

        public CardPlayedFromEffectParser(ParserState ParserState, StateFacade facade)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = facade;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            var isPowerPhase = (node.Parent == null
                       || node.Parent.Type != typeof(Parser.ReplayData.GameActions.Action)
                       || (node.Parent.Object as Parser.ReplayData.GameActions.Action).Type == (int)BlockType.POWER);

            TagChange tagChange;
            FullEntity tagChangeEntity = null;
            bool cardPlayed = node.Type == typeof(TagChange)
                && (tagChange = node.Object as TagChange).Name == (int)GameTag.ZONE
                && tagChange.Value == (int)Zone.PLAY
                && ((tagChangeEntity = GameState.CurrentEntities[(node.Object as TagChange).Entity]).GetTag(GameTag.ZONE) == (int)Zone.SETASIDE
                    // For Nagaling
                    || tagChangeEntity.GetZone() == (int)Zone.REMOVEDFROMGAME);
            return stateType == StateType.PowerTaskList
                && isPowerPhase
                && cardPlayed;
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            // Rune of the Archmage playing spells creates them as FULL_ENTITIES in PLAY, not going through a TAG_CHANGE
            var isPowerPhase = (node.Parent == null
                       || node.Parent.Type != typeof(Parser.ReplayData.GameActions.Action)
                       || (node.Parent.Object as Parser.ReplayData.GameActions.Action).Type == (int)BlockType.POWER);

            ShowEntity fullEntity = null;
            bool cardPlayed = node.Type == typeof(ShowEntity)
                && (fullEntity = node.Object as ShowEntity).GetZone() == (int)Zone.PLAY;
            return stateType == StateType.PowerTaskList
                && isPowerPhase
                && cardPlayed;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetEffectiveController();
            if (GameState.CurrentEntities[tagChange.Entity].GetTag(GameTag.CARDTYPE) == (int)CardType.ENCHANTMENT)
            {
                return null;
            }

            var action = node.Parent.Object as Parser.ReplayData.GameActions.Action;
            // Since 23.4, it can happen that these tags are directly at the root, and not below an action
            var targetId = action?.Target ?? 0;
            string targetCardId = targetId > 0 ? GameState.CurrentEntities[targetId].CardId : null;
            var creator = entity.GetTag(GameTag.CREATOR);
            var creatorCardId = creator != -1 && GameState.CurrentEntities.ContainsKey(creator)
                ? GameState.CurrentEntities[creator].CardId
                : null;
            var gameState = GameEvent.BuildGameState(ParserState, StateFacade, GameState, tagChange, null);

            return new List<GameEventProvider> { GameEventProvider.Create(
                    tagChange.TimeStamp,
                    "CARD_PLAYED_BY_EFFECT",
                    GameEvent.CreateProvider(
                        "CARD_PLAYED_BY_EFFECT",
                        cardId,
                        controllerId,
                        entity.Id,
                        StateFacade,
                        gameState,
                        new {
                            TargetEntityId = targetId,
                            TargetCardId = targetCardId,
                            CreatorCardId = creatorCardId,
                        }
                    ),
                    true,
                    node) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var entity = node.Object as ShowEntity;
            var cardId = entity.CardId;
            var controllerId = entity.GetEffectiveController();
            if (cardId.Length == 0 || GameState.CurrentEntities[entity.Entity].GetTag(GameTag.CARDTYPE) == (int)CardType.ENCHANTMENT)
            {
                return null;
            }

            var action = node.Parent.Object as Parser.ReplayData.GameActions.Action;
            // Since 23.4, it can happen that these tags are directly at the root, and not below an action
            var targetId = action?.Target ?? 0;
            string targetCardId = targetId > 0 ? GameState.CurrentEntities[targetId].CardId : null;
            var creator = entity.GetTag(GameTag.CREATOR);
            var creatorCardId = creator != -1 && GameState.CurrentEntities.ContainsKey(creator)
                ? GameState.CurrentEntities[creator].CardId
                : null;
            var gameState = GameEvent.BuildGameState(ParserState, StateFacade, GameState, null, entity);

            return new List<GameEventProvider> { GameEventProvider.Create(
                    entity.TimeStamp,
                    "CARD_PLAYED_BY_EFFECT",
                    GameEvent.CreateProvider(
                        "CARD_PLAYED_BY_EFFECT",
                        cardId,
                        controllerId,
                        entity.Entity,
                        StateFacade,
                        gameState,
                        new {
                            TargetEntityId = targetId,
                            TargetCardId = targetCardId,
                            CreatorCardId = creatorCardId,
                        }
                    ),
                    true,
                    node) };
        }
    }
}
