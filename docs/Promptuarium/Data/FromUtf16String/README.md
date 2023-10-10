# Data\.FromUtf16String\(String\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from a string using UTF\-16 encoding\.

```csharp
public static System.IO.Stream FromUtf16String(string value)
```

### Parameters

**value** &ensp; [String](https://docs.microsoft.com/en-us/dotnet/api/system.string)

The string

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.Data = Data.FromUtf16String("Hello");
node.MetaData = Data.FromUtf16String("World");
```

