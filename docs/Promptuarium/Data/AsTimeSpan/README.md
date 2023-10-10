# Data\.AsTimeSpan\(Stream\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts a stream to a TimeSpan value\.

```csharp
public static TimeSpan AsTimeSpan(this System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

### Returns

[TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/system.timespan)

The TimeSpan value

## Examples

```
TimeSpan data = node.Data.AsTimeSpan();
```

