using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HearthstoneReplays.Events
{
    public class GameEventProvider
    {
        public Func<GameEvent> SupplyGameEvent { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public bool NeedMetaData { get; set; }
    }
}
