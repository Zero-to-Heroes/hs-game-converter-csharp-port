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
        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(Game);
        }

        public bool AppliesOnCloseNode(Node node)
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
                    Type = "NEW_GAME"
                },
                false,
                node,
                false,
                false,
                true) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
