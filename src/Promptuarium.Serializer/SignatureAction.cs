namespace Promptuarium.Serializer;

/// <summary>
/// What a reader does with a file that is not signed with <see cref="PromptuariumSerializerOptions.Signature"/>: the
/// answer of <see cref="PromptuariumSerializerOptions.UnexpectedSignature"/>.
/// </summary>
public enum SignatureAction
{
    /// <summary>The file is rejected with <see cref="InvalidDataException"/> (the default).</summary>
    Reject,

    /// <summary>The file is read as if it had the signature: its members are read by the usual rules.</summary>
    Accept,

    /// <summary>
    /// The file is read as if it had no members: <c>Deserialize</c> gives a new object with its default values,
    /// <c>Populate</c> leaves the target as it is.
    /// </summary>
    Skip,
}
