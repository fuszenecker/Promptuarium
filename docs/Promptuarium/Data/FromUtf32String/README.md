# Data\.FromUtf32String\(String\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from a string using UTF\-32 encoding\.

```csharp
public static System.IO.Stream FromUtf32String(string value)
```

### Parameters

**value** &ensp; [String](https://docs.microsoft.com/en-us/dotnet/api/system.string)

The string

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.Data = Data.FromUtf32String("Hello");
node.MetaData = Data.FromUtf32String("World");
```

