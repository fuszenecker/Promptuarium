# Data\.FromDecimal\(Decimal\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from an decimal value\.

```csharp
public static System.IO.Stream FromDecimal(decimal value)
```

### Parameters

**value** &ensp; [Decimal](https://docs.microsoft.com/en-us/dotnet/api/system.decimal)

The decimal value

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.Data = Data.FromDecimal(1.234m);
node.MetaData = Data.FromDecimal(5.678m);
```

