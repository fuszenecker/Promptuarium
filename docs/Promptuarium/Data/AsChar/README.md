# Data\.AsChar\(Stream\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts a stream to a char value\.

```csharp
public static char AsChar(this System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

### Returns

[Char](https://docs.microsoft.com/en-us/dotnet/api/system.char)

The char value

## Examples

```
char metadata = node.MetaData.AsChar();
```

