#region
using System;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using HearthstoneReplays.Parser;
using HearthstoneReplays.Events.Parsers;
#endregion

namespace HearthstoneReplays.Events
{
    public class EventQueueHandler
    {
        public List<GameEventProvider> eventQueue;
        private Timer timer;

        private bool waitingForMetaData;

        private readonly Object listLock = new object();
        private StateFacade Helper;

        public EventQueueHandler(StateFacade helper)
        {
            Helper = helper;
            eventQueue = new List<GameEventProvider>();
            timer = new Timer(100);
            timer.Elapsed += ProcessGameEventQueue;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        public void Reset(StateFacade helper)
        {
            this.Helper = helper;
        }

        public void EnqueueGameEvent(List<GameEventProvider> providers)
        {
            //Logger.Log("Enqueuing game event", "");
            if (providers == null || providers.Count == 0)
            {
                return;
            }

            // Filter null providers without creating a new list if possible
            int nullCount = 0;
            for (int i = 0; i < providers.Count; i++)
            {
                if (providers[i] == null)
                {
                    nullCount++;
                }
            }

            // Remove outstanding events - check before filtering to avoid unnecessary work
            bool hasCreateGame = false;
            if (eventQueue.Count > 0)
            {
                for (int i = 0; i < providers.Count; i++)
                {
                    var provider = providers[i];
                    if (provider != null && provider.CreationLogLine?.Contains("CREATE_GAME") == true)
                    {
                        hasCreateGame = true;
                        break;
                    }
                }
                if (hasCreateGame)
                {
                    ClearQueue();
                }
            }

            // Filter nulls and collect duplicate predicates in a single pass
            List<GameEventProvider> validProviders = nullCount > 0 
                ? new List<GameEventProvider>(providers.Count - nullCount) 
                : providers;
            List<Func<GameEventProvider, bool>> duplicatePredicates = null;

            for (int i = 0; i < providers.Count; i++)
            {
                var provider = providers[i];
                if (provider != null)
                {
                    if (nullCount > 0)
                    {
                        validProviders.Add(provider);
                    }
                    if (provider.isDuplicatePredicate != null)
                    {
                        if (duplicatePredicates == null)
                        {
                            duplicatePredicates = new List<Func<GameEventProvider, bool>>();
                        }
                        duplicatePredicates.Add(provider.isDuplicatePredicate);
                    }
                }
            }

            if (validProviders == null)
            {
                validProviders = providers;
            }

            lock (listLock)
            {
                // Remove duplicate events using collected predicates
                if (duplicatePredicates != null && duplicatePredicates.Count > 0 && eventQueue.Count > 0)
                {
                    // Use a more efficient removal approach - iterate backwards to allow safe removal
                    for (int i = eventQueue.Count - 1; i >= 0; i--)
                    {
                        var queued = eventQueue[i];
                        if (queued == null)
                        {
                            eventQueue.RemoveAt(i);
                            continue;
                        }

                        // Check against all predicates
                        for (int j = 0; j < duplicatePredicates.Count; j++)
                        {
                            if (duplicatePredicates[j](queued))
                            {
                                eventQueue.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }

                // Use binary search insertion instead of full sort for better performance
                // This is O(log n) per insertion instead of O(n log n) for full sort
                for (int i = 0; i < validProviders.Count; i++)
                {
                    var provider = validProviders[i];
                    InsertInSortedOrder(provider);
                }
            }
        }

        // Insert a provider in sorted order using binary search for O(log n) insertion
        private void InsertInSortedOrder(GameEventProvider provider)
        {
            if (eventQueue.Count == 0)
            {
                eventQueue.Add(provider);
                return;
            }

            // Binary search for insertion point
            int left = 0;
            int right = eventQueue.Count - 1;
            int insertIndex = eventQueue.Count;

            // Cache timestamp and index to avoid repeated property access
            DateTime providerTimestamp = provider.Timestamp;
            int providerIndex = provider.Index;

            while (left <= right)
            {
                int mid = (left + right) / 2;
                var midProvider = eventQueue[mid];
                
                // Cache mid provider's timestamp and index
                DateTime midTimestamp = midProvider.Timestamp;
                int timestampComparison = providerTimestamp.CompareTo(midTimestamp);
                
                if (timestampComparison < 0)
                {
                    insertIndex = mid;
                    right = mid - 1;
                }
                else if (timestampComparison > 0)
                {
                    left = mid + 1;
                }
                else
                {
                    // Same timestamp, compare by Index
                    int midIndex = midProvider.Index;
                    int indexComparison = providerIndex.CompareTo(midIndex);
                    if (indexComparison < 0)
                    {
                        insertIndex = mid;
                        right = mid - 1;
                    }
                    else
                    {
                        left = mid + 1;
                    }
                }
            }

            eventQueue.Insert(insertIndex, provider);
        }

        public void ClearQueue()
        {
            // Logger.Log("Clearing queue", "");
            lock (listLock)
            {
                //Logger.Log("Acquierd list lock in clearqueue", "");
                // We process all pending events. It can happen (typically with the Hearthstone spell) that 
                // not all events receive their animation log, so we do it just to be sure there aren't any 
                // left overs
                eventQueue.ForEach(provider => ProcessGameEvent(provider));
                eventQueue.Clear();
            }
        }

        private bool processing;
        private void ProcessGameEventQueue(Object source, ElapsedEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                if (processing)
                {
                    return;
                }

                while (IsEventToProcess())
                {
                    processing = true;
                    //Logger.Log("[csharp] Event to process", "");
                    GameEventProvider provider = null;

                    try
                    {
                        lock (listLock)
                        {
                            if (eventQueue.Count == 0)
                            {
                                //Logger.Log("No event", "");
                                processing = false;
                                return;
                            }

                            if (waitingForMetaData)
                            {
                                Logger.Log("Waiting for metadata", "");
                                processing = false;
                                return;
                            }

                            provider = eventQueue[0];
                            eventQueue.RemoveAt(0);
                        }
                        if (provider.NeedMetaData)
                        {
                            waitingForMetaData = true;
                            // Wait until we have all the necessary data
                            while (!Helper.HasMetaData())
                            {
                                Logger.Log($"Awaiting metadata {Helper.GsState.CurrentGame.FormatType}, {Helper.GsState.CurrentGame.GameType}," +
                                    $"{Helper.LocalPlayer}", provider.EventName);
                                await Task.Delay(100);
                            }
                            waitingForMetaData = false;
                        }
                        if (provider.WaitFor != 0)
                        {
                            await Task.Delay(provider.WaitFor);
                        }

                        lock (listLock)
                        {
                            ProcessGameEvent(provider);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("Exception while parsing event " + provider?.EventName, provider?.CreationLogLine);
                        Logger.Log("Exception while parsing event queue " + ex.Message, ex.StackTrace);
                        processing = false;
                        return;
                    }
                }
                processing = false;
            });
        }

        private void ProcessGameEvent(GameEventProvider provider)
        {
            if (provider.SupplyGameEvent == null && provider.GameEvent == null)
            {
                //Logger.Log("No game event", "");
                return;
            }
            var gameEvent = provider.GameEvent != null ? provider.GameEvent : provider.SupplyGameEvent();
            // This can happen because there are some conditions that are only resolved when we 
            // have the full meta data, like dungeon run step
            if (gameEvent != null)
            {
                //Logger.Log("Handling game event", gameEvent.Type);
                GameEventHandler.Handle(gameEvent, false);
            }
        }

        private bool IsEventToProcess()
        {
            try
            {
                var isEvent = false;
                lock (listLock)
                {
                    // We leave some time so that events parsed later can be processed sooner (typiecally the case 
                    // for end-of-block events vs start-of-block events, like tag changes)
                    // Update: the 100ms ticks should be enough to play this role
                    isEvent = eventQueue.Count > 0;
                }
                //Logger.Log("Is event to process? " + isEvent + " // " + eventQueue.Count, 
                //    eventQueue.Count > 0 ? "" + DateTime.Now.Subtract(eventQueue.First().Timestamp).TotalMilliseconds : "");
                return isEvent;
            }
            catch (Exception ex)
            {
                Logger.Log(ex.StackTrace, "" + eventQueue.Count);
                Logger.Log("Exception while trying to determine event to process", ex.Message);
                return false;
            }
        }
    }
}