﻿using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Linq;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;
using System;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using HearthstoneReplays.Parser.ReplayData.Meta;
using HearthstoneReplays.Parser.ReplayData;

namespace HearthstoneReplays.Events.Parsers
{
    public class BattlegroundsHeroSelectedParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public BattlegroundsHeroSelectedParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(Choice)
                && ParserState.CurrentChosenEntites != null
                && ParserState.CurrentChosenEntites.PlayerId == ParserState.LocalPlayer.Id;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            // Don't check for BG here, in case of reconnect
            return ParserState.ReconnectionOngoing
                    && node.Type == typeof(FullEntity)
                    && (node.Object as FullEntity).GetTag(GameTag.CARDTYPE) == (int)CardType.HERO
                    && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.PLAY;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var choice = node.Object as Choice;
            var chosenEntity = GameState.CurrentEntities[choice.Entity];
            if (chosenEntity == null || chosenEntity.GetTag(GameTag.CARDTYPE) != (int)CardType.HERO)
            {
                return null;
            }
            if (chosenEntity.GetTag(GameTag.CONTROLLER) != (int)ParserState.LocalPlayer.PlayerId)
            {
                return null;
            }
            // Heroes proposed at the start are in hand, as opposed to heroes discovered by 
            // Lord Barov's hero power
            if (chosenEntity.GetTag(GameTag.ZONE) != (int)Zone.HAND)
            {
                return null;
            }

            //Logger.Log("Choice timestamp", choice.TimeStamp);

            return new List<GameEventProvider> { GameEventProvider.Create(
                choice.TimeStamp,
                "BATTLEGROUNDS_HERO_SELECTED",
                () => {
                    if (ParserState.CurrentGame.GameType != (int)GameType.GT_BATTLEGROUNDS
                        && ParserState.CurrentGame.GameType != (int)GameType.GT_BATTLEGROUNDS_FRIENDLY)
                    {
                        return null;
                    }

                    return new GameEvent
                    {
                        Type = "BATTLEGROUNDS_HERO_SELECTED",
                        Value = new
                        {
                            CardId = chosenEntity.CardId,
                            LocalPlayer = ParserState.LocalPlayer,
                            OpponentPlayer = ParserState.OpponentPlayer,
                        }
                    };
                },
                true,
                node)
            };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var fullEntity = node.Object as FullEntity;
            return new List<GameEventProvider> { GameEventProvider.Create(
                fullEntity.TimeStamp,
                "BATTLEGROUNDS_HERO_SELECTED",
                () => BuildGameEvent(node),
                true,
                node)
            };
        }

        private GameEvent BuildGameEvent(Node node)
        {
            var fullEntity = node.Object as FullEntity;
            if (ParserState.CurrentGame.GameType != (int)GameType.GT_BATTLEGROUNDS
                && ParserState.CurrentGame.GameType != (int)GameType.GT_BATTLEGROUNDS_FRIENDLY)
            {
                return null;
            }

            if (fullEntity.GetTag(GameTag.CONTROLLER) != ParserState.LocalPlayer.PlayerId)
            {
                return null;
            }

            var nextOpponentPlayerId = fullEntity.GetTag(GameTag.NEXT_OPPONENT_PLAYER_ID);
            var heroes = GameState.CurrentEntities.Values
                .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where(entity => entity.GetTag(GameTag.PLAYER_ID) == nextOpponentPlayerId)
                .Where(entity => entity.CardId != NonCollectible.Neutral.BobsTavernTavernBrawl
                    && entity.CardId != NonCollectible.Neutral.KelthuzadTavernBrawl2)
                .ToList();
            var hero = heroes == null || heroes.Count == 0 ? null : heroes[0];
            // Happens in some circumstances, though it's not clear for me which ones. Maybe
            // when the future opponent isn't here yet, or when players take too long to join?
            if (hero == null)
            {
                GameState.NextBgsOpponentPlayerId = nextOpponentPlayerId;
            }

            return new GameEvent
            {
                Type = "BATTLEGROUNDS_HERO_SELECTED",
                Value = new
                {
                    CardId = fullEntity.CardId,
                    LocalPlayer = ParserState.LocalPlayer,
                    OpponentPlayer = ParserState.OpponentPlayer,
                    LeaderboardPlace = fullEntity.GetTag(GameTag.PLAYER_LEADERBOARD_PLACE),
                    Health = fullEntity.GetTag(GameTag.HEALTH),
                    Damage = fullEntity.GetTag(GameTag.DAMAGE),
                    TavernLevel = fullEntity.GetTag(GameTag.PLAYER_TECH_LEVEL),
                    NextOpponentCardId = hero.CardId,
                },
            };
        }
    }
}