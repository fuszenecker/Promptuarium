# Element\.ToString\(\) Method

[Home](../../../README.md)

**Containing Type**: [Element](../README.md)

**Assembly**: Promptuarium\.dll

  
Converts the element to string \(for debugging purposes\)\.

```csharp
public override string ToString()
```

### Returns

[String](https://docs.microsoft.com/en-us/dotnet/api/system.string)

## Examples

```
var tree = new Element();
tree.Data = Data.FromUtf8String("Hello world");
var nodeString = tree.ToString();
```

