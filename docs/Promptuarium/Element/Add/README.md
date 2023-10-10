# Element\.Add Method

[Home](../../../README.md)

**Containing Type**: [Element](../README.md)

**Assembly**: Promptuarium\.dll

## Overloads

| Method | Summary |
| ------ | ------- |
| [Add(Element\[\])](#2896179060) | Add node\(s\) to the tree\. Node can be null, in this case nothing will happen\. |
| [Add(IEnumerable\<Element\>)](#1328999739) | Add node\(s\) to the tree\. Node can be null, in this case nothing will happen\. |

<a id="2896179060"></a>

## Add\(Element\[\]\) 

  
Add node\(s\) to the tree\. Node can be null, in this case nothing will happen\.

```csharp
public Promptuarium.Element Add(params Promptuarium.Element[] nodes)
```

### Parameters

**nodes** &ensp; [Element](../README.md)\[\]

The nodes to be added

### Returns

[Element](../README.md)

### Exceptions

[PromptuariumException](../../PromptuariumException/README.md)

Thrown if the node already exists in the tree

### Examples

```
var tree = new Element();

var node1 = new Element();
var node2 = new Element();
var node3 = new Element();

tree.Add(node1, node2, node3);
```



```
var tree = new Element()
{
    new Element(),
    new Element(),
    new Element()
};
```

<a id="1328999739"></a>

## Add\(IEnumerable\<Element\>\) 

  
Add node\(s\) to the tree\. Node can be null, in this case nothing will happen\.

```csharp
public Promptuarium.Element Add(System.Collections.Generic.IEnumerable<Promptuarium.Element> nodes)
```

### Parameters

**nodes** &ensp; [IEnumerable](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1)\<[Element](../README.md)\>

The nodes to be added

### Returns

[Element](../README.md)

### Exceptions

[PromptuariumException](../../PromptuariumException/README.md)

Thrown if the node already exists in the tree

### Examples

```
var tree = new Element();

var node1 = new Element();
var node2 = new Element();
var node3 = new Element();

tree.Add(node1, node2, node3);
```

