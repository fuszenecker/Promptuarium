# Element\.Detach Method

[Home](../../../README.md)

**Containing Type**: [Element](../README.md)

**Assembly**: Promptuarium\.dll

## Overloads

| Method | Summary |
| ------ | ------- |
| [Detach()](#2614073258) | Detaches a subtree or node\. |
| [Detach(Element)](#119873975) | Detaches a subtree or node\. |

<a id="2614073258"></a>

## Detach\(\) 

  
Detaches a subtree or node\.

```csharp
public Promptuarium.Element Detach()
```

### Returns

[Element](../README.md)

The node itself

### Exceptions

[PromptuariumException](../../PromptuariumException/README.md)

Thrown if the node does not exist in the tree

### Examples

```
var tree = new Element();

var node1 = new Element();
var node2 = new Element();
var node3 = new Element();

tree.Add(node1, node2, node3);

var detachedNode = node2.Detach();
```

<a id="119873975"></a>

## Detach\(Element\) 

  
Detaches a subtree or node\.

```csharp
public Promptuarium.Element Detach(Promptuarium.Element node)
```

### Parameters

**node** &ensp; [Element](../README.md)

The node to be detached

### Returns

[Element](../README.md)

The node itself

### Exceptions

[PromptuariumException](../../PromptuariumException/README.md)

Thrown if the node does not exist in the tree

### Examples

```
var tree = new Element();

var node1 = new Element();
var node2 = new Element();
var node3 = new Element();

tree.Add(node1, node2, node3);

var detachedNode = tree.Detach(node2);
```

