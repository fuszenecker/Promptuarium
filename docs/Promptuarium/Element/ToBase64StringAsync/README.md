# Element\.ToBase64StringAsync Method

[Home](../../../README.md)

**Containing Type**: [Element](../README.md)

**Assembly**: Promptuarium\.dll

## Overloads

| Method | Summary |
| ------ | ------- |
| [ToBase64StringAsync()](#579211045) | Converts the tree to Base64 string\. |
| [ToBase64StringAsync(CancellationToken)](#154336211) | Converts the tree to Base64 string\. |

<a id="579211045"></a>

## ToBase64StringAsync\(\) 

  
Converts the tree to Base64 string\.

```csharp
public System.Threading.Tasks.Task<string> ToBase64StringAsync()
```

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1)\<[String](https://docs.microsoft.com/en-us/dotnet/api/system.string)\>

The tree in Base64\.

### Examples

```
var tree = new Element();

var node1 = new Element();
    
var node2 = new Element();
node2.Data = Data.FromUtf8String("Hello world");

var node3 = new Element();
node3.Data = Data.FromUtf8String("Hello world");

tree.Add(node1, node2, node3);

var base64String = await tree.ToBase64StringAsync();
```

<a id="154336211"></a>

## ToBase64StringAsync\(CancellationToken\) 

  
Converts the tree to Base64 string\.

```csharp
public System.Threading.Tasks.Task<string> ToBase64StringAsync(System.Threading.CancellationToken cancellationToken)
```

### Parameters

**cancellationToken** &ensp; [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken)

The cancellation token\.

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1)\<[String](https://docs.microsoft.com/en-us/dotnet/api/system.string)\>

The tree in Base64\.

### Examples

```
var tree = new Element();

var node1 = new Element();

var node2 = new Element();
node2.Data = Data.FromUtf8String("Hello world");

var node3 = new Element();
node3.Data = Data.FromUtf8String("Hello world");

tree.Add(node1, node2, node3);

var base64String = await tree.ToBase64StringAsync();
```

