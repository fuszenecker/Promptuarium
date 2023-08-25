# Element Class

[Home](../../README.md) &#x2022; [Constructors](#constructors) &#x2022; [Indexers](#indexers) &#x2022; [Properties](#properties) &#x2022; [Methods](#methods) &#x2022; [Operators](#operators) &#x2022; [Events](#events) &#x2022; [Explicit Interface Implementations](#explicit-interface-implementations) &#x2022; [Delegates](#delegates)

**Namespace**: [Promptuarium](../README.md)

**Assembly**: Promptuarium\.dll

```csharp
public class Element : System.Collections.Generic.IEnumerable<Promptuarium.Element>
```

### Inheritance

[Object](https://docs.microsoft.com/en-us/dotnet/api/system.object) &#x2192; Element

### Implements

* [IEnumerable](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1)\<[Element](./README.md)\>

## Constructors

| Constructor | Summary |
| ----------- | ------- |
| [Element()](-ctor/README.md#3108411760) | Constructor without any data |
| [Element(Element)](-ctor/README.md#612271537) | Copy constructor |
| [Element(Stream, Stream, Element\[\])](-ctor/README.md#454853735) | Contractor that also adds child elements |
| [Element(Stream, Stream, IEnumerable\<Element\>)](-ctor/README.md#1098567175) | LINQ friendly contractor that also adds child elements |

## Indexers

| Indexer | Summary |
| ------- | ------- |
| [Item\[Int32\]](Item/README.md) | Get an item by index\. |

## Properties

| Property | Summary |
| -------- | ------- |
| [Children](Children/README.md) | List of children\. it is never null\. |
| [Data](Data/README.md) | Data storage |
| [MetaData](MetaData/README.md) | Metadata storage |
| [Parent](Parent/README.md) | Reference to the parent node; root node has null as parent\. |

## Methods

| Method | Summary |
| ------ | ------- |
| [Add(Element\[\])](Add/README.md#2896179060) | Add node\(s\) to the tree\. Node can be null, in this case nothing will happen\. |
| [Add(IEnumerable\<Element\>)](Add/README.md#1328999739) | Add node\(s\) to the tree\. Node can be null, in this case nothing will happen\. |
| [Detach()](Detach/README.md#2614073258) | Detaches a subtree or node\. |
| [Detach(Element)](Detach/README.md#119873975) | Detaches a subtree or node\. |
| [Equals(Object)](https://docs.microsoft.com/en-us/dotnet/api/system.object.equals) |  \(Inherited from [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object)\) |
| [FromBase64StringAsync(String, CancellationToken)](FromBase64StringAsync/README.md#142880008) | Creates a tree from a Base64 string\. |
| [FromBase64StringAsync(String)](FromBase64StringAsync/README.md#3836437132) | Creates a tree from a Base64 string\. |
| [GetEnumerator()](GetEnumerator/README.md) |  \(Implements [IEnumerable\<Element\>.GetEnumerator](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1.getenumerator)\) |
| [GetHashCode()](https://docs.microsoft.com/en-us/dotnet/api/system.object.gethashcode) |  \(Inherited from [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object)\) |
| [GetStatistics()](GetStatistics/README.md) | |
| [GetType()](https://docs.microsoft.com/en-us/dotnet/api/system.object.gettype) |  \(Inherited from [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object)\) |
| [LoadAsync(Stream, CancellationToken)](LoadAsync/README.md#2169171820) | Loads a tree from a stream \(synchronously\)\. |
| [LoadAsync(Stream)](LoadAsync/README.md#2517542937) | |
| [LoadAsync(String, CancellationToken)](LoadAsync/README.md#25283078) | Loads a tree from a file \(synchronously\)\. |
| [LoadAsync(String)](LoadAsync/README.md#1926306021) | |
| [MemberwiseClone()](https://docs.microsoft.com/en-us/dotnet/api/system.object.memberwiseclone) |  \(Inherited from [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object)\) |
| [Remove(Element)](Remove/README.md) | Removes a subtree or node recursively\. |
| [SaveAsync(Stream, CancellationToken)](SaveAsync/README.md#404143214) | Saves the tree into a stream \(synchronously\)\. |
| [SaveAsync(Stream)](SaveAsync/README.md#2654619920) | |
| [SaveAsync(String, CancellationToken)](SaveAsync/README.md#717888685) | Saves the tree into a file \(synchronously\)\. |
| [SaveAsync(String)](SaveAsync/README.md#3052064839) | |
| [ToBase64StringAsync()](ToBase64StringAsync/README.md#579211045) | Converts the tree to Base64 string\. |
| [ToBase64StringAsync(CancellationToken)](ToBase64StringAsync/README.md#154336211) | Converts the tree to Base64 string\. |
| [ToString()](ToString/README.md) | Converts the element to string \(for debugging purposes\)\. \(Overrides [Object.ToString](https://docs.microsoft.com/en-us/dotnet/api/system.object.tostring)\) |
| [TreeToString(String)](TreeToString/README.md) | Converts the whole tree to string \(for debugging purposes\)\. |
| [Walk(WalkHandler)](Walk/README.md) | Walks through the tree, and calls the handler for each node\. |

## Operators

| Operator | Summary |
| -------- | ------- |
| [Addition(Element, Element)](op_Addition/README.md) | Add node\(s\) to the tree\. Node can be null, in this case nothing will happen\. |
| [Subtraction(Element, Element)](op_Subtraction/README.md) | Removes a subtree or node recursively\. |

## Events

| Event | Summary |
| ----- | ------- |
| [OnDataLoaded](OnDataLoaded/README.md) | |
| [OnDataLoading](OnDataLoading/README.md) | |
| [OnDataSaved](OnDataSaved/README.md) | |
| [OnDataSaving](OnDataSaving/README.md) | |
| [OnMetaDataLoaded](OnMetaDataLoaded/README.md) | |
| [OnMetaDataLoading](OnMetaDataLoading/README.md) | |
| [OnMetaDataSaved](OnMetaDataSaved/README.md) | |
| [OnMetaDataSaving](OnMetaDataSaving/README.md) | |

## Explicit Interface Implementations

| Member | Summary |
| ------ | ------- |
| [IEnumerable.GetEnumerator()](System-Collections-IEnumerable-GetEnumerator/README.md) | |

## Delegates

| Delegate | Summary |
| -------- | ------- |
| [WalkHandler](WalkHandler/README.md) | The method is called for each nodes\. |

