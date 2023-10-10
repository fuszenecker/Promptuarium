# Data\.AsGuid\(Stream\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts a stream to a GUID value\.

```csharp
public static Guid AsGuid(this System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

### Returns

[Guid](https://docs.microsoft.com/en-us/dotnet/api/system.guid)

The GUID value

## Examples

```
Guid metadata = node.MetaData.AsGuid();
```

