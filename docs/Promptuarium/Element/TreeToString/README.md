# Element\.TreeToString\(String\) Method

[Home](../../../README.md)

**Containing Type**: [Element](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts the whole tree to string \(for debugging purposes\)\.

```csharp
public string TreeToString(string tabulator = ">")
```

### Parameters

**tabulator** &ensp; [String](https://docs.microsoft.com/en-us/dotnet/api/system.string)

An optional tabulator for conversion\.

### Returns

[String](https://docs.microsoft.com/en-us/dotnet/api/system.string)

## Examples

```
var tree = new Element();

var node1 = new Element();
    
var node2 = new Element();
node2.Data = Data.FromUtf8String("Hello world");

var node3 = new Element();
node3.Data = Data.FromUtf8String("Hello world");

tree.Add(node1, node2, node3);

var treeString = tree.TreeToString();
```

