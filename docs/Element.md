#### [Promptuarium](README.md 'README')
### [Promptuarium](README.md#Promptuarium 'Promptuarium')

## Element Class

A node of a tree: a data, a metadata \(both are byte arrays, empty by default\) and children\.
A tree is saved and loaded as a whole, with its structure; see BINARY\_FORMAT\.md for the binary form\.

```csharp
public class Element : System.Collections.Generic.IEnumerable<Promptuarium.Element>, System.Collections.IEnumerable
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → Element

Implements [System\.Collections\.Generic\.IEnumerable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')[Element](Element.md 'Promptuarium\.Element')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1'), [System\.Collections\.IEnumerable](https://learn.microsoft.com/en-us/dotnet/api/system.collections.ienumerable 'System\.Collections\.IEnumerable')
### Constructors

<a name='Promptuarium.Element.Element()'></a>

## Element\(\) Constructor

Constructor without any data

```csharp
public Element();
```

### Example

```csharp
var node = new Element();
```

### Remarks
The constructor creates an empty node\.

<a name='Promptuarium.Element.Element(Promptuarium.Element)'></a>

## Element\(Element\) Constructor

Copy constructor

```csharp
public Element(Promptuarium.Element other);
```
#### Parameters

<a name='Promptuarium.Element.Element(Promptuarium.Element).other'></a>

`other` [Element](Element.md 'Promptuarium\.Element')

Name of the object to by copied

### Example

```csharp
var copy = new Element(original);
```

### Remarks
The copy constructor creates a deep copy of the object\.

<a name='Promptuarium.Element.Element(System.ReadOnlyMemory_byte_,System.ReadOnlyMemory_byte_,Promptuarium.Element[])'></a>

## Element\(ReadOnlyMemory\<byte\>, ReadOnlyMemory\<byte\>, Element\[\]\) Constructor

Contractor that also adds child elements

```csharp
public Element(System.ReadOnlyMemory<byte> data, System.ReadOnlyMemory<byte> metaData, params Promptuarium.Element[] children);
```
#### Parameters

<a name='Promptuarium.Element.Element(System.ReadOnlyMemory_byte_,System.ReadOnlyMemory_byte_,Promptuarium.Element[]).data'></a>

`data` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

Data of the parent node

<a name='Promptuarium.Element.Element(System.ReadOnlyMemory_byte_,System.ReadOnlyMemory_byte_,Promptuarium.Element[]).metaData'></a>

`metaData` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

Metadata of the parent node

<a name='Promptuarium.Element.Element(System.ReadOnlyMemory_byte_,System.ReadOnlyMemory_byte_,Promptuarium.Element[]).children'></a>

`children` [Element](Element.md 'Promptuarium\.Element')[\[\]](https://learn.microsoft.com/en-us/dotnet/api/system.array 'System\.Array')

List of child nodes

### Example

```csharp
var child1 = new Element();
var child2 = new Element();
var child3 = new Element();

var data = Data.FromUtf8String("Hello world");
var metaData = Data.FromUtf8String("Hello world");

var node = new Element(data, metaData, child1, child2, child3);
```

<a name='Promptuarium.Element.Element(System.ReadOnlyMemory_byte_,System.ReadOnlyMemory_byte_,System.Collections.Generic.IEnumerable_Promptuarium.Element_)'></a>

## Element\(ReadOnlyMemory\<byte\>, ReadOnlyMemory\<byte\>, IEnumerable\<Element\>\) Constructor

LINQ friendly contractor that also adds child elements

```csharp
public Element(System.ReadOnlyMemory<byte> data, System.ReadOnlyMemory<byte> metaData, System.Collections.Generic.IEnumerable<Promptuarium.Element> children);
```
#### Parameters

<a name='Promptuarium.Element.Element(System.ReadOnlyMemory_byte_,System.ReadOnlyMemory_byte_,System.Collections.Generic.IEnumerable_Promptuarium.Element_).data'></a>

`data` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

Data of the parent node

<a name='Promptuarium.Element.Element(System.ReadOnlyMemory_byte_,System.ReadOnlyMemory_byte_,System.Collections.Generic.IEnumerable_Promptuarium.Element_).metaData'></a>

`metaData` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

Metadata of the parent node

<a name='Promptuarium.Element.Element(System.ReadOnlyMemory_byte_,System.ReadOnlyMemory_byte_,System.Collections.Generic.IEnumerable_Promptuarium.Element_).children'></a>

`children` [System\.Collections\.Generic\.IEnumerable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')[Element](Element.md 'Promptuarium\.Element')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')

LINQ query of child nodes

### Example

```csharp
var data = Data.FromUtf8String("Hello world");
var metaData = Data.FromInt32(123);

