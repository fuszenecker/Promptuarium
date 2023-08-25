# Data\.AsByteArray\(Stream\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts a stream to a byte array\.

```csharp
public static byte[] AsByteArray(this System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

### Returns

[Byte](https://docs.microsoft.com/en-us/dotnet/api/system.byte)\[\]

The byte array

## Examples

```
byte[] data = node.Data.AsByteArray();
```

