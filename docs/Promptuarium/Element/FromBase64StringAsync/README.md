# Element\.FromBase64StringAsync\(String, Element, CancellationToken\) Method

[Home](../../../README.md)

**Containing Type**: [Element](../README.md)

**Assembly**: Promptuarium\.dll

  
Creates a tree from a Base64 string\.

```csharp
public static System.Threading.Tasks.Task<Promptuarium.Element> FromBase64StringAsync(string base64String, Promptuarium.Element loaderElement = null, System.Threading.CancellationToken cancellationToken = default)
```

### Parameters

**base64String** &ensp; [String](https://docs.microsoft.com/en-us/dotnet/api/system.string)

The tree in Base64\.

**loaderElement** &ensp; [Element](../README.md)

**cancellationToken** &ensp; [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken)

The cancellation token\.

### Returns

[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1)\<[Element](../README.md)\>

The tree\.

## Examples

```
var tree = await Element.FromBase64StringAsync(base64String);
```

