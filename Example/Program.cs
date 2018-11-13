using System;
using System.Threading;
using System.Threading.Tasks;
using HeartbeatWatchdogs;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var heartbeatWatchdog = new HeartbeatWatchdog(
                alertSpan: TimeSpan.FromSeconds(5),
                name: "Example",
                infoLogger: Console.WriteLine,
                debugLogger: Console.WriteLine,
                errorLogger: (e, m) => Console.Error.WriteLine($"{m}: {e}"));

            var isExit = false;

            heartbeatWatchdog.HeartbeatStopped += (_, __) =>
            {
                Console.Error.WriteLine("Task stopped");
                Console.WriteLine("Press enter to exit...");
                Console.ReadLine();
                isExit = true;
            };

            Task.Run(async () =>
            {
                heartbeatWatchdog.Start();

                var counter = 0;
                while (true)
                {
                    if (counter > 2)
                        throw new Exception("AAA");

                    heartbeatWatchdog.Beat();

                    Console.WriteLine($"{counter}th iteration...");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    counter++;
                }
            });

            while (true)
            {
                Thread.Sleep(100);
                if (isExit)
                    break;
            }

            Console.WriteLine("The end");
        }
    }
}
