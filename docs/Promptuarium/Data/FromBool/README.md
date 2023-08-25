# Data\.FromBool\(Boolean\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from a boolean value\.

```csharp
public static System.IO.Stream FromBool(bool value)
```

### Parameters

**value** &ensp; [Boolean](https://docs.microsoft.com/en-us/dotnet/api/system.boolean)

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.Data = Data.FromBool(true);
node.MetaData = Data.FromBool(false);
```

