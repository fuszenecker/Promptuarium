# Element\.GetStatistics\(\) Method

[Home](../../../README.md)

**Containing Type**: [Element](../README.md)

**Assembly**: Promptuarium\.dll

  
Gets the statistics about the tree

```csharp
public Promptuarium.Statistics GetStatistics()
```

### Returns

[Statistics](../../Statistics/README.md)

The statistics

## Examples

```
var statistics = tree.GetStatistics();
Console.WriteLine(statistics.ToString());
```

## Remarks

This method walks the tree and collects statistics about the tree\.