# Data\.FromInt\(Int32\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from an int value\.

```csharp
public static System.IO.Stream FromInt(int value)
```

### Parameters

**value** &ensp; [Int32](https://docs.microsoft.com/en-us/dotnet/api/system.int32)

The int value

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.Data = Data.FromInt(0x12345678);
node.MetaData = Data.FromInt(0x9ABCDEF0);
```

