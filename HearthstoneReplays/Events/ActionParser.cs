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
        bool NeedMetaData();
        bool AppliesOnNewNode(Node node);
        bool AppliesOnCloseNode(Node node);
        GameEventProvider CreateGameEventProviderFromNew(Node node);
        GameEventProvider CreateGameEventProviderFromClose(Node node);
    }
}
