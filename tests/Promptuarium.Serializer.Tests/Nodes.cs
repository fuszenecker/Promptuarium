using Promptuarium;
using Promptuarium.Serializer;

namespace Promptuarium.Serializer.Tests;

/// <summary>Builds CFG1-LE trees by hand, for the cases a writer never produces.</summary>
internal static class Nodes
{
    public static byte[] Hex(string hex)
    {
        return [.. hex.Split([' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Select(b => Convert.ToByte(b, 16))];
    }

    public static Element Root(params Element[] members)
    {
        return new ElementBuilder()
            .WithSignature(Data.FromAsciiString(PromptuariumSerializerOptions.DefaultSignature))
            .WithChildren(members)
            .Build();
    }

    /// <summary>A NAME holder: the TYPE node, then the VALUE node if a value is given.</summary>
    public static Element Member(string name, byte type, ReadOnlyMemory<byte>? value = null)
    {
        return Holder(0x00, Data.FromUtf8String(name), type, value);
    }

    public static Element Holder(byte tag, ReadOnlyMemory<byte> name, byte type, ReadOnlyMemory<byte>? value = null)
    {
        var holder = new Element(name, Data.FromByte(tag), new Element(Data.FromByte(type), Data.FromByte(0x01)));

        if (value is { } content)
        {
            holder.Add(new Element(content, Data.FromByte(0x03)));
        }

        return holder;
    }

    /// <summary>A NAME holder of an OBJECT with the given members.</summary>
    public static Element Object(string name, params Element[] members)
    {
        Element holder = Member(name, 0x20);
        holder.Add(members);
        return holder;
    }
}
