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
    public class BattlegroundsOpponentRevealedParser : ActionParser
    {
        private static IList<int> EXCLUDED_HERO_CREATOR_DBFIDS = new List<int>()
        {
            63600 // TB_BaconShop_HP_081
        };

        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }
        private StateFacade StateFacade { get; set; }

        public BattlegroundsOpponentRevealedParser(ParserState ParserState, StateFacade stateFacade)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = stateFacade;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            return false;
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            return stateType == StateType.PowerTaskList
                && node.Type == typeof(FullEntity)
                && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.SETASIDE 
                && (node.Object as FullEntity).GetTag(GameTag.CARDTYPE) == (int)CardType.HERO;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var fullEntity = node.Object as FullEntity;
            var cardId = fullEntity.CardId;
            if (cardId == CardIds.BaconphheroHeroicBattlegrounds)
            {
                return null;
            }

            var result = new List<GameEventProvider>();
            result.Add(GameEventProvider.Create(
                fullEntity.TimeStamp,
                "BATTLEGROUNDS_OPPONENT_REVEALED",
                () => BuildGameEvent(node),
                false,
                node)
            );
            if (fullEntity.GetTag(GameTag.PLAYER_ID) == GameState.NextBgsOpponentPlayerId
                && !EXCLUDED_HERO_CREATOR_DBFIDS.Contains(fullEntity.GetTag(GameTag.CREATOR_DBID)))
            {
                result.Add(GameEventProvider.Create(
                        fullEntity.TimeStamp,
                        "BATTLEGROUNDS_NEXT_OPPONENT",
                        () =>
                        {
                            if (!StateFacade.IsBattlegrounds())
                            {
                                return null;
                            }
                            return new GameEvent
                            {
                                Type = "BATTLEGROUNDS_NEXT_OPPONENT",
                                Value = new
                                {
                                    CardId = cardId,
                                    LeaderboardPlace = fullEntity.GetTag(GameTag.PLAYER_LEADERBOARD_PLACE),
                                }
                            };
                        },
                        true,
                        node));
                GameState.BgsHasSentNextOpponent = true;
                GameState.NextBgsOpponentPlayerId = -1;
            }
            return result;
        }

        private GameEvent BuildGameEvent(Node node)
        {
            var fullEntity = node.Object as FullEntity;
            var cardId = fullEntity.CardId;
            if (!StateFacade.IsBattlegrounds())
            {
                return null;
            }

            if (StateFacade.OpponentPlayer?.PlayerId != fullEntity.GetEffectiveController())
            {
                return null;
            }

            return new GameEvent
            {
                Type = "BATTLEGROUNDS_OPPONENT_REVEALED",
                Value = new
                {
                    CardId = cardId,
                    LeaderboardPlace = fullEntity.GetTag(GameTag.PLAYER_LEADERBOARD_PLACE),
                    Health = fullEntity.GetTag(GameTag.HEALTH),
                }
            };
        }
    }
}
