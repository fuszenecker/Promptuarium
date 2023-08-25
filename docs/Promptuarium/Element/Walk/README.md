# Element\.Walk\(Element\.WalkHandler\) Method

[Home](../../../README.md)

**Containing Type**: [Element](../README.md)

**Assembly**: Promptuarium\.dll

  
Walks through the tree, and calls the handler for each node\.

```csharp
public Promptuarium.Element Walk(Promptuarium.Element.WalkHandler handler)
```

### Parameters

**handler** &ensp; [Element.WalkHandler](../WalkHandler/README.md)

The method to be called for each nodes

### Returns

[Element](../README.md)

The node itself

## Examples

```
var tree = new Element();

var node1 = new Element();
var node2 = new Element();
var node3 = new Element();

tree.Add(node1, node2, node3);

tree.Walk((Element node, IReadOnlyCollection<Element> ancestors) =>
   {
       Console.WriteLine(node);
   });
   
```

