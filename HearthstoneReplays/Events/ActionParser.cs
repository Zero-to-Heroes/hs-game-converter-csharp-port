using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HearthstoneReplays.Parser;

namespace HearthstoneReplays.Events
{
    interface ActionParser
    {
        bool AppliesOnNewNode(Node node);
        bool AppliesOnCloseNode(Node node);
        List<GameEventProvider> CreateGameEventProviderFromNew(Node node);
        List<GameEventProvider> CreateGameEventProviderFromClose(Node node);
    }
}