var tree = new Element(data, metaData,
   Enumerable.Range(0, 10).Select(i => new Element(Data.FromVarInt(i))));
```
### Properties

<a name='Promptuarium.Element.Children'></a>

## Element\.Children Property

List of children\. it is never null, but can be empty\.
The list is read\-only, use the tree operations \(Add, Remove, Detach\) to change it\.

```csharp
public System.Collections.Generic.IReadOnlyList<Promptuarium.Element> Children { get; }
```

#### Property Value
[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[Element](Element.md 'Promptuarium\.Element')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')

<a name='Promptuarium.Element.Data'></a>

## Element\.Data Property

The data of the node \(empty by default\)\. An absent and an empty content are the same\.

```csharp
public System.ReadOnlyMemory<byte> Data { get; set; }
```

#### Property Value
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

### Remarks
A byte array can be assigned directly \(it is not copied\); [Data](Data.md 'Promptuarium\.Data') converts values to contents\.

<a name='Promptuarium.Element.MetaData'></a>

## Element\.MetaData Property

The metadata of the node \(empty by default\)\. An absent and an empty content are the same\.

```csharp
public System.ReadOnlyMemory<byte> MetaData { get; set; }
```

#### Property Value
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

### Remarks
A byte array can be assigned directly \(it is not copied\); [Data](Data.md 'Promptuarium\.Data') converts values to contents\.

<a name='Promptuarium.Element.Parent'></a>

## Element\.Parent Property

Reference to the parent node; root node has null as parent\.
It is maintained by the tree operations \(Add, Remove, Detach\)\.

```csharp
public Promptuarium.Element? Parent { get; }
```

#### Property Value
[Element](Element.md 'Promptuarium\.Element')

<a name='Promptuarium.Element.this[int]'></a>

## Element\.this\[int\] Property

Get an item by index\.

```csharp
public Promptuarium.Element this[int index] { get; }
```
#### Parameters

<a name='Promptuarium.Element.this[int].index'></a>

`index` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The index\.

#### Property Value
[Element](Element.md 'Promptuarium\.Element')

#### Exceptions

[System\.ArgumentOutOfRangeException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentoutofrangeexception 'System\.ArgumentOutOfRangeException')  
If the index is out of range

### Example

```csharp
var node = tree[0];
```
### Methods

<a name='Promptuarium.Element.Add(Promptuarium.Element[])'></a>

## Element\.Add\(Element\[\]\) Method

Adds node\(s\) to the tree\. A node that has another parent is moved here\.

```csharp
public Promptuarium.Element Add(params Promptuarium.Element[] nodes);
```
#### Parameters

<a name='Promptuarium.Element.Add(Promptuarium.Element[]).nodes'></a>

`nodes` [Element](Element.md 'Promptuarium\.Element')[\[\]](https://learn.microsoft.com/en-us/dotnet/api/system.array 'System\.Array')

The nodes to be added

#### Returns
[Element](Element.md 'Promptuarium\.Element')

#### Exceptions

[PromptuariumException](PromptuariumException.md 'Promptuarium\.PromptuariumException')  
Thrown if the node is already a child of this node, or if it is this node or one of its ancestors

### Example

```csharp
var tree = new Element();

var node1 = new Element();
var node2 = new Element();
var node3 = new Element();

tree.Add(node1, node2, node3);
```

```csharp
var tree = new Element()
{
    new Element(),
    new Element(),
    new Element()
};
```

<a name='Promptuarium.Element.Add(System.Collections.Generic.IEnumerable_Promptuarium.Element_)'></a>

## Element\.Add\(IEnumerable\<Element\>\) Method

Adds node\(s\) to the tree\. A node that has another parent is moved here\.

```csharp
public Promptuarium.Element Add(System.Collections.Generic.IEnumerable<Promptuarium.Element> nodes);
```
#### Parameters

<a name='Promptuarium.Element.Add(System.Collections.Generic.IEnumerable_Promptuarium.Element_).nodes'></a>

`nodes` [System\.Collections\.Generic\.IEnumerable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')[Element](Element.md 'Promptuarium\.Element')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')

The nodes to be added

#### Returns
[Element](Element.md 'Promptuarium\.Element')

#### Exceptions

[PromptuariumException](PromptuariumException.md 'Promptuarium\.PromptuariumException')  
Thrown if the node already exists in the tree

### Example

```csharp
var tree = new Element();

