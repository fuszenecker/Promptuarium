# Data\.FromTimeSpan\(TimeSpan\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from a TimeSpan value\.

```csharp
public static System.IO.Stream FromTimeSpan(TimeSpan value)
```

### Parameters

**value** &ensp; [TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/system.timespan)

The TimeSpan value

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.Data = Data.FromTimeSpan(TimeSpan.FromHours(1));
node.MetaData = Data.FromTimeSpan(TimeSpan.FromMinutes(30));
```

