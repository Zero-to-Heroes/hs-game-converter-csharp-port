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
        public DateTimeOffset Timestamp { get; set; }
        public bool NeedMetaData { get; set; }
        public bool AnimationReady { get; set; }
        public string CreationLogLine { get; set; }
        public GameEvent GameEvent { get; private set; }

        private Helper helper = new Helper();

        private GameEventProvider()
        {

        }

        public static GameEventProvider Create(
            string originalTimestamp, 
            Func<GameEvent> eventProvider, 
            bool needMetaData,
            string creationLogLine)
        {
            return Create(originalTimestamp, eventProvider, (a) => false, needMetaData, creationLogLine);
        }

        public static GameEventProvider Create(
            string originalTimestamp,
            Func<GameEvent> eventProvider,
            Func<GameEventProvider, bool> isDuplicatePredicate,
            bool needMetaData,
            string creationLogLine)
        {
            return new GameEventProvider
            {
                Timestamp = ParseTimestamp(originalTimestamp),
                SupplyGameEvent = eventProvider,
                isDuplicatePredicate = isDuplicatePredicate,
                NeedMetaData = needMetaData,
                CreationLogLine = creationLogLine
            };
        }

        public void ReceiveAnimationLog(string data, ParserState state)
        {
            // Mark the event as ready to be emitted
            IsEventReady(data, state);
            // And now's the time to compute the event itself
            if (AnimationReady)
            {
                GameEvent = SupplyGameEvent();
            }
        }

        private void IsEventReady(string data, ParserState state) {
        {
            if (CreationLogLine == null)
            {
                Console.WriteLine("ERROR - Missing CreationLogLine for " + SupplyGameEvent);
            }
            data = data.Trim();
            if (data == CreationLogLine)
            {
                AnimationReady = true;
                return;
            }
            // In the case of discarded cards, the position in the zone can change between 
            // the GameState and PowerTaskList logs, so we do a check without taking 
            // the zone position into account
            var dataWithoutZones = Regex.Replace(data, @"zonePos=\d", "");
            var creationLogWithoutZones = Regex.Replace(CreationLogLine, @"zonePos=\d", "");
            if (dataWithoutZones == creationLogWithoutZones)
            {
                AnimationReady = true;
                return;
            }

            // And sometimes the full entity is logged in PTL, while only the entity is logged 
            // in GS
            var ptlMatchForFullEntity = Regexes.EntityRegex.Match(data);
            if (ptlMatchForFullEntity.Success)
            {
                var id = ptlMatchForFullEntity.Groups[1];
                var dataWithOnlyEntityId = Regex.Replace(data, Regexes.EntityRegex.ToString(), "" + id);
                if (dataWithOnlyEntityId == CreationLogLine)
                {
                    AnimationReady = true;
                    return;
                }
            }

            // Sometimes the information doesn't exactly match - one has more details on the entity
            // So here we compared the most basic form of both logs
            var matchShowInGameState = Regexes.ActionShowEntityRegex.Match(CreationLogLine);
            var matchShowInPowerTaskList = Regexes.ActionShowEntityRegex.Match(data);
            if (matchShowInGameState.Success && matchShowInPowerTaskList.Success)
            {
                var gsRawEntity = matchShowInGameState.Groups[1].Value;
                var gsEntity = helper.ParseEntity(gsRawEntity, state);

                var ptlRawEntity = matchShowInGameState.Groups[1].Value;
                var ptlEntity = helper.ParseEntity(ptlRawEntity, state);

                if (gsEntity == ptlEntity)
                {
                    AnimationReady = true;
                    return;
                }
            }
            // Special case for PowerTaskList Updating an entity that was only created in GameState
            var matchCreationInGameState = Regexes.ActionFullEntityCreatingRegex.Match(CreationLogLine);
            var matchUpdateInPowerTaskList = Regexes.ActionFullEntityUpdatingRegex.Match(data);
            if (matchCreationInGameState.Success && matchUpdateInPowerTaskList.Success)
            {
                var gsRawEntity = matchCreationInGameState.Groups[1].Value;
                var gsEntity = helper.ParseEntity(gsRawEntity, state);

                var ptlRawEntity = matchUpdateInPowerTaskList.Groups[1].Value;
                var ptlEntity = helper.ParseEntity(ptlRawEntity, state);

                if (gsEntity == ptlEntity)
                {
                    AnimationReady = true;
                    return;
                }
            }
        }
    }

        private static DateTimeOffset ParseTimestamp(string timestamp)
        {
            if (!string.IsNullOrEmpty(timestamp))
            {
                String[] split = timestamp.Split(':');
                int hours = int.Parse(split[0]);
                if (hours >= 24) 
                {  
                    String newTs = "00:" + split[1] + ":" + split[2];
                    Logger.Log(timestamp, newTs);
                    // We don't need to add a day here, because the computer's clock will also have 
                    // made the time leap to the next day
                    return DateTimeOffset.Parse(newTs);
                }
                return DateTimeOffset.Parse(timestamp);
            }
            return DateTimeOffset.MinValue;
        }
    }
}