var node1 = new Element();
var node2 = new Element();
var node3 = new Element();

tree.Add(node1, node2, node3);
```

<a name='Promptuarium.Element.Detach()'></a>

## Element\.Detach\(\) Method

Detaches a subtree or node\.

```csharp
public Promptuarium.Element Detach();
```

#### Returns
[Element](Element.md 'Promptuarium\.Element')  
The node itself

### Example

```csharp
var tree = new Element();

var node1 = new Element();
var node2 = new Element();
var node3 = new Element();

tree.Add(node1, node2, node3);

var detachedNode = node2.Detach();
```

<a name='Promptuarium.Element.Detach(Promptuarium.Element)'></a>

## Element\.Detach\(Element\) Method

Detaches a subtree or node\.

```csharp
public Promptuarium.Element Detach(Promptuarium.Element node);
```
#### Parameters

<a name='Promptuarium.Element.Detach(Promptuarium.Element).node'></a>

`node` [Element](Element.md 'Promptuarium\.Element')

The node to be detached

#### Returns
[Element](Element.md 'Promptuarium\.Element')  
The node itself

#### Exceptions

[PromptuariumException](PromptuariumException.md 'Promptuarium\.PromptuariumException')  
Thrown if the node is not a child of this node

### Example

```csharp
var tree = new Element();

var node1 = new Element();
var node2 = new Element();
var node3 = new Element();

tree.Add(node1, node2, node3);

var detachedNode = tree.Detach(node2);
```

<a name='Promptuarium.Element.FromBase64String(string)'></a>

## Element\.FromBase64String\(string\) Method

Creates a tree from a Base64 string\.

```csharp
public static Promptuarium.Element FromBase64String(string base64String);
```
#### Parameters

<a name='Promptuarium.Element.FromBase64String(string).base64String'></a>

`base64String` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The tree in Base64\.

#### Returns
[Element](Element.md 'Promptuarium\.Element')  
The tree\.

### Example

```csharp
var tree = Element.FromBase64String(base64String);
```

<a name='Promptuarium.Element.FromBase64StringAsync(string)'></a>

## Element\.FromBase64StringAsync\(string\) Method

Creates a tree from a Base64 string\.

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> FromBase64StringAsync(string base64String);
```
#### Parameters

<a name='Promptuarium.Element.FromBase64StringAsync(string).base64String'></a>

`base64String` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The tree in Base64\.

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[Element](Element.md 'Promptuarium\.Element')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')  
The tree\.

### Example

```csharp
var tree = await Element.FromBase64StringAsync(base64String);
```

<a name='Promptuarium.Element.FromBase64StringAsync(string,System.Threading.CancellationToken)'></a>

## Element\.FromBase64StringAsync\(string, CancellationToken\) Method

