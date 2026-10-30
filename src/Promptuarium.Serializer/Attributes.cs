namespace Promptuarium.Serializer;

/// <summary>The property is neither written nor read.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PromptuariumIgnoreAttribute : Attribute
{
}

/// <summary>The property is stored under <see cref="Name"/> instead of its own name.</summary>
/// <param name="name">The name of the member in the file.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PromptuariumNameAttribute(string name) : Attribute
{
    /// <summary>The name of the member in the file.</summary>
    public string Name { get; } = name;
}
