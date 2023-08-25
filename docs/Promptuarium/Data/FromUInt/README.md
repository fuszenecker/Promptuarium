# Data\.FromUInt\(UInt32\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from an uint value\.

```csharp
public static System.IO.Stream FromUInt(uint value)
```

### Parameters

**value** &ensp; [UInt32](https://docs.microsoft.com/en-us/dotnet/api/system.uint32)

The uint value

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.Data = Data.FromUInt(0x12345678);
node.MetaData = Data.FromUInt(0x9ABCDEF0);
```

