# Data\.AsElementAsync\(Stream\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts a stream to a tree\.

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> AsElementAsync(this System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1)\<[Element](../../Element/README.md)\>

The tree

## Examples

```
var tree = await Data.AsTreeAsync(stream);
```

