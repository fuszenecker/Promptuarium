# Data\.FromShort\(Int16\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from a short value\.

```csharp
public static System.IO.Stream FromShort(short value)
```

### Parameters

**value** &ensp; [Int16](https://docs.microsoft.com/en-us/dotnet/api/system.int16)

The short value

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.Data = Data.FromShort(0x1234);
node.MetaData = Data.FromShort(0x5678);
```

