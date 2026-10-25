#### [Promptuarium](README.md 'README')
### [Promptuarium](README.md#Promptuarium 'Promptuarium')

## LoadOptions Class

Limits for loading a tree, e\.g\. from an untrusted source\.

```csharp
public class LoadOptions
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → LoadOptions

### Example

```csharp
var options = new LoadOptions { MaxBytes = 10 * 1024 * 1024, MaxNodes = 100_000 };
var tree = await Element.LoadAsync(networkStream, options, cancellationToken);
```
### Properties

<a name='Promptuarium.LoadOptions.MaxBytes'></a>

## LoadOptions\.MaxBytes Property

Maximum number of bytes read from the stream \(control bytes included\), or null for no limit\.

```csharp
public System.Nullable<long> MaxBytes { get; set; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int64](https://learn.microsoft.com/en-us/dotnet/api/system.int64 'System\.Int64')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='Promptuarium.LoadOptions.MaxNodes'></a>

## LoadOptions\.MaxNodes Property

Maximum number of nodes created while reading the stream \(auxiliary nodes included\), or null for no limit\.

```csharp
public System.Nullable<long> MaxNodes { get; set; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int64](https://learn.microsoft.com/en-us/dotnet/api/system.int64 'System\.Int64')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')