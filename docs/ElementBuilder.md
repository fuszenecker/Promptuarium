#### [Promptuarium](README.md 'README')
### [Promptuarium](README.md#Promptuarium 'Promptuarium')

## ElementBuilder Class

Builds a node step by step: its data, metadata, children, or a signature \(see BINARY\_FORMAT\.md, "File identification"\)\.

```csharp
public class ElementBuilder
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → ElementBuilder

### Example

```csharp
var root = new ElementBuilder()
    .WithSignature(Data.FromAsciiString("PRMT"))
    .WithChildren(new Element(Data.FromUtf8String("content"), null))
    .Build();

var node = new ElementBuilder()
    .WithData(Data.FromInt(42))
    .WithMetaData(Data.FromAsciiString("answer"))
    .Build();
```
### Fields

<a name='Promptuarium.ElementBuilder.MaxSignatureLength'></a>

## ElementBuilder\.MaxSignatureLength Field

Maximum length of a signature: it fits in a single linear chunk, so it is at a fixed position at the beginning of the stream\.

```csharp
public const int MaxSignatureLength = 15;
```

#### Field Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')
### Methods

<a name='Promptuarium.ElementBuilder.Build()'></a>

## ElementBuilder\.Build\(\) Method

Builds the node\.

```csharp
public Promptuarium.Element Build();
```

#### Returns
[Element](Element.md 'Promptuarium\.Element')  
The new node

#### Exceptions

[System\.InvalidOperationException](https://learn.microsoft.com/en-us/dotnet/api/system.invalidoperationexception 'System\.InvalidOperationException')  
Thrown if a signature is combined with data or metadata

[PromptuariumException](PromptuariumException.md 'Promptuarium\.PromptuariumException')  
Thrown if a child can not be added \(e\.g\. it was added twice\)

<a name='Promptuarium.ElementBuilder.WithChildren(Promptuarium.Element[])'></a>

## ElementBuilder\.WithChildren\(Element\[\]\) Method

Adds children to the node\. They are moved into the node when it is built\.

```csharp
public Promptuarium.ElementBuilder WithChildren(params Promptuarium.Element[] children);
```
#### Parameters

<a name='Promptuarium.ElementBuilder.WithChildren(Promptuarium.Element[]).children'></a>

`children` [Element](Element.md 'Promptuarium\.Element')[\[\]](https://learn.microsoft.com/en-us/dotnet/api/system.array 'System\.Array')

The children

#### Returns
[ElementBuilder](ElementBuilder.md 'Promptuarium\.ElementBuilder')  
The builder

<a name='Promptuarium.ElementBuilder.WithChildren(System.Collections.Generic.IEnumerable_Promptuarium.Element_)'></a>

## ElementBuilder\.WithChildren\(IEnumerable\<Element\>\) Method

Adds children to the node\. They are moved into the node when it is built\.

```csharp
public Promptuarium.ElementBuilder WithChildren(System.Collections.Generic.IEnumerable<Promptuarium.Element> children);
```
#### Parameters

<a name='Promptuarium.ElementBuilder.WithChildren(System.Collections.Generic.IEnumerable_Promptuarium.Element_).children'></a>

`children` [System\.Collections\.Generic\.IEnumerable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')[Element](Element.md 'Promptuarium\.Element')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')

The children

#### Returns
[ElementBuilder](ElementBuilder.md 'Promptuarium\.ElementBuilder')  
The builder

<a name='Promptuarium.ElementBuilder.WithData(System.ReadOnlyMemory_byte_)'></a>

## ElementBuilder\.WithData\(ReadOnlyMemory\<byte\>\) Method

Sets the data of the node\.

```csharp
public Promptuarium.ElementBuilder WithData(System.ReadOnlyMemory<byte> data);
```
#### Parameters

<a name='Promptuarium.ElementBuilder.WithData(System.ReadOnlyMemory_byte_).data'></a>

`data` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

The data

#### Returns
[ElementBuilder](ElementBuilder.md 'Promptuarium\.ElementBuilder')  
The builder

<a name='Promptuarium.ElementBuilder.WithMetaData(System.ReadOnlyMemory_byte_)'></a>

## ElementBuilder\.WithMetaData\(ReadOnlyMemory\<byte\>\) Method

Sets the metadata of the node\.

```csharp
public Promptuarium.ElementBuilder WithMetaData(System.ReadOnlyMemory<byte> metaData);
```
#### Parameters

<a name='Promptuarium.ElementBuilder.WithMetaData(System.ReadOnlyMemory_byte_).metaData'></a>

`metaData` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

The metadata

#### Returns
[ElementBuilder](ElementBuilder.md 'Promptuarium\.ElementBuilder')  
The builder

<a name='Promptuarium.ElementBuilder.WithSignature(System.ReadOnlyMemory_byte_)'></a>

## ElementBuilder\.WithSignature\(ReadOnlyMemory\<byte\>\) Method

Makes the node a signed root: no data, and the signature as metadata, so every stream of the tree starts with the same bytes\.
The content of the file goes into the children\.

```csharp
public Promptuarium.ElementBuilder WithSignature(System.ReadOnlyMemory<byte> signature);
```
#### Parameters

<a name='Promptuarium.ElementBuilder.WithSignature(System.ReadOnlyMemory_byte_).signature'></a>

`signature` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

The signature, 1 to 15 bytes\. It is copied, later changes of the source do not affect the node\.

#### Returns
[ElementBuilder](ElementBuilder.md 'Promptuarium\.ElementBuilder')  
The builder

#### Exceptions

[System\.ArgumentException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentexception 'System\.ArgumentException')  
Thrown if the signature is empty or longer than 15 bytes