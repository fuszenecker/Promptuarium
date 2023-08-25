# Data\.AsLong\(Stream\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts a stream to a long value\.

```csharp
public static long AsLong(this System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

### Returns

[Int64](https://docs.microsoft.com/en-us/dotnet/api/system.int64)

The long value

## Examples

```
long data = node.Data.AsLong();
```

