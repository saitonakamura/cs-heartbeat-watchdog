using System;
using System.Threading;
using System.Threading.Tasks;
using saitonakamura.Watchdog;
using Serilog;

namespace Example
{
    public class Program
    {
        public static void Main()
        {
            var isExit = false;

            // I use a logger to illustrate how watchdog works, but don't necessarily need it
            InitLogger();

            var heartbeatWatchdog = new HeartbeatWatchdog(
                alertSpan: TimeSpan.FromSeconds(5),
                // In fact, all of this logger params (name also) are optional
                name: "Example",
                infoLogger: Log.Information,
                debugLogger: Log.Debug,
                errorLogger: Log.Error);

            // Here we subscribe to HeartbeatStopped, we can do anything here
            // Like, log an error, return from function or more likely to restart the task (thread, application)
            heartbeatWatchdog.HeartbeatStopped += (_, __) =>
            {
                // Check the time in the console, it should be ~4-6 seconds after Error
                Log.Warning("Heartbeat monitor determined that task has been stopped");
                isExit = true;
            };

            // Here we run the task that is monitored
            Task.Run(async () =>
            {
                // Don't forget to actually start the watchdog
                // Or use HeartbeatWatchdog.StartNew method (like in the Stopwatch)
                heartbeatWatchdog.Start();

                try
                {
                    var counter = 0;
                    while (true)
                    {
                        // Imitate task failure
                        if (counter > 2)
                        {
                            throw new Exception("AAA");
                        }

                        // This is the most essential part, the actual heartbeat
                        // It need to be happening more often than alert span (twice as much is kinda safe)
                        // So watch out for your delays and timeouts
                        heartbeatWatchdog.Beat();

                        Log.Debug($"{counter}th iteration...");

                        // Imitate some work
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        counter++;
                    }
                }
                catch (Exception e)
                {
                    // Logging an error here to track the time
                    Log.Error(e, "Task failed");
                    throw;
                }
            });

            while (true)
            {
                Thread.Sleep(100);
                if (isExit)
                    break;
            }
        }

        private static void InitLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .CreateLogger();
        }
    }
}
