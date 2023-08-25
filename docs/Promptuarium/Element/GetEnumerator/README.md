# Element\.GetEnumerator\(\) Method

[Home](../../../README.md)

**Containing Type**: [Element](../README.md)

**Assembly**: Promptuarium\.dll

  
Returns an enumerator that iterates through the Children

```csharp
public System.Collections.Generic.IEnumerator<Promptuarium.Element> GetEnumerator()
```

### Returns

[IEnumerator](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerator-1)\<[Element](../README.md)\>

The enumerator of children

### Implements

* [IEnumerable\<Element\>.GetEnumerator](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1.getenumerator)

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

