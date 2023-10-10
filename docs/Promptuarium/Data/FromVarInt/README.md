# Data\.FromVarInt\(Int64\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from a long value\.

```csharp
public static System.IO.Stream FromVarInt(long value)
```

### Parameters

**value** &ensp; [Int64](https://docs.microsoft.com/en-us/dotnet/api/system.int64)

The long value

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.Data = Data.FromVarInt(100);
node.MetaData = Data.FromVarInt(200);
```

