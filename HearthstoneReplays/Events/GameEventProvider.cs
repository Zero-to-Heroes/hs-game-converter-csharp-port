using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HearthstoneReplays.Parser;

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
            data = data.Trim();
            if (data == CreationLogLine)
            {
                AnimationReady = true;
                return;
            }
            //var matchCreationInGameState = Regexes.ActionFullEntityCreatingRegex.Match(CreationLogLine);
            //var matchUpdateInPowerTaskList = Regexes.ActionFullEntityUpdatingRegex.Match(data);
            //// Special case for PowerTaskList Updating an entity that was only created in GameState
            //if (matchCreationInGameState.Success && matchUpdateInPowerTaskList.Success)
            //{
            //    var creationEntity = matchCreationInGameState.Groups[1].Value;
            //    var updateEntity = matchUpdateInPowerTaskList.Groups[1].Value;
            //    if (creationEntity == updateEntity)
            //    {
            //        AnimationReady = true;
            //        return;
            //    }
            //}
        }
    }
}
