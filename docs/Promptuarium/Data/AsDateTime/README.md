# Data\.AsDateTime\(Stream\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts a stream to a DateTime value\.

```csharp
public static DateTime AsDateTime(this System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

### Returns

[DateTime](https://docs.microsoft.com/en-us/dotnet/api/system.datetime)

The DateTime value

## Examples

```
DateTime data = node.Data.AsDateTime();
```

