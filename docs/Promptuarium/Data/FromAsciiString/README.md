# Data\.FromAsciiString\(String\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from a string using ASCII encoding\.

```csharp
public static System.IO.Stream FromAsciiString(string value)
```

### Parameters

**value** &ensp; [String](https://docs.microsoft.com/en-us/dotnet/api/system.string)

The string

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.Data = Data.FromAsciiString("Hello");
node.MetaData = Data.FromAsciiString("World");
```

