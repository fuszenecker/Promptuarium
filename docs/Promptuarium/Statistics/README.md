# Statistics Class

[Home](../../README.md) &#x2022; [Examples](#examples) &#x2022; [Remarks](#remarks) &#x2022; [Constructors](#constructors) &#x2022; [Fields](#fields) &#x2022; [Methods](#methods)

**Namespace**: [Promptuarium](../README.md)

**Assembly**: Promptuarium\.dll

  
Statistics of the tree

```csharp
public class Statistics
```

### Inheritance

[Object](https://docs.microsoft.com/en-us/dotnet/api/system.object) &#x2192; Statistics

## Examples

```
var statistics = tree.GetStatistics().ToString();
Console.WriteLine(statistics);
```

## Remarks

This class is used to collect statistics of the tree\.

## Constructors

| Constructor | Summary |
| ----------- | ------- |
| [Statistics()](-ctor/README.md) | |

## Fields

| Field | Summary |
| ----- | ------- |
| [Depth](Depth/README.md) | The depth of the tree |
| [LongestData](LongestData/README.md) | The longest data in the tree |
| [LongestMetaData](LongestMetaData/README.md) | The longest metadata in the tree |
| [MaxChildren](MaxChildren/README.md) | The maximum number of children of a node |
| [MinChildren](MinChildren/README.md) | The minimum number of children of a node |
| [Nodes](Nodes/README.md) | The number of nodes in the tree |
| [NodesWithData](NodesWithData/README.md) | The number of nodes with data |
| [NodesWithDataAndMetaData](NodesWithDataAndMetaData/README.md) | The number of nodes with data and metadata |
| [NodesWithMetaData](NodesWithMetaData/README.md) | The number of nodes with metadata |
| [NodesWithoutChildren](NodesWithoutChildren/README.md) | The number of nodes without children |
| [NodesWithoutDataAndMetaData](NodesWithoutDataAndMetaData/README.md) | The number of nodes without data and metadata |
| [ShortestData](ShortestData/README.md) | The shortest data in the tree |
| [ShortestMetaData](ShortestMetaData/README.md) | The shortest metadata in the tree |

## Methods

| Method | Summary |
| ------ | ------- |
| [Equals(Object)](https://docs.microsoft.com/en-us/dotnet/api/system.object.equals) |  \(Inherited from [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object)\) |
| [GetHashCode()](https://docs.microsoft.com/en-us/dotnet/api/system.object.gethashcode) |  \(Inherited from [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object)\) |
| [GetType()](https://docs.microsoft.com/en-us/dotnet/api/system.object.gettype) |  \(Inherited from [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object)\) |
| [MemberwiseClone()](https://docs.microsoft.com/en-us/dotnet/api/system.object.memberwiseclone) |  \(Inherited from [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object)\) |
| [ToString()](ToString/README.md) | Returns a string that represents the current object\. \(Overrides [Object.ToString](https://docs.microsoft.com/en-us/dotnet/api/system.object.tostring)\) |

