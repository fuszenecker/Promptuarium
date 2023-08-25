# Element Constructors

[Home](../../../README.md)

**Containing Type**: [Element](../README.md)

**Assembly**: Promptuarium\.dll

## Overloads

| Constructor | Summary |
| ----------- | ------- |
| [Element()](#3108411760) | Constructor without any data |
| [Element(Element)](#612271537) | Copy constructor |
| [Element(Stream, Stream, Element\[\])](#454853735) | Contractor that also adds child elements |
| [Element(Stream, Stream, IEnumerable\<Element\>)](#1098567175) | LINQ friendly contractor that also adds child elements |

<a id="3108411760"></a>

## Element\(\) 

  
Constructor without any data

```csharp
public Element()
```

<a id="612271537"></a>

## Element\(Element\) 

  
Copy constructor

```csharp
public Element(Promptuarium.Element other)
```

### Parameters

**other** &ensp; [Element](../README.md)

Name of the object to by copied<a id="454853735"></a>

## Element\(Stream, Stream, Element\[\]\) 

  
Contractor that also adds child elements

```csharp
public Element(System.IO.Stream? data, System.IO.Stream? metaData, params Promptuarium.Element[] children)
```

### Parameters

**data** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

Data of the parent node

**metaData** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

**children** &ensp; [Element](../README.md)\[\]

List of child nodes<a id="1098567175"></a>

## Element\(Stream, Stream, IEnumerable\<Element\>\) 

  
LINQ friendly contractor that also adds child elements

```csharp
public Element(System.IO.Stream? data, System.IO.Stream? metaData, System.Collections.Generic.IEnumerable<Promptuarium.Element> children)
```

### Parameters

**data** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

Data of the parent node

**metaData** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

**children** &ensp; [IEnumerable](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1)\<[Element](../README.md)\>

LINQ query of child nodes