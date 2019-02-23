using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HearthstoneReplays.Events
{
    public class GameEventProvider
    {
        public GameEvent GameEvent { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
