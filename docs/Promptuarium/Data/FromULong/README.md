# Data\.FromULong\(UInt64\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from an ulong value\.

```csharp
public static System.IO.Stream FromULong(ulong value)
```

### Parameters

**value** &ensp; [UInt64](https://docs.microsoft.com/en-us/dotnet/api/system.uint64)

The ulong value

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.Data = Data.FromULong(0x123456789ABCDEF0);
node.MetaData = Data.FromULong(0x123456789ABCDEF0);
```

