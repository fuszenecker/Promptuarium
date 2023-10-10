# Data\.AsUShort\(Stream\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts a stream to an ushort value\.

```csharp
public static ushort AsUShort(this System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

### Returns

[UInt16](https://docs.microsoft.com/en-us/dotnet/api/system.uint16)

The ushort value

## Examples

```
ushort data = node.Data.AsUShort();
```

