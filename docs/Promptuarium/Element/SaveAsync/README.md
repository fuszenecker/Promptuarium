# Element\.SaveAsync Method

[Home](../../../README.md)

**Containing Type**: [Element](../README.md)

**Assembly**: Promptuarium\.dll

## Overloads

| Method | Summary |
| ------ | ------- |
| [SaveAsync(Stream, CancellationToken)](#404143214) | Saves the tree into a stream \(synchronously\)\. |
| [SaveAsync(Stream)](#2654619920) | |
| [SaveAsync(String, CancellationToken)](#717888685) | Saves the tree into a file \(synchronously\)\. |
| [SaveAsync(String)](#3052064839) | Saves the tree into a file \(synchronously\)\. |

<a id="404143214"></a>

## SaveAsync\(Stream, CancellationToken\) 

  
Saves the tree into a stream \(synchronously\)\.

```csharp
public System.Threading.Tasks.Task SaveAsync(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The target stream

**cancellationToken** &ensp; [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken)

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task)

True, if no error occurred<a id="2654619920"></a>

## SaveAsync\(Stream\) 

```csharp
public System.Threading.Tasks.Task SaveAsync(System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task)

<a id="717888685"></a>

## SaveAsync\(String, CancellationToken\) 

  
Saves the tree into a file \(synchronously\)\.

```csharp
public System.Threading.Tasks.Task SaveAsync(string fileName, System.Threading.CancellationToken cancellationToken)
```

### Parameters

**fileName** &ensp; [String](https://docs.microsoft.com/en-us/dotnet/api/system.string)

The target file name

**cancellationToken** &ensp; [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken)

The cancellation token

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task)

True, if no error occurred<a id="3052064839"></a>

## SaveAsync\(String\) 

  
Saves the tree into a file \(synchronously\)\.

```csharp
public System.Threading.Tasks.Task SaveAsync(string fileName)
```

### Parameters

**fileName** &ensp; [String](https://docs.microsoft.com/en-us/dotnet/api/system.string)

The target file name

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task)

True, if no error occurred