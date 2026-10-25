using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Promptuarium;

/// <summary>
/// Builds a node step by step: its data, metadata, children, or a signature (see BINARY_FORMAT.md, "File identification").
/// </summary>
/// <example>
/// <code>
/// var root = new ElementBuilder()
///     .WithSignature(Data.FromAsciiString("PRMT"))
///     .WithChildren(new Element(Data.FromUtf8String("content"), null))
///     .Build();
///
/// var node = new ElementBuilder()
///     .WithData(Data.FromInt(42))
///     .WithMetaData(Data.FromAsciiString("answer"))
///     .Build();
/// </code>
/// </example>
public class ElementBuilder
{
    /// <summary>
    /// Maximum length of a signature: it fits in a single linear chunk, so it is at a fixed position at the beginning of the stream.
    /// </summary>
    public const int MaxSignatureLength = 15;

    private readonly List<Element> children = new();
    private Stream? data;
    private Stream? metaData;
    private byte[]? signature;

    /// <summary>
    /// Sets the data of the node.
    /// </summary>
    /// <param name="data">The data</param>
    /// <returns>The builder</returns>
    public ElementBuilder WithData(Stream? data)
    {
        this.data = data;
        return this;
    }

    /// <summary>
    /// Sets the metadata of the node.
    /// </summary>
    /// <param name="metaData">The metadata</param>
    /// <returns>The builder</returns>
    public ElementBuilder WithMetaData(Stream? metaData)
    {
        this.metaData = metaData;
        return this;
    }

    /// <summary>
    /// Makes the node a signed root: no data, and the signature as metadata, so every stream of the tree starts with the same bytes.
    /// The content of the file goes into the children.
    /// </summary>
    /// <param name="signature">The signature, 1 to 15 bytes. It is copied, later changes of the stream do not affect the node.</param>
    /// <returns>The builder</returns>
    /// <exception cref="ArgumentException">Thrown if the signature is empty or longer than 15 bytes</exception>
    public ElementBuilder WithSignature(Stream signature)
    {
        ArgumentNullException.ThrowIfNull(signature);

        if (signature.Length < 1 || signature.Length > MaxSignatureLength)
        {
            throw new ArgumentException($"A signature is 1 to {MaxSignatureLength.ToString(CultureInfo.InvariantCulture)} bytes long, not {signature.Length.ToString(CultureInfo.InvariantCulture)} bytes.", nameof(signature));
        }

        this.signature = signature.AsByteArray();
        return this;
    }

    /// <summary>
    /// Adds children to the node. They are moved into the node when it is built.
    /// </summary>
    /// <param name="children">The children</param>
    /// <returns>The builder</returns>
    public ElementBuilder WithChildren(params Element[] children)
    {
        return WithChildren((IEnumerable<Element>)children);
    }

    /// <summary>
    /// Adds children to the node. They are moved into the node when it is built.
    /// </summary>
    /// <param name="children">The children</param>
    /// <returns>The builder</returns>
    public ElementBuilder WithChildren(IEnumerable<Element> children)
    {
        ArgumentNullException.ThrowIfNull(children);

        foreach (Element child in children)
        {
            ArgumentNullException.ThrowIfNull(child, nameof(children));
            this.children.Add(child);
        }

        return this;
    }

    /// <summary>
    /// Builds the node.
    /// </summary>
    /// <returns>The new node</returns>
    /// <exception cref="InvalidOperationException">Thrown if a signature is combined with data or metadata</exception>
    /// <exception cref="PromptuariumException">Thrown if a child can not be added (e.g. it was added twice)</exception>
    public Element Build()
    {
        if (signature != null && (data != null || metaData != null))
        {
            throw new InvalidOperationException("A signed root has no data, and its metadata is the signature: WithSignature can not be combined with WithData or WithMetaData.");
        }

        // Every built node gets its own copy of the signature
        return new Element(data, signature != null ? new MemoryStream(signature) : metaData, children);
    }
}
