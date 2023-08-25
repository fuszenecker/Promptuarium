# Element\.Subtraction\(Element, Element\) Operator

[Home](../../../README.md)

**Containing Type**: [Element](../README.md)

**Assembly**: Promptuarium\.dll

  
Removes a subtree or node recursively\.

```csharp
public static Promptuarium.Element operator -(Promptuarium.Element tree, Promptuarium.Element node)
```

### Parameters

**tree** &ensp; [Element](../README.md)

The tree to be shrunk

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

tree -= node2;
```

