# Data\.AsDateTimeOffset\(Stream\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts a stream to a DateTimeOffset value\.

```csharp
public static DateTimeOffset AsDateTimeOffset(this System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

### Returns

[DateTimeOffset](https://docs.microsoft.com/en-us/dotnet/api/system.datetimeoffset)

The DateTimeOffset value

## Examples

```
DateTimeOffset data = node.Data.AsDateTimeOffset();
```

