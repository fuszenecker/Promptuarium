# Element\.SaveAsync Method

[Home](../../../README.md)

**Containing Type**: [Element](../README.md)

**Assembly**: Promptuarium\.dll

## Overloads

| Method | Summary |
| ------ | ------- |
| [SaveAsync(Stream, CancellationToken)](#404143214) | Saves the tree into a stream\. |
| [SaveAsync(Stream)](#2654619920) | Saves the tree into a stream\. |
| [SaveAsync(String, CancellationToken)](#717888685) | Saves the tree into a file\. |
| [SaveAsync(String)](#3052064839) | Saves the tree into a file\. |

<a id="404143214"></a>

## SaveAsync\(Stream, CancellationToken\) 

  
Saves the tree into a stream\.

```csharp
public System.Threading.Tasks.Task SaveAsync(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The target stream

**cancellationToken** &ensp; [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken)

The cancellation token

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task)

### Examples

```
using var stream = new FileStream("test.p", FileMode.Create);
await tree.SaveAsync(stream, cancellationToken);
```

<a id="2654619920"></a>

## SaveAsync\(Stream\) 

  
Saves the tree into a stream\.

```csharp
public System.Threading.Tasks.Task SaveAsync(System.IO.Stream stream)
```

### Parameters

**stream** &ensp; [Stream](https://docs.microsoft.com/en-us/dotnet/api/system.io.stream)

The target stream

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task)

True, if no error occurred

### Examples

```
using var stream = new FileStream("test.p", FileMode.Create);
await tree.SaveAsync(stream);
```

<a id="717888685"></a>

## SaveAsync\(String, CancellationToken\) 

  
Saves the tree into a file\.

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

### Examples

```
await tree.SaveAsync("test.p", cancellationToken);
```

<a id="3052064839"></a>

## SaveAsync\(String\) 

  
Saves the tree into a file\.

```csharp
public System.Threading.Tasks.Task SaveAsync(string fileName)
```

### Parameters

**fileName** &ensp; [String](https://docs.microsoft.com/en-us/dotnet/api/system.string)

The target file name

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task)

### Examples

```
await tree.SaveAsync("test.p");
```

