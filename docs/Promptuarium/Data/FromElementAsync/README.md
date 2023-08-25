# Data\.FromElementAsync\(Element\) Method

[Home](../../../README.md)

**Containing Type**: [Data](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a stream from a tree\.

```csharp
public static System.Threading.Tasks.Task<System.IO.Stream> FromElementAsync(Promptuarium.Element value)
```

### Parameters

**value** &ensp; [Element](../../Element/README.md)

The tree

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1)\<[Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)\>

The stream

## Examples

```
var tree = new Element();
tree.Data = Data.FromInt(123);
tree.MetaData = Data.FromUtf8String("Hello");

tree += new Element();

var stream = Data.FromTree(tree);
```

