# Data\.FromDouble\(Double\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from a double value\.

```csharp
public static System.IO.Stream FromDouble(double value)
```

### Parameters

**value** &ensp; [Double](https://docs.microsoft.com/en-us/dotnet/api/system.double)

The double value

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.Data = Data.FromDouble(1.234);
node.MetaData = Data.FromDouble(5.678);
```

