# Element\.LoadAsync Method

[Home](../../../README.md)

**Containing Type**: [Element](../README.md)

**Assembly**: Promptuarium\.dll

## Overloads

| Method | Summary |
| ------ | ------- |
| [LoadAsync(Stream, CancellationToken)](#2169171820) | Loads a tree from a stream\. |
| [LoadAsync(Stream)](#2517542937) | Loads a tree from a stream\. |
| [LoadAsync(String, CancellationToken)](#25283078) | Loads a tree from a file\. |
| [LoadAsync(String)](#1926306021) | Loads a tree from a file\. |

<a id="2169171820"></a>

## LoadAsync\(Stream, CancellationToken\) 

  
Loads a tree from a stream\.

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> LoadAsync(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

**cancellationToken** &ensp; [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken)

The cancellation token

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1)\<[Element](../README.md)\>

The tree, if no error occurred

### Exceptions

[PromptuariumException](../../PromptuariumException/README.md)

Thrown if the stream is not a valid Promptuarium stream

### Examples

```
using var stream = new FileStream("test.p", FileMode.Open);
var tree = await Element.LoadAsync(stream, cancellationToken);
```

<a id="2517542937"></a>

## LoadAsync\(Stream\) 

  
Loads a tree from a stream\.

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> LoadAsync(System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1)\<[Element](../README.md)\>

The tree, if no error occurred

### Exceptions

[PromptuariumException](../../PromptuariumException/README.md)

Thrown if the stream is not a valid Promptuarium stream

### Examples

```
using var stream = new FileStream("test.p", FileMode.Open);
var tree = await Element.LoadAsync(stream);
```

<a id="25283078"></a>

## LoadAsync\(String, CancellationToken\) 

  
Loads a tree from a file\.

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> LoadAsync(string fileName, System.Threading.CancellationToken cancellationToken)
```

### Parameters

**fileName** &ensp; [String](https://docs.microsoft.com/en-us/dotnet/api/system.string)

The source file name

**cancellationToken** &ensp; [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken)

The cancellation token

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1)\<[Element](../README.md)\>

The tree, if no error occurred

### Exceptions

[PromptuariumException](../../PromptuariumException/README.md)

Thrown if the file is not a valid Promptuarium file

### Examples

```
var tree = await Element.LoadAsync("test.p", cancellationToken);
```

<a id="1926306021"></a>

## LoadAsync\(String\) 

  
Loads a tree from a file\.

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> LoadAsync(string fileName)
```

### Parameters

**fileName** &ensp; [String](https://docs.microsoft.com/en-us/dotnet/api/system.string)

The source file name

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1)\<[Element](../README.md)\>

The tree, if no error occurred

### Exceptions

[PromptuariumException](../../PromptuariumException/README.md)

Thrown if the file is not a valid Promptuarium file

### Examples

```
var tree = await Element.LoadAsync("test.p");
```

