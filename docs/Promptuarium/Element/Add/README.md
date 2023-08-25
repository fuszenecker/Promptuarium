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

