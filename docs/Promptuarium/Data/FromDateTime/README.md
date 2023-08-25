# Data\.FromDateTime\(DateTime\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from a DateTime value\.

```csharp
public static System.IO.Stream FromDateTime(DateTime value)
```

### Parameters

**value** &ensp; [DateTime](https://docs.microsoft.com/en-us/dotnet/api/system.datetime)

The DateTime value

### Returns

[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The stream

## Examples

```
node.Data = Data.FromDateTime(DateTime.Now);
node.MetaData = Data.FromDateTime(DateTime.UtcNow);
```

