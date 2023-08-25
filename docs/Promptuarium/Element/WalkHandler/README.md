# Element\.WalkHandler Delegate

[Home](../../../README.md)

**Namespace**: [Promptuarium](../../README.md)

**Assembly**: Promptuarium\.dll

  
The method is called for each nodes\.

```csharp
public delegate void Element.WalkHandler(Promptuarium.Element node, System.Collections.Generic.IReadOnlyCollection<Promptuarium.Element> ancestors)
```

### Parameters

**node** &ensp; [Element](../README.md)

**ancestors** &ensp; [IReadOnlyCollection](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlycollection-1)\<[Element](../README.md)\>

### Inheritance

[Object](https://docs.microsoft.com/en-us/dotnet/api/system.object) &#x2192; [Delegate](https://docs.microsoft.com/en-us/dotnet/api/system.delegate) &#x2192; [MulticastDelegate](https://docs.microsoft.com/en-us/dotnet/api/system.multicastdelegate) &#x2192; Element\.WalkHandler
