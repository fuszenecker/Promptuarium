# Element\.LoadAsync Method

[Home](../../../README.md)

**Containing Type**: [Element](../README.md)

**Assembly**: Promptuarium\.dll

## Overloads

| Method | Summary |
| ------ | ------- |
| [LoadAsync(Stream, CancellationToken)](#2169171820) | Loads a tree from a stream \(synchronously\)\. |
| [LoadAsync(Stream)](#2517542937) | |
| [LoadAsync(String, CancellationToken)](#25283078) | Loads a tree from a file \(synchronously\)\. |
| [LoadAsync(String)](#1926306021) | |

<a id="2169171820"></a>

## LoadAsync\(Stream, CancellationToken\) 

  
Loads a tree from a stream \(synchronously\)\.

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> LoadAsync(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The source stream

**cancellationToken** &ensp; [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken)

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1)\<[Element](../README.md)\>

The tree, if no error occurred<a id="2517542937"></a>

## LoadAsync\(Stream\) 

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> LoadAsync(System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1)\<[Element](../README.md)\>

<a id="25283078"></a>

## LoadAsync\(String, CancellationToken\) 

  
Loads a tree from a file \(synchronously\)\.

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> LoadAsync(string fileName, System.Threading.CancellationToken cancellationToken)
```

### Parameters

**fileName** &ensp; [String](https://docs.microsoft.com/en-us/dotnet/api/system.string)

The source file name

**cancellationToken** &ensp; [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken)

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1)\<[Element](../README.md)\>

The tree, if no error occurred<a id="1926306021"></a>

## LoadAsync\(String\) 

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> LoadAsync(string fileName)
```

### Parameters

**fileName** &ensp; [String](https://docs.microsoft.com/en-us/dotnet/api/system.string)

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1)\<[Element](../README.md)\>

