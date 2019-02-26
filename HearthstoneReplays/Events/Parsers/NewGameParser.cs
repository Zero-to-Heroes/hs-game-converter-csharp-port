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

        public GameEventProvider CreateGameEventProviderFromNew(Node node)
        {
            return new GameEventProvider
            {
                Timestamp = DateTimeOffset.Parse((node.Object as Game).TimeStamp),
                SupplyGameEvent = () => new GameEvent
                {
                    Type = "NEW_GAME"
                },
                NeedMetaData = false
            };
        }

        public GameEventProvider CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
