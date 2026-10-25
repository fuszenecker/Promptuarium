#### [Promptuarium](README.md 'README')
### [Promptuarium](README.md#Promptuarium 'Promptuarium').[Element](Element.md 'Promptuarium\.Element')

## Element\.WalkHandler\(Element, IReadOnlyCollection\<Element\>\) Delegate

The method is called for each nodes\.

```csharp
public delegate void Element.WalkHandler(Promptuarium.Element node, System.Collections.Generic.IReadOnlyCollection<Promptuarium.Element> ancestors);
```
#### Parameters

<a name='Promptuarium.Element.WalkHandler(Promptuarium.Element,System.Collections.Generic.IReadOnlyCollection_Promptuarium.Element_).node'></a>

`node` [Element](Element.md 'Promptuarium\.Element')

The current node

<a name='Promptuarium.Element.WalkHandler(Promptuarium.Element,System.Collections.Generic.IReadOnlyCollection_Promptuarium.Element_).ancestors'></a>

`ancestors` [System\.Collections\.Generic\.IReadOnlyCollection&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlycollection-1 'System\.Collections\.Generic\.IReadOnlyCollection\`1')[Element](Element.md 'Promptuarium\.Element')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlycollection-1 'System\.Collections\.Generic\.IReadOnlyCollection\`1')

The ancestors of the node, from the root to the parent\. The collection is only valid during the call: copy it if it is needed later\.