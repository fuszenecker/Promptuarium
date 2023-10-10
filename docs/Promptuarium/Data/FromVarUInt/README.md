# Data\.FromVarUInt\(UInt64\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from a ulong value\.

```csharp
public static System.IO.Stream FromVarUInt(ulong value)
```

### Parameters

**value** &ensp; [UInt64](https://docs.microsoft.com/en-us/dotnet/api/system.uint64)

The ulong value

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.Data = Data.FromVarUInt(100);
node.MetaData = Data.FromVarUInt(200);
```

