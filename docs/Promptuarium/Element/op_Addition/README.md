# Element\.Addition\(Element, Element\) Operator

[Home](../../../README.md)

**Containing Type**: [Element](../README.md)

**Assembly**: Promptuarium\.dll

  
Add node\(s\) to the tree\. Node can be null, in this case nothing will happen\.

```csharp
public static Promptuarium.Element operator +(Promptuarium.Element tree, Promptuarium.Element node)
```

### Parameters

**tree** &ensp; [Element](../README.md)

The tree to be extended

**node** &ensp; [Element](../README.md)

The node to be added

### Returns

[Element](../README.md)

### Exceptions

[PromptuariumException](../../PromptuariumException/README.md)

Thrown if the node already exists in the tree

## Examples

```
var tree = new Element();

var node1 = new Element();
var node2 = new Element();
var node3 = new Element();

tree += node1;
tree += node2;
tree += node3;
```

