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
        public bool AnimationReady { get; set; }
        public string CreationLogLine { get; set; }

        public void ReceiveAnimationLog(string data)
        {
            if (CreationLogLine == null)
            {
                Console.WriteLine("ERROR - Missing CreationLogLine for " + SupplyGameEvent);
            }
            if (data.Trim() == CreationLogLine)
            {
                AnimationReady = true;
            }
        }
    }
}
