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
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public BattlegroundsOpponentRevealedParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return false;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return (ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS
                    || ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS_FRIENDLY)
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
            if (ParserState.OpponentPlayer?.PlayerId != fullEntity.GetTag(GameTag.CONTROLLER))
            {
                return null;
            }
            var cardId = fullEntity.CardId;
            var result = new List<GameEventProvider>();
            result.Add(GameEventProvider.Create(
                fullEntity.TimeStamp,
                "BATTLEGROUNDS_OPPONENT_REVEALED",
                () =>
                {
                    if (ParserState.CurrentGame.GameType != (int)GameType.GT_BATTLEGROUNDS
                        && ParserState.CurrentGame.GameType != (int)GameType.GT_BATTLEGROUNDS_FRIENDLY)
                    {
                        return null;
                    }
                    return new GameEvent
                    {
                        Type = "BATTLEGROUNDS_OPPONENT_REVEALED",
                        Value = new
                        {
                            CardId = cardId
                        }
                    };
                },
                false,
                node.CreationLogLine)
            );
            if (fullEntity.GetTag(GameTag.PLAYER_ID) == GameState.NextBgsOpponentPlayerId)
            {
                result.Add(GameEventProvider.Create(
                        fullEntity.TimeStamp,
                        "BATTLEGROUNDS_NEXT_OPPONENT",
                        () =>
                        {
                            if (ParserState.CurrentGame.GameType != (int)GameType.GT_BATTLEGROUNDS
                                && ParserState.CurrentGame.GameType != (int)GameType.GT_BATTLEGROUNDS_FRIENDLY)
                            {
                                return null;
                            }
                            return new GameEvent
                            {
                                Type = "BATTLEGROUNDS_NEXT_OPPONENT",
                                Value = new
                                {
                                    CardId = cardId,
                                }
                            };
                        },
                        true,
                        node.CreationLogLine));
                GameState.NextBgsOpponentPlayerId = -1;
            }
            return result;
        }
    }
}
