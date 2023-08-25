# Data\.AsShort\(Stream\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts a stream to a short value\.

```csharp
public static short AsShort(this System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

### Returns

[Int16](https://docs.microsoft.com/en-us/dotnet/api/system.int16)

The short value

## Examples

```
short data = node.Data.AsShort();
```

