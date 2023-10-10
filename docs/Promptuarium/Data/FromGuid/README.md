# Data\.FromGuid\(Guid\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from a GUID value\.

```csharp
public static System.IO.Stream FromGuid(Guid value)
```

### Parameters

**value** &ensp; [Guid](https://docs.microsoft.com/en-us/dotnet/api/system.guid)

The GUID

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.MetaData = Data.FromGuid(Guid.NewGuid());
```

