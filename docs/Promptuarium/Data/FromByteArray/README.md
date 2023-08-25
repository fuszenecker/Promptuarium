# Data\.FromByteArray\(Byte\[\]\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from a byte array\.

```csharp
public static System.IO.Stream FromByteArray(byte[] value)
```

### Parameters

**value** &ensp; [Byte](https://docs.microsoft.com/en-us/dotnet/api/system.byte)\[\]

The byte array

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.Data = Data.FromByteArray(new byte[] { 0x12, 0x34, 0x56, 0x78 });
node.MetaData = Data.FromByteArray(new byte[] { 0x9A, 0xBC, 0xDE, 0xF0 });
```

