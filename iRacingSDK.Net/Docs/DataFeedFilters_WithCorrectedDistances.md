###WithCorrectedDistances

If a car's data feed drops out momentary due to usual network glitches (often seen in game with blinking cars), the data feed will
report the car's lap and distance value as -1. 

This filter will suppress the -1 values for the fields, and replace them with the car's last known maximum values.  

For example, you may get data like:

| Frame Number | CarIdxLap | CarIdxLapDistPct |
| ------------ | --------- | ---------------- |
|  60000       |        2  |           45.44  |
|  60001       |       -1  |            -1.0  |
|  60002       |       -1  |            -1.0  |
|  60003       |        2  |           46.23  |

By using this Filter, the above table would be converted to:

| Frame Number | CarIdxLap | CarIdxLapDistPct |
| ------------ | --------- | ---------------- |
|  60000       |        2  |           45.44  |
|  60001       |        2  |           45.44  |
|  60002       |        2  |           45.44  |
|  60003       |        2  |           46.23  |

**Example**

```
foreach( var data in iRacing
             .GetDataFeed()
			 .WithCorrectedDistances())
{
    Console.WriteLine(data.Telemetry.CarIdxLap[3]);
    Console.WriteLine(data.Telemetry.CarIdxLapDistPct[3]);

    Thread.Sleep(1000);
}
```

Its important to note that this filter can only be used, if you are replaying the data stream forward.  If you jump the replay to a different position, or replay in reverse, then this filter will not be able to function.

It will raise an error if it gets non incrementing frame numbers.