Creates a tree from a Base64 string\.

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> FromBase64StringAsync(string base64String, System.Threading.CancellationToken cancellationToken);
```
#### Parameters

<a name='Promptuarium.Element.FromBase64StringAsync(string,System.Threading.CancellationToken).base64String'></a>

`base64String` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The tree in Base64\.

<a name='Promptuarium.Element.FromBase64StringAsync(string,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

The cancellation token\.

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[Element](Element.md 'Promptuarium\.Element')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')  
The tree\.

### Example

```csharp
var tree = await Element.FromBase64StringAsync(base64String);
```

<a name='Promptuarium.Element.FromBytes(System.ReadOnlyMemory_byte_,Promptuarium.LoadOptions)'></a>

## Element\.FromBytes\(ReadOnlyMemory\<byte\>, LoadOptions\) Method

Decodes a tree from bytes\. Further data \(e\.g\. another tree\) may follow the tree\.

```csharp
public static Promptuarium.Element FromBytes(System.ReadOnlyMemory<byte> bytes, Promptuarium.LoadOptions? options=null);
```
#### Parameters

<a name='Promptuarium.Element.FromBytes(System.ReadOnlyMemory_byte_,Promptuarium.LoadOptions).bytes'></a>

`bytes` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

The bytes

<a name='Promptuarium.Element.FromBytes(System.ReadOnlyMemory_byte_,Promptuarium.LoadOptions).options'></a>

`options` [LoadOptions](LoadOptions.md 'Promptuarium\.LoadOptions')

The limits, or null

#### Returns
[Element](Element.md 'Promptuarium\.Element')  
The tree

#### Exceptions

[PromptuariumException](PromptuariumException.md 'Promptuarium\.PromptuariumException')  
Thrown if the bytes are not a valid Promptuarium stream, or if they exceed a limit

### Example

```csharp
var tree = Element.FromBytes(bytes);
```

<a name='Promptuarium.Element.FromBytes(System.ReadOnlyMemory_byte_,Promptuarium.LoadOptions,int)'></a>

## Element\.FromBytes\(ReadOnlyMemory\<byte\>, LoadOptions, int\) Method

Decodes a tree from bytes, and tells where the tree ends \(further trees may follow from there\)\.

```csharp
public static Promptuarium.Element FromBytes(System.ReadOnlyMemory<byte> bytes, Promptuarium.LoadOptions? options, out int consumed);
```
#### Parameters

<a name='Promptuarium.Element.FromBytes(System.ReadOnlyMemory_byte_,Promptuarium.LoadOptions,int).bytes'></a>

`bytes` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

The bytes

<a name='Promptuarium.Element.FromBytes(System.ReadOnlyMemory_byte_,Promptuarium.LoadOptions,int).options'></a>

`options` [LoadOptions](LoadOptions.md 'Promptuarium\.LoadOptions')

The limits, or null

<a name='Promptuarium.Element.FromBytes(System.ReadOnlyMemory_byte_,Promptuarium.LoadOptions,int).consumed'></a>

`consumed` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The number of bytes of the tree, up to and including the terminator

#### Returns
[Element](Element.md 'Promptuarium\.Element')  
The tree

#### Exceptions

[PromptuariumException](PromptuariumException.md 'Promptuarium\.PromptuariumException')  
Thrown if the bytes are not a valid Promptuarium stream, or if they exceed a limit

### Example

```csharp
var first = Element.FromBytes(bytes, null, out int consumed);
var second = Element.FromBytes(bytes[consumed..]);
```

<a name='Promptuarium.Element.GetEnumerator()'></a>

## Element\.GetEnumerator\(\) Method

Returns an enumerator that iterates through the Children

```csharp
public System.Collections.Generic.IEnumerator<Promptuarium.Element> GetEnumerator();
```

Implements [GetEnumerator\(\)](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1.getenumerator 'System\.Collections\.Generic\.IEnumerable\`1\.GetEnumerator'), [GetEnumerator\(\)](https://learn.microsoft.com/en-us/dotnet/api/system.collections.ienumerable.getenumerator 'System\.Collections\.IEnumerable\.GetEnumerator')

#### Returns
[System\.Collections\.Generic\.IEnumerator&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerator-1 'System\.Collections\.Generic\.IEnumerator\`1')[Element](Element.md 'Promptuarium\.Element')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerator-1 'System\.Collections\.Generic\.IEnumerator\`1')  
The enumerator of children

### Example

```csharp
var tree = new Element();

var node1 = new Element();
var node2 = new Element();
var node3 = new Element();

tree.Add(node1, node2, node3);

