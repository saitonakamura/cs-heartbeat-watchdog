using System;
using System.Threading;
using System.Threading.Tasks;

namespace saitonakamura.Watchdog
{
    public class HeartbeatWatchdog
    {
        public event EventHandler<EventArgs> HeartbeatStopped = delegate { };

        private readonly int _alertMs;
        private readonly string _name;
        private readonly Action<string> _infoLogger = delegate { };
        private readonly Action<string> _debugLogger = delegate { };
        private readonly Action<Exception, string> _errorLogger = delegate { };

        // Thread shared var, be careful
        private long _lastBeatTotalMs;

        public HeartbeatWatchdog(int milliseconds, 
            string name = "",
            EventHandler<EventArgs> stopHandler = null,
            Action<string> infoLogger = null, 
            Action<string> debugLogger = null,
            Action<Exception, string> errorLogger = null)
        {
            _alertMs = milliseconds;
            _name = name;

            if (stopHandler != null)
                HeartbeatStopped += stopHandler;

            if (infoLogger != null)
                _infoLogger = infoLogger;
            if (debugLogger != null)
                _debugLogger = debugLogger;
            if (errorLogger != null)
                _errorLogger = errorLogger;
        }

        public HeartbeatWatchdog(TimeSpan alertSpan,
            string name = null,
            EventHandler<EventArgs> stopHandler = null,
            Action<string> infoLogger = null,
            Action<string> debugLogger = null,
            Action<Exception, string> errorLogger = null)

            : this((int)alertSpan.TotalMilliseconds, name, stopHandler, infoLogger, debugLogger, errorLogger)
        {
        }

        public static HeartbeatWatchdog StartNew(TimeSpan alertSpan,
            EventHandler<EventArgs> stopHandler,
            string name = null,
            CancellationToken outerCancellationToken = default(CancellationToken),
            Action<string> infoLogger = null,
            Action<string> debugLogger = null,
            Action<Exception, string> errorLogger = null)
        {
            var heartbeatWatchdog = new HeartbeatWatchdog(alertSpan, name, stopHandler, infoLogger, debugLogger, errorLogger);
            heartbeatWatchdog.Start(outerCancellationToken);
            return heartbeatWatchdog;
        }

        public static HeartbeatWatchdog StartNew(int alertSpan,
            EventHandler<EventArgs> stopHandler,
            string name = null,
            CancellationToken outerCancellationToken = default(CancellationToken),
            Action<string> infoLogger = null,
            Action<string> debugLogger = null,
            Action<Exception, string> errorLogger = null)
        {
            var heartbeatWatchdog = new HeartbeatWatchdog(alertSpan, name, stopHandler, infoLogger, debugLogger, errorLogger);
            heartbeatWatchdog.Start(outerCancellationToken);
            return heartbeatWatchdog;
        }

        public void Start(CancellationToken cancelToken = default(CancellationToken), DateTime? now = null)
        {
            _infoLogger($"HeartbeatWatchdog {_name} has been started");

            Beat(now);
            Task.Run(async () => await RunMonitor(cancelToken), cancelToken);
        }

        public void Beat(DateTime? now = null)
        {
            Volatile.Write(ref _lastBeatTotalMs, DateTimeToTotalMs(now ?? DateTime.Now));
            _debugLogger($"HeartbeatWatchdog {_name} got a beat");
        }

        private async Task RunMonitor(CancellationToken cancelToken)
        {
            try
            {
                while (true)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    var lastHeartbeatMs = Volatile.Read(ref _lastBeatTotalMs);
                    var msNow = DateTimeToTotalMs(DateTime.Now);
                    var msElapsed = msNow - lastHeartbeatMs;
                    if (msElapsed > _alertMs)
                    {
                        HeartbeatStopped(this, EventArgs.Empty);
                    }

                    await Task.Delay(_alertMs / 2, cancelToken);
                }
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cancelToken)
            {
                _debugLogger($"HeartbeatWatchdog {_name} has been cancelled");
            }
            catch (Exception e)
            {
                _errorLogger(e, $"HeartbeatWatchdog {_name} failed");
            }
        }

        private static long DateTimeToTotalMs(DateTime dateTime)
        {
            return ((DateTimeOffset)dateTime).ToUnixTimeMilliseconds();
        }
    }
}