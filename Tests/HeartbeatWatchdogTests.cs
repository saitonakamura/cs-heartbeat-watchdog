using System;
using System.Threading;
using Moq;
using saitonakamura.Watchdog;
using Xunit;

namespace Tests
{
    public class StartTests
    {
        [Fact]
        public void Given1SecondAndStart_FiresEventBeforeSecondAndABit()
        {
            var handlerMock = new Mock<ITestHandler>();

            var watchdog = new HeartbeatWatchdog(TestTimespans.Second);
            watchdog.HeartbeatStopped += handlerMock.Object.Handle;

            watchdog.Start();
            Thread.Sleep(TestTimespans.SecondAndABit);

            handlerMock.Verify(x => x.Handle(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.AtLeastOnce);
        }

        [Fact]
        public void Given1SecondAndNoStart_DoesntFireEvent()
        {
            var handlerMock = new Mock<ITestHandler>();

            var watchdog = new HeartbeatWatchdog(TestTimespans.Second);
            watchdog.HeartbeatStopped += handlerMock.Object.Handle;

            Thread.Sleep(TestTimespans.SecondAndABit);

            handlerMock.Verify(x => x.Handle(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.Never);
        }
    }

    public class StartNewTests
    {
        [Fact]
        public void Given1SecondAndStartNew_FiresEventBeforeSecondAndABit()
        {
            var handlerMock = new Mock<ITestHandler>();

            HeartbeatWatchdog.StartNew(TestTimespans.Second, handlerMock.Object.Handle);

            Thread.Sleep(TestTimespans.SecondAndABit);

            handlerMock.Verify(x => x.Handle(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.AtLeastOnce);
        }
    }

    public class CancelTests
    {
        [Fact]
        public void Given1SecondAndStartAndImmediateCancel_DoesntFireEvent()
        {
            var handlerMock = new Mock<ITestHandler>();
            var cancellationTokenSource = new CancellationTokenSource();

            var watchdog = new HeartbeatWatchdog(TestTimespans.Second);
            watchdog.HeartbeatStopped += handlerMock.Object.Handle;
            watchdog.Start(cancellationTokenSource.Token);

            cancellationTokenSource.Cancel();

            Thread.Sleep(TestTimespans.SecondAndABit);

            handlerMock.Verify(x => x.Handle(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.Never);
        }
    }

    public class CoupleBeatsTests
    {
        [Fact]
        public void GivenTwoHalfSecondBeats_FiresEvent()
        {
            var handlerMock = new Mock<ITestHandler>();

            var watchdog = HeartbeatWatchdog.StartNew(TestTimespans.Second, handlerMock.Object.Handle);

            Thread.Sleep(TestTimespans.HalfSecond);
            handlerMock.Verify(x => x.Handle(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.Never);
            watchdog.Beat();

            Thread.Sleep(TestTimespans.HalfSecond);
            handlerMock.Verify(x => x.Handle(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.Never);
            watchdog.Beat();

            Thread.Sleep(TestTimespans.TwoSecondAndABit);
            handlerMock.Verify(x => x.Handle(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.AtLeastOnce);
        }
    }

    public class BeatWithExternalNowTests
    {
        [Fact]
        public void GivenExternalNow_FiresEvent()
        {
            var handlerMock = new Mock<ITestHandler>();

            var watchdog =  new HeartbeatWatchdog(TestTimespans.Second);
            watchdog.HeartbeatStopped += handlerMock.Object.Handle;
            watchdog.Start();

            Thread.Sleep(TestTimespans.HalfSecond);
            watchdog.Beat(DateTime.Now);

            Thread.Sleep(TestTimespans.TwoSecondAndABit);

            handlerMock.Verify(x => x.Handle(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.AtLeastOnce);
        }

        [Fact]
        public void GivenExternalNowInTheFuture_DoesntFireEvent()
        {
            var handlerMock = new Mock<ITestHandler>();

            var watchdog = new HeartbeatWatchdog(TestTimespans.Second);
            watchdog.HeartbeatStopped += handlerMock.Object.Handle;
            watchdog.Start();

            Thread.Sleep(TestTimespans.HalfSecond);
            watchdog.Beat(DateTime.Now.AddSeconds(3));

            Thread.Sleep(TestTimespans.TwoSecondAndABit);

            handlerMock.Verify(x => x.Handle(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.Never);
        }

        [Fact]
        public void GivenExternalNowInThePast_FiresEvent()
        {
            var handlerMock = new Mock<ITestHandler>();

            var watchdog = new HeartbeatWatchdog(TestTimespans.Second);
            watchdog.HeartbeatStopped += handlerMock.Object.Handle;
            watchdog.Start();

            Thread.Sleep(TestTimespans.HalfSecond);
            watchdog.Beat(DateTime.Now.AddSeconds(-3));

            Thread.Sleep(TestTimespans.SecondAndABit);

            handlerMock.Verify(x => x.Handle(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.AtLeastOnce);
        }
    }

    public class IntIntervalsTests
    {
        // ReSharper disable once ConvertToConstant.Local
        private readonly int _second = 1000;
        // ReSharper disable once ConvertToConstant.Local
        private readonly int _secondAndABit = 1200;

        [Fact]
        public void Given1SecondAndStartNew_FiresEventBeforeSecondAndABit()
        {
            var handlerMock = new Mock<ITestHandler>();

            HeartbeatWatchdog.StartNew(_second, handlerMock.Object.Handle);

            Thread.Sleep(_secondAndABit);

            handlerMock.Verify(x => x.Handle(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.AtLeastOnce);
        }
    }

    public class LoggerTests
    {
        [Fact]
        public void GivenInfoLogger_LogsStart()
        {
            var infoLoggerMock = new Mock<ILogger>();

            HeartbeatWatchdog.StartNew(alertSpan: TestTimespans.Second,
                stopHandler:(_, __) => { },
                infoLogger: infoLoggerMock.Object.Info);

            infoLoggerMock.Verify(x => x.Info("HeartbeatWatchdog  has been started"), Times.Once);
        }
    }

    public static class TestTimespans
    {
        public static TimeSpan HalfSecond = TimeSpan.FromSeconds(0.5);
        public static TimeSpan Second = TimeSpan.FromSeconds(1);
        public static TimeSpan SecondAndABit = TimeSpan.FromSeconds(1).Add(TimeSpan.FromMilliseconds(200));
        public static TimeSpan TwoSecondAndABit = TimeSpan.FromSeconds(2).Add(TimeSpan.FromMilliseconds(200));
    }

    public interface ITestHandler
    {
        void Handle(object sender, EventArgs eventArgs);
    }

    public interface ILogger
    {
        void Info(string obj);
    }
}
