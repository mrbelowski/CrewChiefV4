###WithCorrectedPercentages

Work around what appears to be a bug in the iRacing data stream, where cars' lap percentage is reported slightly behind - 
so that as the cars cross the start/finish line, their percentage still is in the 99% range a frame later there percentage drops to near 0%.

This makes it look as if the car has instantly jump to the end of the next lap, then jump back to start of lap.

For example, you may get data like:

| Frame Number | CarIdxLap | CarIdxLapDistPct |
| ------------ | --------- | ---------------- |
|  60000       |        2  |           97.59  |
|  60001       |        2  |           98.52  |
|  60002       |        3  |           99.80  |
|  60003       |        3  |           01.23  |

By using this Filter, the above table would be converted to:

| Frame Number | CarIdxLap | CarIdxLapDistPct |
| ------------ | --------- | ---------------- |
|  60000       |        2  |           97.59  |
|  60001       |        2  |           98.52  |
|  60002       |        3  |           00.00  |
|  60003       |        3  |           01.23  |

**Example**

```
foreach( var data in iRacing
             .GetDataFeed()
			 .WithCorrectedPercentages())
{
    Console.WriteLine(data.Telemetry.CarIdxLap[3]);
    Console.WriteLine(data.Telemetry.CarIdxLapDistPct[3]);

    Thread.Sleep(1000);
}
```

Its important to note that this filter can only be used, if you are replaying the data stream forward.  If you jump the replay to a different position, or replay in reverse, then this filter will not be able to function.

It will raise an error if it gets non incrementing frame numbers.
