# Element\.IEnumerable\.GetEnumerator\(\) Method

[Home](../../../README.md)

**Containing Type**: [Element](../README.md)

**Assembly**: Promptuarium\.dll

  
Returns an enumerator that iterates through the Children

```csharp
System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
```

### Returns

[IEnumerator](https://docs.microsoft.com/en-us/dotnet/api/system.collections.ienumerator)

The enumerator of children

## Examples

```
var tree = new Element();

var node1 = new Element();
var node2 = new Element();
var node3 = new Element();

tree.Add(node1, node2, node3);

foreach (var node in tree)
{
  Console.WriteLine(node.ToString());
}
```

