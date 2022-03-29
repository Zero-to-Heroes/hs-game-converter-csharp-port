using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HearthstoneReplays.Parser;
using System.Text.RegularExpressions;

namespace HearthstoneReplays.Events
{
    public class GameEventProvider
    {
        public Func<GameEvent> SupplyGameEvent { get; set; }
        public Func<GameEventProvider, bool> isDuplicatePredicate { get; set; }
        public DateTime Timestamp { get; set; }
        public bool NeedMetaData { get; set; }
        public string EventName { get; set; }
        public string CreationLogLine { get; set; }
        public int Index { get; set; }
        public GameEvent GameEvent { get; private set; }
        public object Props { get; set; }

        public static GameEventProvider Create(
            DateTime originalTimestamp,
            string eventName,
            Func<GameEvent> eventProvider,
            bool needMetaData,
            Node node,
            object props = null)
        {
            return Create(originalTimestamp, eventName, eventProvider, (a) => false, needMetaData, node, props);
        }

        public static GameEventProvider Create(
            DateTime originalTimestamp,
            string eventName,
            Func<GameEvent> eventProvider,
            Func<GameEventProvider, bool> isDuplicatePredicate,
            bool needMetaData,
            Node node,
            object props = null)
        {
            string creationLogLine = node.CreationLogLine;
            int index = node.Index;
            var result = new GameEventProvider
            {
                Timestamp = originalTimestamp,
                Index = index,
                EventName = eventName,
                SupplyGameEvent = eventProvider,
                isDuplicatePredicate = isDuplicatePredicate,
                NeedMetaData = needMetaData,
                CreationLogLine = creationLogLine?.Trim(),
                Props = props,
            };
            return result;
        }
    }
}
