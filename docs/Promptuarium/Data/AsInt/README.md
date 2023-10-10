# Data\.AsInt\(Stream\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts a stream to an int value\.

```csharp
public static int AsInt(this System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

### Returns

[Int32](https://docs.microsoft.com/en-us/dotnet/api/system.int32)

The int value

## Examples

```
int data = node.Data.AsInt();
```

