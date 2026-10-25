#### [Promptuarium](README.md 'README')
### [Promptuarium](README.md#Promptuarium 'Promptuarium')

## Statistics Class

Statistics of the tree

```csharp
public class Statistics
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → Statistics

### Example

```csharp
var statistics = tree.GetStatistics().ToString();
Console.WriteLine(statistics);
```

### Remarks
This class is used to collect statistics of the tree\.
### Properties

<a name='Promptuarium.Statistics.Depth'></a>

## Statistics\.Depth Property

The depth of the tree

```csharp
public int Depth { get; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='Promptuarium.Statistics.LongestData'></a>

## Statistics\.LongestData Property

The longest data in the tree

```csharp
public long LongestData { get; }
```

#### Property Value
[System\.Int64](https://learn.microsoft.com/en-us/dotnet/api/system.int64 'System\.Int64')

<a name='Promptuarium.Statistics.LongestMetaData'></a>

## Statistics\.LongestMetaData Property

The longest metadata in the tree

```csharp
public long LongestMetaData { get; }
```

#### Property Value
[System\.Int64](https://learn.microsoft.com/en-us/dotnet/api/system.int64 'System\.Int64')

<a name='Promptuarium.Statistics.MaxChildren'></a>

## Statistics\.MaxChildren Property

The maximum number of children of a node

```csharp
public int MaxChildren { get; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='Promptuarium.Statistics.MinChildren'></a>

## Statistics\.MinChildren Property

The minimum number of children of a node

```csharp
public int MinChildren { get; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='Promptuarium.Statistics.Nodes'></a>

## Statistics\.Nodes Property

The number of nodes in the tree

```csharp
public int Nodes { get; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='Promptuarium.Statistics.NodesWithData'></a>

## Statistics\.NodesWithData Property

The number of nodes with data, but without metadata

```csharp
public int NodesWithData { get; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='Promptuarium.Statistics.NodesWithDataAndMetaData'></a>

## Statistics\.NodesWithDataAndMetaData Property

The number of nodes with data and metadata

```csharp
public int NodesWithDataAndMetaData { get; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='Promptuarium.Statistics.NodesWithMetaData'></a>

## Statistics\.NodesWithMetaData Property

The number of nodes with metadata, but without data

```csharp
public int NodesWithMetaData { get; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='Promptuarium.Statistics.NodesWithoutChildren'></a>

## Statistics\.NodesWithoutChildren Property

The number of nodes without children

```csharp
public int NodesWithoutChildren { get; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='Promptuarium.Statistics.NodesWithoutDataAndMetaData'></a>

## Statistics\.NodesWithoutDataAndMetaData Property

The number of nodes without data and metadata

```csharp
public int NodesWithoutDataAndMetaData { get; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='Promptuarium.Statistics.ShortestData'></a>

## Statistics\.ShortestData Property

The shortest data in the tree

```csharp
public long ShortestData { get; }
```

#### Property Value
[System\.Int64](https://learn.microsoft.com/en-us/dotnet/api/system.int64 'System\.Int64')

<a name='Promptuarium.Statistics.ShortestMetaData'></a>

## Statistics\.ShortestMetaData Property

The shortest metadata in the tree

```csharp
public long ShortestMetaData { get; }
```

#### Property Value
[System\.Int64](https://learn.microsoft.com/en-us/dotnet/api/system.int64 'System\.Int64')
### Methods

<a name='Promptuarium.Statistics.ToString()'></a>

## Statistics\.ToString\(\) Method

Returns a string that represents the current object\.

```csharp
public override string ToString();
```

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
A string that represents the current object\.

### Example

```csharp
var statistics = tree.GetStatistics();
Console.WriteLine(statistics.ToString());
```