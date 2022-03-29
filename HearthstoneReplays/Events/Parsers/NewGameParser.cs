using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HearthstoneReplays.Events;
using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;

namespace HearthstoneReplays.Events.Parsers
{
    public class NewGameParser : ActionParser
    {
        private ParserState ParserState { get; set; }

        public NewGameParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
        }
        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            return stateType == StateType.GameState
                && node.Type == typeof(Game);
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return new List<GameEventProvider> { GameEventProvider.Create(
                (node.Object as Game).TimeStamp,
                "NEW_GAME",
                () => new GameEvent
                {
                    Type = "NEW_GAME",
                    Value = new
                    {
                        Spectating = ParserState.Spectating,
                    },
                },
                false,
                node) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
