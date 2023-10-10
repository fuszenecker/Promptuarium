# Data\.FromFloat\(Single\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from a float value\.

```csharp
public static System.IO.Stream FromFloat(float value)
```

### Parameters

**value** &ensp; [Single](https://docs.microsoft.com/en-us/dotnet/api/system.single)

The float value

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.Data = Data.FromFloat(1.234f);
node.MetaData = Data.FromFloat(5.678f);
```

