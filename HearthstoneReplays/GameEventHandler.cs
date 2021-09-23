﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HearthstoneReplays
{
    public class GameEventHandler
    {
        public static Action<GameEvent> EventProvider;
        public static Action<IList<GameEvent>> EventProviderAll;

        private static IList<GameEvent> queuedEvents = new List<GameEvent>();

        //public static void Handle(GameEvent gameEvent) {
        //	EventProvider?.Invoke(gameEvent);
        //}

        public static void Handle(GameEvent gameEvent, bool isDevMode)
        {
            if (isDevMode)
            {
                if (gameEvent != null)
                {
                    queuedEvents.Add(gameEvent);
                }
            }
            else
            {
                //if (queuedEvents.Count > 50 && lastEmitDate)
                // First event we get 
                if (queuedEvents.Count > 0)
                {
                    if (gameEvent != null)
                    {
                        queuedEvents.Add(gameEvent);
                    }
                    Logger.Log("Sending queued events", "");
                    EventProviderAll?.Invoke(queuedEvents);
                    queuedEvents.Clear();
                }
                else
                {
                    EventProvider?.Invoke(gameEvent);
                }
            }
        }
    }
}