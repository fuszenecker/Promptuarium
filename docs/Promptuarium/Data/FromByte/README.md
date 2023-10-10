# Data\.FromByte\(Byte\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from a byte value\.

```csharp
public static System.IO.Stream FromByte(byte value)
```

### Parameters

**value** &ensp; [Byte](https://docs.microsoft.com/en-us/dotnet/api/system.byte)

The byte value

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.Data = Data.FromByte(0x12);
node.MetaData = Data.FromByte(0x34);
```

