# Data\.AsVarUInt\(Stream\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts a stream to a ulong value\.

```csharp
public static ulong AsVarUInt(this System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

### Returns

[UInt64](https://docs.microsoft.com/en-us/dotnet/api/system.uint64)

The ulong value

## Examples

```
ulong data = node.Data.AsVarUInt();
```

