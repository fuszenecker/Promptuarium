#### [Promptuarium](README.md 'README')
### [Promptuarium](README.md#Promptuarium 'Promptuarium')

## Data Class

Conversions between values and contents \(data or metadata\)\.

```csharp
public static class Data
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → Data

### Example

```csharp
var node = new Element(Data.FromInt(42), Data.FromUtf8String("answer"));
int value = node.Data.AsInt();
```

### Remarks

The container does not store types: a content is a plain byte sequence, and the reader has to know how it was written.
            All numbers are little-endian, independently of the platform.

The fixed-size conversions (e.g. [AsInt\(this ReadOnlyMemory&lt;byte&gt;\)](Data.md#Promptuarium.Data.AsInt(thisSystem.ReadOnlyMemory_byte_) 'Promptuarium\.Data\.AsInt\(this System\.ReadOnlyMemory\<byte\>\)')) read the first bytes of the content:
            a shorter content is padded with zero bytes, and the bytes after the value are ignored.
### Methods

<a name='Promptuarium.Data.AsAsciiString(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsAsciiString\(this ReadOnlyMemory\<byte\>\) Method

Converts ASCII bytes to a string\.

```csharp
public static string AsAsciiString(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsAsciiString(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='Promptuarium.Data.AsBool(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsBool\(this ReadOnlyMemory\<byte\>\) Method

Converts a content to a boolean value: true if the first byte is not zero\.

```csharp
public static bool AsBool(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsBool(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

<a name='Promptuarium.Data.AsByte(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsByte\(this ReadOnlyMemory\<byte\>\) Method

Converts a content to a byte \(0 for an empty content\)\.

```csharp
public static byte AsByte(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsByte(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')

<a name='Promptuarium.Data.AsByteArray(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsByteArray\(this ReadOnlyMemory\<byte\>\) Method

Copies a content into a new array\.

```csharp
public static byte[] AsByteArray(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsByteArray(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[\[\]](https://learn.microsoft.com/en-us/dotnet/api/system.array 'System\.Array')

<a name='Promptuarium.Data.AsChar(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsChar\(this ReadOnlyMemory\<byte\>\) Method

Converts a content to a character \(a UTF\-16 code unit\)\.

```csharp
public static char AsChar(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsChar(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.Char](https://learn.microsoft.com/en-us/dotnet/api/system.char 'System\.Char')

<a name='Promptuarium.Data.AsDateTime(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsDateTime\(this ReadOnlyMemory\<byte\>\) Method

Converts a content to a date and time\.

```csharp
public static System.DateTime AsDateTime(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsDateTime(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.DateTime](https://learn.microsoft.com/en-us/dotnet/api/system.datetime 'System\.DateTime')

<a name='Promptuarium.Data.AsDateTimeOffset(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsDateTimeOffset\(this ReadOnlyMemory\<byte\>\) Method

Converts a content to a date and time with offset\.

```csharp
public static System.DateTimeOffset AsDateTimeOffset(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsDateTimeOffset(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.DateTimeOffset](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset 'System\.DateTimeOffset')

<a name='Promptuarium.Data.AsDecimal(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsDecimal\(this ReadOnlyMemory\<byte\>\) Method

Converts a content to a decimal number\.

```csharp
public static decimal AsDecimal(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsDecimal(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

<a name='Promptuarium.Data.AsDouble(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsDouble\(this ReadOnlyMemory\<byte\>\) Method

Converts a content to a double precision number\.

```csharp
public static double AsDouble(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsDouble(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.Double](https://learn.microsoft.com/en-us/dotnet/api/system.double 'System\.Double')

<a name='Promptuarium.Data.AsElement(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsElement\(this ReadOnlyMemory\<byte\>\) Method

Converts a content \(a nested Promptuarium stream\) to a tree\.

```csharp
public static Promptuarium.Element AsElement(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsElement(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[Element](Element.md 'Promptuarium\.Element')

#### Exceptions

[PromptuariumException](PromptuariumException.md 'Promptuarium\.PromptuariumException')  
Thrown if the content is not a valid Promptuarium stream

<a name='Promptuarium.Data.AsElementAsync(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsElementAsync\(this ReadOnlyMemory\<byte\>\) Method

Converts a content to a tree, see [AsElement\(this ReadOnlyMemory&lt;byte&gt;\)](Data.md#Promptuarium.Data.AsElement(thisSystem.ReadOnlyMemory_byte_) 'Promptuarium\.Data\.AsElement\(this System\.ReadOnlyMemory\<byte\>\)')\.

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> AsElementAsync(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsElementAsync(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[Element](Element.md 'Promptuarium\.Element')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')

<a name='Promptuarium.Data.AsFloat(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsFloat\(this ReadOnlyMemory\<byte\>\) Method

Converts a content to a single precision number\.

```csharp
public static float AsFloat(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsFloat(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.Single](https://learn.microsoft.com/en-us/dotnet/api/system.single 'System\.Single')

<a name='Promptuarium.Data.AsGuid(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsGuid\(this ReadOnlyMemory\<byte\>\) Method

Converts a content of exactly 16 bytes to a GUID\.

```csharp
public static System.Guid AsGuid(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsGuid(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

<a name='Promptuarium.Data.AsInt(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsInt\(this ReadOnlyMemory\<byte\>\) Method

Converts a content to a 32\-bit integer\.

```csharp
public static int AsInt(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsInt(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='Promptuarium.Data.AsLong(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsLong\(this ReadOnlyMemory\<byte\>\) Method

Converts a content to a 64\-bit integer\.

```csharp
public static long AsLong(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsLong(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.Int64](https://learn.microsoft.com/en-us/dotnet/api/system.int64 'System\.Int64')

<a name='Promptuarium.Data.AsShort(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsShort\(this ReadOnlyMemory\<byte\>\) Method

Converts a content to a 16\-bit integer\.

```csharp
public static short AsShort(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsShort(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.Int16](https://learn.microsoft.com/en-us/dotnet/api/system.int16 'System\.Int16')

<a name='Promptuarium.Data.AsTimeSpan(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsTimeSpan\(this ReadOnlyMemory\<byte\>\) Method

Converts a content to a time span\.

```csharp
public static System.TimeSpan AsTimeSpan(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsTimeSpan(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.TimeSpan](https://learn.microsoft.com/en-us/dotnet/api/system.timespan 'System\.TimeSpan')

<a name='Promptuarium.Data.AsUInt(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsUInt\(this ReadOnlyMemory\<byte\>\) Method

Converts a content to an unsigned 32\-bit integer\.

```csharp
public static uint AsUInt(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsUInt(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.UInt32](https://learn.microsoft.com/en-us/dotnet/api/system.uint32 'System\.UInt32')

<a name='Promptuarium.Data.AsULong(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsULong\(this ReadOnlyMemory\<byte\>\) Method

Converts a content to an unsigned 64\-bit integer\.

```csharp
public static ulong AsULong(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsULong(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.UInt64](https://learn.microsoft.com/en-us/dotnet/api/system.uint64 'System\.UInt64')

<a name='Promptuarium.Data.AsUShort(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsUShort\(this ReadOnlyMemory\<byte\>\) Method

Converts a content to an unsigned 16\-bit integer\.

```csharp
public static ushort AsUShort(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsUShort(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.UInt16](https://learn.microsoft.com/en-us/dotnet/api/system.uint16 'System\.UInt16')

<a name='Promptuarium.Data.AsUtf16String(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsUtf16String\(this ReadOnlyMemory\<byte\>\) Method

Converts UTF\-16 little\-endian bytes to a string\.

```csharp
public static string AsUtf16String(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsUtf16String(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='Promptuarium.Data.AsUtf32String(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsUtf32String\(this ReadOnlyMemory\<byte\>\) Method

Converts UTF\-32 little\-endian bytes to a string\.

```csharp
public static string AsUtf32String(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsUtf32String(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='Promptuarium.Data.AsUtf8String(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsUtf8String\(this ReadOnlyMemory\<byte\>\) Method

Converts UTF\-8 bytes to a string\.

```csharp
public static string AsUtf8String(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsUtf8String(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='Promptuarium.Data.AsVarInt(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsVarInt\(this ReadOnlyMemory\<byte\>\) Method

Converts a content of at most 8 bytes to a signed integer, see [FromVarInt\(long\)](Data.md#Promptuarium.Data.FromVarInt(long) 'Promptuarium\.Data\.FromVarInt\(long\)')\.

```csharp
public static long AsVarInt(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsVarInt(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.Int64](https://learn.microsoft.com/en-us/dotnet/api/system.int64 'System\.Int64')

#### Exceptions

[PromptuariumException](PromptuariumException.md 'Promptuarium\.PromptuariumException')  
Thrown if the content is longer than 8 bytes

<a name='Promptuarium.Data.AsVarUInt(thisSystem.ReadOnlyMemory_byte_)'></a>

## Data\.AsVarUInt\(this ReadOnlyMemory\<byte\>\) Method

Converts a content of at most 8 bytes to an unsigned integer, see [FromVarUInt\(ulong\)](Data.md#Promptuarium.Data.FromVarUInt(ulong) 'Promptuarium\.Data\.FromVarUInt\(ulong\)')\.

```csharp
public static ulong AsVarUInt(this System.ReadOnlyMemory<byte> content);
```
#### Parameters

<a name='Promptuarium.Data.AsVarUInt(thisSystem.ReadOnlyMemory_byte_).content'></a>

`content` [System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

#### Returns
[System\.UInt64](https://learn.microsoft.com/en-us/dotnet/api/system.uint64 'System\.UInt64')

#### Exceptions

[PromptuariumException](PromptuariumException.md 'Promptuarium\.PromptuariumException')  
Thrown if the content is longer than 8 bytes

<a name='Promptuarium.Data.FromAsciiString(string)'></a>

## Data\.FromAsciiString\(string\) Method

Converts a string to ASCII bytes; characters above U\+007F become '?'\.

```csharp
public static System.ReadOnlyMemory<byte> FromAsciiString(string value);
```
#### Parameters

<a name='Promptuarium.Data.FromAsciiString(string).value'></a>

`value` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromBool(bool)'></a>

## Data\.FromBool\(bool\) Method

Converts a boolean value to a content: 01 for true, 00 for false\.

```csharp
public static System.ReadOnlyMemory<byte> FromBool(bool value);
```
#### Parameters

<a name='Promptuarium.Data.FromBool(bool).value'></a>

`value` [System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromByte(byte)'></a>

## Data\.FromByte\(byte\) Method

Converts a byte to a content\.

```csharp
public static System.ReadOnlyMemory<byte> FromByte(byte value);
```
#### Parameters

<a name='Promptuarium.Data.FromByte(byte).value'></a>

`value` [System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromByteArray(byte[])'></a>

## Data\.FromByteArray\(byte\[\]\) Method

Converts bytes to a content\. The bytes are copied, so later changes of the array do not change the content\.

```csharp
public static System.ReadOnlyMemory<byte> FromByteArray(byte[] value);
```
#### Parameters

<a name='Promptuarium.Data.FromByteArray(byte[]).value'></a>

`value` [System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[\[\]](https://learn.microsoft.com/en-us/dotnet/api/system.array 'System\.Array')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromBytes(System.ReadOnlySpan_byte_)'></a>

## Data\.FromBytes\(ReadOnlySpan\<byte\>\) Method

Converts bytes to a content\. The bytes are copied\.

```csharp
public static System.ReadOnlyMemory<byte> FromBytes(System.ReadOnlySpan<byte> value);
```
#### Parameters

<a name='Promptuarium.Data.FromBytes(System.ReadOnlySpan_byte_).value'></a>

`value` [System\.ReadOnlySpan&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlyspan-1 'System\.ReadOnlySpan\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlyspan-1 'System\.ReadOnlySpan\`1')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromChar(char)'></a>

## Data\.FromChar\(char\) Method

Converts a character to a content \(a UTF\-16 code unit, 2 bytes\)\.

```csharp
public static System.ReadOnlyMemory<byte> FromChar(char value);
```
#### Parameters

<a name='Promptuarium.Data.FromChar(char).value'></a>

`value` [System\.Char](https://learn.microsoft.com/en-us/dotnet/api/system.char 'System\.Char')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromDateTime(System.DateTime)'></a>

## Data\.FromDateTime\(DateTime\) Method

Converts a date and time to a content: its ticks \(8 bytes\)\. The kind \(UTC, local\) is not stored\.

```csharp
public static System.ReadOnlyMemory<byte> FromDateTime(System.DateTime value);
```
#### Parameters

<a name='Promptuarium.Data.FromDateTime(System.DateTime).value'></a>

`value` [System\.DateTime](https://learn.microsoft.com/en-us/dotnet/api/system.datetime 'System\.DateTime')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromDateTimeOffset(System.DateTimeOffset)'></a>

## Data\.FromDateTimeOffset\(DateTimeOffset\) Method

Converts a date and time with offset to a content: the ticks of the clock time, then the ticks of the offset \(16 bytes\)\.

```csharp
public static System.ReadOnlyMemory<byte> FromDateTimeOffset(System.DateTimeOffset value);
```
#### Parameters

<a name='Promptuarium.Data.FromDateTimeOffset(System.DateTimeOffset).value'></a>

`value` [System\.DateTimeOffset](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset 'System\.DateTimeOffset')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromDecimal(decimal)'></a>

## Data\.FromDecimal\(decimal\) Method

Converts a decimal number to a content \(16 bytes: lo, mid, hi and flags as 32\-bit integers, like BinaryWriter\.Write\(decimal\)\)\.

```csharp
public static System.ReadOnlyMemory<byte> FromDecimal(decimal value);
```
#### Parameters

<a name='Promptuarium.Data.FromDecimal(decimal).value'></a>

`value` [System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromDouble(double)'></a>

## Data\.FromDouble\(double\) Method

Converts a double precision number to a content \(IEEE 754 binary64, 8 bytes\)\.

```csharp
public static System.ReadOnlyMemory<byte> FromDouble(double value);
```
#### Parameters

<a name='Promptuarium.Data.FromDouble(double).value'></a>

`value` [System\.Double](https://learn.microsoft.com/en-us/dotnet/api/system.double 'System\.Double')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromElement(Promptuarium.Element)'></a>

## Data\.FromElement\(Element\) Method

Converts a tree to a content: a complete nested Promptuarium stream\.

```csharp
public static System.ReadOnlyMemory<byte> FromElement(Promptuarium.Element value);
```
#### Parameters

<a name='Promptuarium.Data.FromElement(Promptuarium.Element).value'></a>

`value` [Element](Element.md 'Promptuarium\.Element')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromElementAsync(Promptuarium.Element)'></a>

## Data\.FromElementAsync\(Element\) Method

Converts a tree to a content, see [FromElement\(Element\)](Data.md#Promptuarium.Data.FromElement(Promptuarium.Element) 'Promptuarium\.Data\.FromElement\(Promptuarium\.Element\)')\.

```csharp
public static System.Threading.Tasks.Task<System.ReadOnlyMemory<byte>> FromElementAsync(Promptuarium.Element value);
```
#### Parameters

<a name='Promptuarium.Data.FromElementAsync(Promptuarium.Element).value'></a>

`value` [Element](Element.md 'Promptuarium\.Element')

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')

<a name='Promptuarium.Data.FromFloat(float)'></a>

## Data\.FromFloat\(float\) Method

Converts a single precision number to a content \(IEEE 754 binary32, 4 bytes\)\.

```csharp
public static System.ReadOnlyMemory<byte> FromFloat(float value);
```
#### Parameters

<a name='Promptuarium.Data.FromFloat(float).value'></a>

`value` [System\.Single](https://learn.microsoft.com/en-us/dotnet/api/system.single 'System\.Single')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromGuid(System.Guid)'></a>

## Data\.FromGuid\(Guid\) Method

Converts a GUID to a content \(16 bytes, the layout of Guid\.ToByteArray\)\.

```csharp
public static System.ReadOnlyMemory<byte> FromGuid(System.Guid value);
```
#### Parameters

<a name='Promptuarium.Data.FromGuid(System.Guid).value'></a>

`value` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromInt(int)'></a>

## Data\.FromInt\(int\) Method

Converts a 32\-bit integer to a content \(4 bytes\)\.

```csharp
public static System.ReadOnlyMemory<byte> FromInt(int value);
```
#### Parameters

<a name='Promptuarium.Data.FromInt(int).value'></a>

`value` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromLong(long)'></a>

## Data\.FromLong\(long\) Method

Converts a 64\-bit integer to a content \(8 bytes\)\.

```csharp
public static System.ReadOnlyMemory<byte> FromLong(long value);
```
#### Parameters

<a name='Promptuarium.Data.FromLong(long).value'></a>

`value` [System\.Int64](https://learn.microsoft.com/en-us/dotnet/api/system.int64 'System\.Int64')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromShort(short)'></a>

## Data\.FromShort\(short\) Method

Converts a 16\-bit integer to a content \(2 bytes\)\.

```csharp
public static System.ReadOnlyMemory<byte> FromShort(short value);
```
#### Parameters

<a name='Promptuarium.Data.FromShort(short).value'></a>

`value` [System\.Int16](https://learn.microsoft.com/en-us/dotnet/api/system.int16 'System\.Int16')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromTimeSpan(System.TimeSpan)'></a>

## Data\.FromTimeSpan\(TimeSpan\) Method

Converts a time span to a content: its ticks \(8 bytes\)\.

```csharp
public static System.ReadOnlyMemory<byte> FromTimeSpan(System.TimeSpan value);
```
#### Parameters

<a name='Promptuarium.Data.FromTimeSpan(System.TimeSpan).value'></a>

`value` [System\.TimeSpan](https://learn.microsoft.com/en-us/dotnet/api/system.timespan 'System\.TimeSpan')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromUInt(uint)'></a>

## Data\.FromUInt\(uint\) Method

Converts an unsigned 32\-bit integer to a content \(4 bytes\)\.

```csharp
public static System.ReadOnlyMemory<byte> FromUInt(uint value);
```
#### Parameters

<a name='Promptuarium.Data.FromUInt(uint).value'></a>

`value` [System\.UInt32](https://learn.microsoft.com/en-us/dotnet/api/system.uint32 'System\.UInt32')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromULong(ulong)'></a>

## Data\.FromULong\(ulong\) Method

Converts an unsigned 64\-bit integer to a content \(8 bytes\)\.

```csharp
public static System.ReadOnlyMemory<byte> FromULong(ulong value);
```
#### Parameters

<a name='Promptuarium.Data.FromULong(ulong).value'></a>

`value` [System\.UInt64](https://learn.microsoft.com/en-us/dotnet/api/system.uint64 'System\.UInt64')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromUShort(ushort)'></a>

## Data\.FromUShort\(ushort\) Method

Converts an unsigned 16\-bit integer to a content \(2 bytes\)\.

```csharp
public static System.ReadOnlyMemory<byte> FromUShort(ushort value);
```
#### Parameters

<a name='Promptuarium.Data.FromUShort(ushort).value'></a>

`value` [System\.UInt16](https://learn.microsoft.com/en-us/dotnet/api/system.uint16 'System\.UInt16')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromUtf16String(string)'></a>

## Data\.FromUtf16String\(string\) Method

Converts a string to UTF\-16 little\-endian bytes \(without byte order mark\)\.

```csharp
public static System.ReadOnlyMemory<byte> FromUtf16String(string value);
```
#### Parameters

<a name='Promptuarium.Data.FromUtf16String(string).value'></a>

`value` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromUtf32String(string)'></a>

## Data\.FromUtf32String\(string\) Method

Converts a string to UTF\-32 little\-endian bytes \(without byte order mark\)\.

```csharp
public static System.ReadOnlyMemory<byte> FromUtf32String(string value);
```
#### Parameters

<a name='Promptuarium.Data.FromUtf32String(string).value'></a>

`value` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromUtf8String(string)'></a>

## Data\.FromUtf8String\(string\) Method

Converts a string to UTF\-8 bytes \(without byte order mark\)\.

```csharp
public static System.ReadOnlyMemory<byte> FromUtf8String(string value);
```
#### Parameters

<a name='Promptuarium.Data.FromUtf8String(string).value'></a>

`value` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromVarInt(long)'></a>

## Data\.FromVarInt\(long\) Method

Converts a signed integer to a content of 1 to 8 bytes: the magnitude shifted left by one bit, the lowest bit is the sign
\(long\.MinValue is "negative zero"\), then encoded as [FromVarUInt\(ulong\)](Data.md#Promptuarium.Data.FromVarUInt(ulong) 'Promptuarium\.Data\.FromVarUInt\(ulong\)')\.

```csharp
public static System.ReadOnlyMemory<byte> FromVarInt(long value);
```
#### Parameters

<a name='Promptuarium.Data.FromVarInt(long).value'></a>

`value` [System\.Int64](https://learn.microsoft.com/en-us/dotnet/api/system.int64 'System\.Int64')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')

<a name='Promptuarium.Data.FromVarUInt(ulong)'></a>

## Data\.FromVarUInt\(ulong\) Method

Converts an unsigned integer to a content of 1 to 8 bytes: little\-endian, without the trailing zero bytes\.

```csharp
public static System.ReadOnlyMemory<byte> FromVarUInt(ulong value);
```
#### Parameters

<a name='Promptuarium.Data.FromVarUInt(ulong).value'></a>

`value` [System\.UInt64](https://learn.microsoft.com/en-us/dotnet/api/system.uint64 'System\.UInt64')

#### Returns
[System\.ReadOnlyMemory&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1 'System\.ReadOnlyMemory\`1')