foreach (var node in tree)
{
   Console.WriteLine(node.ToString());
}
```

<a name='Promptuarium.Element.GetStatistics()'></a>

## Element\.GetStatistics\(\) Method

Gets the statistics about the tree

```csharp
public Promptuarium.Statistics GetStatistics();
```

#### Returns
[Statistics](Statistics.md 'Promptuarium\.Statistics')  
The statistics

### Example

```csharp
var statistics = tree.GetStatistics();
Console.WriteLine(statistics.ToString());
```

### Remarks
This method walks the tree and collects statistics about the tree\.

<a name='Promptuarium.Element.Insert(int,Promptuarium.Element)'></a>

## Element\.Insert\(int, Element\) Method

Inserts a node at the given position of the children\. A node that has another parent is moved here\.

```csharp
public Promptuarium.Element Insert(int index, Promptuarium.Element node);
```
#### Parameters

<a name='Promptuarium.Element.Insert(int,Promptuarium.Element).index'></a>

`index` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The position of the new child, from 0 to the number of children \(to append\)

<a name='Promptuarium.Element.Insert(int,Promptuarium.Element).node'></a>

`node` [Element](Element.md 'Promptuarium\.Element')

The node to be inserted

#### Returns
[Element](Element.md 'Promptuarium\.Element')  
The tree itself

#### Exceptions

[System\.ArgumentOutOfRangeException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentoutofrangeexception 'System\.ArgumentOutOfRangeException')  
Thrown if the index is out of range

[PromptuariumException](PromptuariumException.md 'Promptuarium\.PromptuariumException')  
Thrown if the node is already a child of this node, or if it is this node or one of its ancestors

### Example

```csharp
var tree = new Element(null, null, node1, node3);
tree.Insert(1, node2);
```

<a name='Promptuarium.Element.LoadAsync(string)'></a>

## Element\.LoadAsync\(string\) Method

Loads a tree from a file\.

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> LoadAsync(string fileName);
```
#### Parameters

<a name='Promptuarium.Element.LoadAsync(string).fileName'></a>

`fileName` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The source file name

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[Element](Element.md 'Promptuarium\.Element')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')  
The tree, if no error occurred

#### Exceptions

[PromptuariumException](PromptuariumException.md 'Promptuarium\.PromptuariumException')  
Thrown if the file is not a valid Promptuarium file

### Example

```csharp
var tree = await Element.LoadAsync("test.prm");
```

<a name='Promptuarium.Element.LoadAsync(string,Promptuarium.LoadOptions,System.Threading.CancellationToken)'></a>

## Element\.LoadAsync\(string, LoadOptions, CancellationToken\) Method

Loads a tree from a file, with limits\.

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> LoadAsync(string fileName, Promptuarium.LoadOptions? options, System.Threading.CancellationToken cancellationToken);
```
#### Parameters

<a name='Promptuarium.Element.LoadAsync(string,Promptuarium.LoadOptions,System.Threading.CancellationToken).fileName'></a>

`fileName` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The source file name

<a name='Promptuarium.Element.LoadAsync(string,Promptuarium.LoadOptions,System.Threading.CancellationToken).options'></a>

`options` [LoadOptions](LoadOptions.md 'Promptuarium\.LoadOptions')

The limits, or null

<a name='Promptuarium.Element.LoadAsync(string,Promptuarium.LoadOptions,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

The cancellation token

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[Element](Element.md 'Promptuarium\.Element')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')  
The tree, if no error occurred

#### Exceptions

[PromptuariumException](PromptuariumException.md 'Promptuarium\.PromptuariumException')  
Thrown if the file is not a valid Promptuarium file, or if it exceeds a limit

### Example

```csharp
var tree = await Element.LoadAsync("test.prm", new LoadOptions { MaxNodes = 1000 }, cancellationToken);
```

<a name='Promptuarium.Element.LoadAsync(string,System.Threading.CancellationToken)'></a>

## Element\.LoadAsync\(string, CancellationToken\) Method

Loads a tree from a file\.

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> LoadAsync(string fileName, System.Threading.CancellationToken cancellationToken);
```
#### Parameters

<a name='Promptuarium.Element.LoadAsync(string,System.Threading.CancellationToken).fileName'></a>

`fileName` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The source file name

<a name='Promptuarium.Element.LoadAsync(string,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

The cancellation token

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[Element](Element.md 'Promptuarium\.Element')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')  
The tree, if no error occurred

#### Exceptions

[PromptuariumException](PromptuariumException.md 'Promptuarium\.PromptuariumException')  
Thrown if the file is not a valid Promptuarium file

### Example

```csharp
var tree = await Element.LoadAsync("test.prm", cancellationToken);
```

<a name='Promptuarium.Element.LoadAsync(System.IO.Stream)'></a>

## Element\.LoadAsync\(Stream\) Method

