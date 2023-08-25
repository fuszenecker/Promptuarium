# Data\.AsUInt\(Stream\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts a stream to an uint value\.

```csharp
public static uint AsUInt(this System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

### Returns

[UInt32](https://docs.microsoft.com/en-us/dotnet/api/system.uint32)

The uint value

## Examples

```
uint data = node.Data.AsUInt();
```

