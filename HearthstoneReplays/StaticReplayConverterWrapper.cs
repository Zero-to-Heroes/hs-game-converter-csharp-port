using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HearthstoneReplays
{
    internal class StaticReplayConverterWrapper
    {
        private static ReplayConverterPlugin _staticPlugin = null;
        private static object _staticLock = new object();

        // Parameterless constructor for electron-edge-js
        public StaticReplayConverterWrapper()
        {

        }

        private static ReplayConverterPlugin GetStaticPlugin()
        {
            lock (_staticLock)
            {
                if (_staticPlugin == null)
                {
                    _staticPlugin = new ReplayConverterPlugin();
                }
                return _staticPlugin;
            }
        }


        // Method to set memory update callback - electron-edge-js compatible signature
        public async Task<object> setGameEventCallback(dynamic input)
        {
            var callbackInput = input as Func<object, Task<object>>;
            Action<object> logHandler = (msg1) =>
            {
                Task.Run(async () =>
                {
                    await callbackInput(msg1);
                });
            };
            if (logHandler != null)
            {
                GetStaticPlugin().setGameEventCallback(logHandler);
            }
            return "GameEvent callback set on static instance ";
        }

        public async Task<object> setLogger(dynamic input)
        {
            var callbackInput = input as Func<object, Task<object>>;
            Action<object, object> logHandler = (msg1, msg2) =>
            {
                Task.Run(async () =>
                {
                    await callbackInput(msg1 + "," + msg2);
                });
            };
            if (logHandler != null)
            {
                GetStaticPlugin().setLogger(logHandler);
            }
            return "Logger set on static instance";
        }

        public async Task<object> initRealtimeLogConversion(dynamic input)
        {
            var tcs = new TaskCompletionSource<object>();
            object result = null;

            GetStaticPlugin()?.initRealtimeLogConversion((object message) =>
            {
                try
                {
                    result = message;
                    // Signal completion
                    tcs.TrySetResult(message);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            // Wait for the callback to complete
            await tcs.Task;

            return result;
        }

        public async Task<object> realtimeLogProcessing(dynamic input)
        {
            string[] logLines = input.logLines;
            var tcs = new TaskCompletionSource<object>();
            object result = null;

            GetStaticPlugin()?.realtimeLogProcessing(logLines, (object message) =>
            {
                try
                {
                    result = message;
                    // Signal completion
                    tcs.TrySetResult(message);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            // Wait for the callback to complete
            await tcs.Task;

            return result;
        }

        public async Task<object> askForGameStateUpdate(dynamic input)
        {
            GetStaticPlugin()?.askForGameStateUpdate();
            return null;
        }
    }
}