Loads a tree from a stream\.

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> LoadAsync(System.IO.Stream stream);
```
#### Parameters

<a name='Promptuarium.Element.LoadAsync(System.IO.Stream).stream'></a>

`stream` [System\.IO\.Stream](https://learn.microsoft.com/en-us/dotnet/api/system.io.stream 'System\.IO\.Stream')

The source stream

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[Element](Element.md 'Promptuarium\.Element')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')  
The tree, if no error occurred

#### Exceptions

[PromptuariumException](PromptuariumException.md 'Promptuarium\.PromptuariumException')  
Thrown if the stream is not a valid Promptuarium stream

### Example

```csharp
using var stream = new FileStream("test.prm", FileMode.Open);
var tree = await Element.LoadAsync(stream);
```

<a name='Promptuarium.Element.LoadAsync(System.IO.Stream,Promptuarium.LoadOptions,System.Threading.CancellationToken)'></a>

## Element\.LoadAsync\(Stream, LoadOptions, CancellationToken\) Method

Loads a tree from a stream, with limits\.

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> LoadAsync(System.IO.Stream stream, Promptuarium.LoadOptions? options, System.Threading.CancellationToken cancellationToken);
```
#### Parameters

<a name='Promptuarium.Element.LoadAsync(System.IO.Stream,Promptuarium.LoadOptions,System.Threading.CancellationToken).stream'></a>

