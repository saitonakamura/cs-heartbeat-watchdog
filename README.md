# HeartbeatWatchdog [![Travis CI master branch status](https://travis-ci.org/saitonakamura/cs-heartbeat-watchdog.svg?branch=master)](https://travis-ci.org/saitonakamura/cs-heartbeat-watchdog)

Lightweight, thread-safe Watchdog with manual beats

## Installation

```
dotnet add package saitonakamura.Watchdog
```

## Usage

```csharp
var watchdog = new HeartbeatWatchdog(alertSpan: TimeSpan.FromSeconds(5));

watchdog.HeartbeatStopped += (sender, eventArgs) => {
  // Heartbeat has been stopped, means task either failed, completed or frozen
  // Here you do anything, log the error, restart the task, you name it
};

Task.Run(() => {
    // Don't forget to actually start the watchdog
    // Or use HeartbeatWatchdog.StartNew method (like in the Stopwatch)
    watchdog.Start();

    while (true) {
      // This is the most essential part, the actual heartbeat
      // It need to be happening more often than alert span
      // So watch out for your delays and timeouts
      heartbeatWatchdog.Beat();

      // Do some work
    }
});
```
