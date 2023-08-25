# Data\.AsAsciiString\(Stream\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts a stream to a string using ASCII encoding\.

```csharp
public static string AsAsciiString(this System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

### Returns

[String](https://docs.microsoft.com/en-us/dotnet/api/system.string)

The string

## Examples

```
string data = node.Data.AsAsciiString();
```

