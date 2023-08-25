# Data\.AsByte\(Stream\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts a stream to a byte value\.

```csharp
public static byte AsByte(this System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

### Returns

[Byte](https://docs.microsoft.com/en-us/dotnet/api/system.byte)

The byte value

## Examples

```
byte data = node.Data.AsByte();
byte metadata = node.MetaData.AsByte();
```

