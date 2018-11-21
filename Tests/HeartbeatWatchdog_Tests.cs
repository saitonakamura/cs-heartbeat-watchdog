using System;
using System.Threading;
using Moq;
using saitonakamura.Watchdog;
using Xunit;

namespace Tests
{
    public class HeartbeatWatchdog_Tests
    {
        private readonly TimeSpan _second = TimeSpan.FromSeconds(1);
        private readonly TimeSpan _secondAndABit = TimeSpan.FromSeconds(1).Add(TimeSpan.FromMilliseconds(100));

        [Fact]
        public void Given1SecondAndStart_FiresEventBeforeSecondAndABit()
        {
            var handlerMock = new Mock<ITestHandler>();

            var watchdog = new HeartbeatWatchdog(_second);
            watchdog.HeartbeatStopped  += handlerMock.Object.Handle;

            watchdog.Start();
            Thread.Sleep(_secondAndABit);

            handlerMock.Verify(x => x.Handle(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.AtLeastOnce);
        }

        [Fact]
        public void Given1SecondAndNoStart_DoesntFireEvent()
        {
            var handlerMock = new Mock<ITestHandler>();

            var watchdog = new HeartbeatWatchdog(_second);
            watchdog.HeartbeatStopped += handlerMock.Object.Handle;

            Thread.Sleep(_secondAndABit);

            handlerMock.Verify(x => x.Handle(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.Never);
        }
    }

    public interface ITestHandler
    {
        void Handle(object sender, EventArgs eventArgs);
    }
}
