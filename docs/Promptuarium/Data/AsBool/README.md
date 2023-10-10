# Data\.AsBool\(Stream\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts a stream to a boolean value\.

```csharp
public static bool AsBool(this System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

### Returns

[Boolean](https://docs.microsoft.com/en-us/dotnet/api/system.boolean)

The boolean value

## Examples

```
bool data = node.Data.AsBool();
bool metadata = node.MetaData.AsBool();
```

