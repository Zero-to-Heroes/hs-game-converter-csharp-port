using HearthstoneReplays.Parser;
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
            return false;
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
                node.CreationLogLine)
            };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