`stream` [System\.IO\.Stream](https://learn.microsoft.com/en-us/dotnet/api/system.io.stream 'System\.IO\.Stream')

The source stream

<a name='Promptuarium.Element.LoadAsync(System.IO.Stream,Promptuarium.LoadOptions,System.Threading.CancellationToken).options'></a>

`options` [LoadOptions](LoadOptions.md 'Promptuarium\.LoadOptions')

The limits, or null

<a name='Promptuarium.Element.LoadAsync(System.IO.Stream,Promptuarium.LoadOptions,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

The cancellation token

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[Element](Element.md 'Promptuarium\.Element')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')  
The tree, if no error occurred

#### Exceptions

[PromptuariumException](PromptuariumException.md 'Promptuarium\.PromptuariumException')  
Thrown if the stream is not a valid Promptuarium stream, or if it exceeds a limit

### Example

```csharp
var options = new LoadOptions { MaxBytes = 10 * 1024 * 1024, MaxNodes = 100_000 };
var tree = await Element.LoadAsync(networkStream, options, cancellationToken);
```

<a name='Promptuarium.Element.LoadAsync(System.IO.Stream,System.Threading.CancellationToken)'></a>

## Element\.LoadAsync\(Stream, CancellationToken\) Method

Loads a tree from a stream\.

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> LoadAsync(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken);
```
#### Parameters

<a name='Promptuarium.Element.LoadAsync(System.IO.Stream,System.Threading.CancellationToken).stream'></a>

`stream` [System\.IO\.Stream](https://learn.microsoft.com/en-us/dotnet/api/system.io.stream 'System\.IO\.Stream')

The source stream

<a name='Promptuarium.Element.LoadAsync(System.IO.Stream,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

The cancellation token

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[Element](Element.md 'Promptuarium\.Element')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')  
The tree, if no error occurred

#### Exceptions

[PromptuariumException](PromptuariumException.md 'Promptuarium\.PromptuariumException')  
Thrown if the stream is not a valid Promptuarium stream

### Example

```csharp
using var stream = new FileStream("test.prm", FileMode.Open);
var tree = await Element.LoadAsync(stream, cancellationToken);
```

<a name='Promptuarium.Element.Remove(Promptuarium.Element)'></a>

## Element\.Remove\(Element\) Method

Removes a subtree or node recursively\.

```csharp
public Promptuarium.Element Remove(Promptuarium.Element node);
```
#### Parameters

<a name='Promptuarium.Element.Remove(Promptuarium.Element).node'></a>

`node` [Element](Element.md 'Promptuarium\.Element')

The node to be removed

#### Returns
[Element](Element.md 'Promptuarium\.Element')

#### Exceptions

[PromptuariumException](PromptuariumException.md 'Promptuarium\.PromptuariumException')  
Thrown if the node is not a child of this node

### Example

```csharp
var tree = new Element();

var node1 = new Element();
var node2 = new Element();
var node3 = new Element();

tree.Add(node1, node2, node3);

tree.Remove(node2);
```

<a name='Promptuarium.Element.SaveAsync(string)'></a>

## Element\.SaveAsync\(string\) Method

Saves the tree into a file\.

```csharp
public System.Threading.Tasks.Task SaveAsync(string fileName);
```
#### Parameters

<a name='Promptuarium.Element.SaveAsync(string).fileName'></a>

`fileName` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The target file name

#### Returns
[System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task')

### Example

```csharp
await tree.SaveAsync("test.prm");
```

<a name='Promptuarium.Element.SaveAsync(string,System.Threading.CancellationToken)'></a>

## Element\.SaveAsync\(string, CancellationToken\) Method

Saves the tree into a file\.

```csharp
public System.Threading.Tasks.Task SaveAsync(string fileName, System.Threading.CancellationToken cancellationToken);
```
#### Parameters

<a name='Promptuarium.Element.SaveAsync(string,System.Threading.CancellationToken).fileName'></a>

`fileName` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The target file name

<a name='Promptuarium.Element.SaveAsync(string,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

The cancellation token

#### Returns
[System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task')

### Example

```csharp
await tree.SaveAsync("test.prm", cancellationToken);
```

<a name='Promptuarium.Element.SaveAsync(System.IO.Stream)'></a>

## Element\.SaveAsync\(Stream\) Method

Saves the tree into a stream\.

```csharp
public System.Threading.Tasks.Task SaveAsync(System.IO.Stream stream);
```
#### Parameters

<a name='Promptuarium.Element.SaveAsync(System.IO.Stream).stream'></a>

`stream` [System\.IO\.Stream](https://learn.microsoft.com/en-us/dotnet/api/system.io.stream 'System\.IO\.Stream')

The target stream

#### Returns
[System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task')

### Example

```csharp
using var stream = new FileStream("test.prm", FileMode.Create);
await tree.SaveAsync(stream);
```

<a name='Promptuarium.Element.SaveAsync(System.IO.Stream,System.Threading.CancellationToken)'></a>

## Element\.SaveAsync\(Stream, CancellationToken\) Method

Saves the tree into a stream\.

```csharp
public System.Threading.Tasks.Task SaveAsync(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken);
```
#### Parameters

<a name='Promptuarium.Element.SaveAsync(System.IO.Stream,System.Threading.CancellationToken).stream'></a>

`stream` [System\.IO\.Stream](https://learn.microsoft.com/en-us/dotnet/api/system.io.stream 'System\.IO\.Stream')

The target stream

<a name='Promptuarium.Element.SaveAsync(System.IO.Stream,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

The cancellation token

#### Returns
[System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task')

### Example

```csharp
using var stream = new FileStream("test.prm", FileMode.Create);
await tree.SaveAsync(stream, cancellationToken);
```

<a name='Promptuarium.Element.StructurallyEquals(Promptuarium.Element)'></a>

## Element\.StructurallyEquals\(Element\) Method

Compares the structure and the contents of two trees: the data, the metadata and the children \(in order\) of every node\.

```csharp
public bool StructurallyEquals(Promptuarium.Element? other);
```
#### Parameters

<a name='Promptuarium.Element.StructurallyEquals(Promptuarium.Element).other'></a>

`other` [Element](Element.md 'Promptuarium\.Element')

The other tree

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')  
True, if the two trees have the same structure and contents

### Example

```csharp
var loaded = await Element.LoadAsync(stream);
bool same = tree.StructurallyEquals(loaded);
```

### Remarks
An absent \(null\) and an empty content are equal, as in the binary format\. The parents are not compared,
so a subtree can be compared to a standalone tree\.
\(Equals is not overridden: a tree is mutable, and the tree operations identify the nodes by reference\.\)

<a name='Promptuarium.Element.ToBase64String()'></a>

## Element\.ToBase64String\(\) Method

Converts the tree to a Base64 string\.

```csharp
public string ToBase64String();
```

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')  
The tree in Base64\.

### Example

```csharp
var base64String = tree.ToBase64String();
```

<a name='Promptuarium.Element.ToBase64StringAsync()'></a>

## Element\.ToBase64StringAsync\(\) Method

Converts the tree to Base64 string\.

```csharp
public System.Threading.Tasks.Task<string> ToBase64StringAsync();
```

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')  
The tree in Base64\.

### Example

```csharp
var tree = new Element();

var node1 = new Element();

var node2 = new Element();
node2.Data = Data.FromUtf8String("Hello world");

var node3 = new Element();
node3.Data = Data.FromUtf8String("Hello world");

tree.Add(node1, node2, node3);

var base64String = await tree.ToBase64StringAsync();
```

<a name='Promptuarium.Element.ToBase64StringAsync(System.Threading.CancellationToken)'></a>

## Element\.ToBase64StringAsync\(CancellationToken\) Method

Converts the tree to Base64 string\.

```csharp
public System.Threading.Tasks.Task<string> ToBase64StringAsync(System.Threading.CancellationToken cancellationToken);
```
#### Parameters

<a name='Promptuarium.Element.ToBase64StringAsync(System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

The cancellation token\.

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')  
The tree in Base64\.

### Example

```csharp
var tree = new Element();

var node1 = new Element();

var node2 = new Element();
node2.Data = Data.FromUtf8String("Hello world");

var node3 = new Element();
node3.Data = Data.FromUtf8String("Hello world");

tree.Add(node1, node2, node3);

var base64String = await tree.ToBase64StringAsync();
```

<a name='Promptuarium.Element.ToBytes()'></a>

## Element\.ToBytes\(\) Method

Encodes the tree into its canonical byte sequence \(BINARY\_FORMAT\.md, section 6\)\.

```csharp
public byte[] ToBytes();
```

#### Returns
[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[\[\]](https://learn.microsoft.com/en-us/dotnet/api/system.array 'System\.Array')  
A new array, exactly as long as the encoding

### Example

```csharp
byte[] bytes = tree.ToBytes();
```

<a name='Promptuarium.Element.ToString()'></a>

## Element\.ToString\(\) Method

Converts the element to string \(for debugging purposes\)\.

```csharp
public override string ToString();
```

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

### Example

```csharp
var tree = new Element();
tree.Data = Data.FromUtf8String("Hello world");
var nodeString = tree.ToString();
```

<a name='Promptuarium.Element.TreeToString(string)'></a>

## Element\.TreeToString\(string\) Method

Converts the whole tree to string \(for debugging purposes\)\.

```csharp
public string TreeToString(string tabulator=">");
```
#### Parameters

<a name='Promptuarium.Element.TreeToString(string).tabulator'></a>

`tabulator` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

An optional tabulator for conversion\.

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

### Example

```csharp
var tree = new Element();

var node1 = new Element();
    
var node2 = new Element();
node2.Data = Data.FromUtf8String("Hello world");

var node3 = new Element();
node3.Data = Data.FromUtf8String("Hello world");

tree.Add(node1, node2, node3);

var treeString = tree.TreeToString();
```

<a name='Promptuarium.Element.Walk(Promptuarium.Element.WalkHandler)'></a>

## Element\.Walk\(WalkHandler\) Method

Walks through the tree, and calls the handler for each node\.

```csharp
public Promptuarium.Element Walk(Promptuarium.Element.WalkHandler handler);
```
#### Parameters

<a name='Promptuarium.Element.Walk(Promptuarium.Element.WalkHandler).handler'></a>

`handler` [WalkHandler\(Element, IReadOnlyCollection&lt;Element&gt;\)](Element.WalkHandler(Element,IReadOnlyCollection_Element_).md 'Promptuarium\.Element\.WalkHandler\(Promptuarium\.Element, System\.Collections\.Generic\.IReadOnlyCollection\<Promptuarium\.Element\>\)')

The method to be called for each nodes

#### Returns
[Element](Element.md 'Promptuarium\.Element')  
The node itself

### Example

```csharp
var tree = new Element();

var node1 = new Element();
var node2 = new Element();
var node3 = new Element();

tree.Add(node1, node2, node3);

tree.Walk((Element node, IReadOnlyCollection<Element> ancestors) =>
   {
       Console.WriteLine(node);
   });
```
### Explicit Interface Implementations

<a name='Promptuarium.Element.System.Collections.IEnumerable.GetEnumerator()'></a>

## System\.Collections\.IEnumerable\.GetEnumerator\(\) Method

Returns an enumerator that iterates through the Children

```csharp
System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator();
```

Implements [GetEnumerator\(\)](https://learn.microsoft.com/en-us/dotnet/api/system.collections.ienumerable.getenumerator 'System\.Collections\.IEnumerable\.GetEnumerator')

### Example

```csharp
var tree = new Element();

var node1 = new Element();
var node2 = new Element();
var node3 = new Element();

tree.Add(node1, node2, node3);

foreach (var node in tree)
{
  Console.WriteLine(node.ToString());
}
```