# Element\.Remove\(Element\) Method

[Home](../../../README.md)

**Containing Type**: [Element](../README.md)

**Assembly**: Promptuarium\.dll

  
Removes a subtree or node recursively\.

```csharp
public Promptuarium.Element Remove(Promptuarium.Element node)
```

### Parameters

**node** &ensp; [Element](../README.md)

The node to be removed

### Returns

[Element](../README.md)

### Exceptions

[PromptuariumException](../../PromptuariumException/README.md)

Thrown if the node does not exist in the tree

## Examples

```
var tree = new Element();

var node1 = new Element();
var node2 = new Element();
var node3 = new Element();

tree.Add(node1, node2, node3);

tree.Remove(node2);
```

