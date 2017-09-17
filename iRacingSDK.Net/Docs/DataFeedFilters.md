##Data Feed Filters

The following describes a set of extension methods to the enumeration `IEnumerable<DataSample>` to enhance and refine some of the game's data stream.

They can each be applied to a `IEnumerable<DataSample>` and return an `IEnumerable<DataSample>`.  

The filters can be chained together.

###WithCorrectedPercentages

```
public static IEnumerable<DataSample> WithCorrectedPercentages(this IEnumerable<DataSample> samples)
```

Work around what appears to be a bug in the iRacing data stream, where cars' lap percentage is reported slightly behind - 
so that as the cars cross the start/finish line, their percentage still is in the 99% range, a frame later, their percentage drops to near 0%.

[Details](DataFeedFilters_WithCorrectedPercentages.md)


###WithCorrectedDistances

```
public static IEnumerable<DataSample> WithCorrectedDistances(this IEnumerable<DataSample> samples)
```

Corrects a car's lap and distance values, when there is a momentary network dropout for the cars' data feed.

[Details](DataFeedFilters_WithCorrectedDistances.md)

