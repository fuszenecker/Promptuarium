# Data\.AsDouble\(Stream\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts a stream to a double value\.

```csharp
public static double AsDouble(this System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

### Returns

[Double](https://docs.microsoft.com/en-us/dotnet/api/system.double)

The double value

## Examples

```
double data = node.Data.AsDouble();
```

