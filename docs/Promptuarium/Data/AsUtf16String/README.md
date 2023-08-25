# Data\.AsUtf16String\(Stream\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts a stream to a string using UTF\-16 encoding\.

```csharp
public static string AsUtf16String(this System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

### Returns

[String](https://docs.microsoft.com/en-us/dotnet/api/system.string)

The string

## Examples

```
string data = node.Data.AsUtf16String();
```

