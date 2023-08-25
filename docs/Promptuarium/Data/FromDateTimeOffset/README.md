# Data\.FromDateTimeOffset\(DateTimeOffset\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from a DateTimeOffset value\.

```csharp
public static System.IO.Stream FromDateTimeOffset(DateTimeOffset value)
```

### Parameters

**value** &ensp; [DateTimeOffset](https://docs.microsoft.com/en-us/dotnet/api/system.datetimeoffset)

The DateTimeOffset value

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.Data = Data.FromDateTimeOffset(DateTimeOffset.Now);
node.MetaData = Data.FromDateTimeOffset(DateTimeOffset.UtcNow);
```

