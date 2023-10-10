# Data\.FromUtf8String\(String\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from a string using UTF\-8 encoding\.

```csharp
public static System.IO.Stream FromUtf8String(string value)
```

### Parameters

**value** &ensp; [String](https://docs.microsoft.com/en-us/dotnet/api/system.string)

The string

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.Data = Data.FromUtf8String("Hello");
node.MetaData = Data.FromUtf8String("World");
```

