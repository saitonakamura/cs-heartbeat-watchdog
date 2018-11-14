using System;
using System.Threading;
using System.Threading.Tasks;

namespace saitonakamura.Watchdog
{
    public class HeartbeatWatchdog : IDisposable
    {
        public event EventHandler<EventArgs> HeartbeatStopped = delegate { };

        private readonly int _alertMs;
        private readonly string _name;
        private readonly Action<string> _infoLogger = delegate { };
        private readonly Action<string> _debugLogger = delegate { };
        private readonly Action<Exception, string> _errorLogger = delegate { };

        private CancellationTokenSource _cancelSource;
        private CancellationTokenSource _innerCancellationTokenSource;

        // Thread shared var, be careful
        private long _lastBeatTotalMs;

        public HeartbeatWatchdog(int milliseconds, 
            string name = "",
            Action<string> infoLogger = null, 
            Action<string> debugLogger = null,
            Action<Exception, string> errorLogger = null)
        {
            _alertMs = milliseconds;
            _name = name;

            if (infoLogger != null)
                _infoLogger = infoLogger;
            if (debugLogger != null)
                _debugLogger = debugLogger;
            if (errorLogger != null)
                _errorLogger = errorLogger;
        }

        public HeartbeatWatchdog(TimeSpan alertSpan,
            string name = null,
            Action<string> infoLogger = null,
            Action<string> debugLogger = null,
            Action<Exception, string> errorLogger = null)

            : this((int)alertSpan.TotalMilliseconds, name, infoLogger, debugLogger, errorLogger)
        {
        }

        public static HeartbeatWatchdog StartNew(TimeSpan alertSpan,
            string name = null,
            Action<string> infoLogger = null,
            Action<string> debugLogger = null,
            Action<Exception, string> errorLogger = null)
        {
            return new HeartbeatWatchdog(alertSpan, name, infoLogger, debugLogger, errorLogger);
        }

        public static HeartbeatWatchdog StartNew(int alertSpan,
            string name = null,
            Action<string> infoLogger = null,
            Action<string> debugLogger = null,
            Action<Exception, string> errorLogger = null)
        {
            return new HeartbeatWatchdog(alertSpan, name, infoLogger, debugLogger, errorLogger);
        }

        public void Start(CancellationToken outerCancellationToken = default(CancellationToken))
        {
            _innerCancellationTokenSource = new CancellationTokenSource();

            _cancelSource = CancellationTokenSource.CreateLinkedTokenSource(
                _innerCancellationTokenSource.Token,
                outerCancellationToken);

            _infoLogger($"HeartbeatWatchdog {_name} has been started");

            Beat();
            Task.Run(async () => await RunMonitor(_cancelSource.Token), _cancelSource.Token);
        }

//        public void Restart(CancellationToken outerCancellationToken = default(CancellationToken))
//        {
//            _infoLogger($"HeartbeatWatchdog {_name} about to restart");
//            _cancelSource.Cancel();
//
//            _cancelSource.Dispose();
//            _innerCancellationTokenSource.Dispose();
//
//            Start(outerCancellationToken);
//        }

        public void Beat()
        {
            Volatile.Write(ref _lastBeatTotalMs, DateTimeToTotalMs(DateTime.Now));
            _debugLogger($"HeartbeatWatchdog {_name} got a beat");
        }

        public void Dispose()
        {
            _cancelSource.Dispose();
            _innerCancellationTokenSource.Dispose();
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