namespace Promptuarium.Serializer;

/// <summary>The property is neither written nor read.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PromptuariumIgnoreAttribute : Attribute
{
}

/// <summary>
/// A comment written with the value, in a COMMENT node (UTF-8): on a property, in the holder of the member; on a class
/// or a struct, in the holder of its objects (in the root for the root object), unless the property has a comment of
/// its own. Readers ignore the comments.
/// </summary>
/// <param name="text">The comment.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class PromptuariumCommentAttribute(string text) : Attribute
{
    /// <summary>The comment.</summary>
    public string Text { get; } = text;
}

/// <summary>The property is stored under <see cref="Name"/> instead of its own name.</summary>
/// <param name="name">The name of the member in the file.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PromptuariumNameAttribute(string name) : Attribute
{
    /// <summary>The name of the member in the file.</summary>
    public string Name { get; } = name;
}
