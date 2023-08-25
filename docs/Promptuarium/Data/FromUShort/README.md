# Data\.FromUShort\(UInt16\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from an ushort value\.

```csharp
public static System.IO.Stream FromUShort(ushort value)
```

### Parameters

**value** &ensp; [UInt16](https://docs.microsoft.com/en-us/dotnet/api/system.uint16)

The ushort value

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.Data = Data.FromUShort(0x1234);
node.MetaData = Data.FromUShort(0x5678);
```

