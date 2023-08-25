# Data\.AsFloat\(Stream\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts a stream to a float value\.

```csharp
public static float AsFloat(this System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

### Returns

[Single](https://docs.microsoft.com/en-us/dotnet/api/system.single)

The float value

## Examples

```
float data = node.Data.AsFloat();
```

