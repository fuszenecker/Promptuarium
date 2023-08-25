# Element\.FromBase64StringAsync Method

[Home](../../../README.md)

**Containing Type**: [Element](../README.md)

**Assembly**: Promptuarium\.dll

## Overloads

| Method | Summary |
| ------ | ------- |
| [FromBase64StringAsync(String, CancellationToken)](#142880008) | Creates a tree from a Base64 string\. |
| [FromBase64StringAsync(String)](#3836437132) | Creates a tree from a Base64 string\. |

<a id="142880008"></a>

## FromBase64StringAsync\(String, CancellationToken\) 

  
Creates a tree from a Base64 string\.

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> FromBase64StringAsync(string base64String, System.Threading.CancellationToken cancellationToken)
```

### Parameters

**base64String** &ensp; [String](https://docs.microsoft.com/en-us/dotnet/api/system.string)

The tree in Base64\.

**cancellationToken** &ensp; [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken)

The cancellation token\.

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1)\<[Element](../README.md)\>

The tree\.

### Examples

```
var tree = await Element.FromBase64StringAsync(base64String);
```

<a id="3836437132"></a>

## FromBase64StringAsync\(String\) 

  
Creates a tree from a Base64 string\.

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> FromBase64StringAsync(string base64String)
```

### Parameters

**base64String** &ensp; [String](https://docs.microsoft.com/en-us/dotnet/api/system.string)

The tree in Base64\.

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1)\<[Element](../README.md)\>

The tree\.

### Examples

```
var tree = await Element.FromBase64StringAsync(base64String);
```

