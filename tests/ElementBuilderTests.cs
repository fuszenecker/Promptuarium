using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Promptuarium;

namespace PromptuariumTests
{
    /// <summary>
    /// Tests for the element builder
    /// </summary>
    [TestClass]
    public class ElementBuilderTests
    {
        [TestMethod]
        public async Task SignedRootStartsWithTheSignature()
        {
            var root = new ElementBuilder()
                .WithSignature(Data.FromAsciiString("PRMT"))
                .WithChildren(new Element(Data.FromAsciiString("a"), null))
                .Build();

            using var stream = new MemoryStream();
            await root.SaveAsync(stream).ConfigureAwait(false);

            // See BINARY_FORMAT.md, 10.2
            CollectionAssert.AreEqual(new byte[] { 0x64, 0x50, 0x52, 0x4D, 0x54, 0x41, 0x61, 0x00 }, stream.ToArray());

            stream.Position = 0;
            Assert.IsTrue(root.StructurallyEquals(await Element.LoadAsync(stream).ConfigureAwait(false)));
        }

        [TestMethod]
        public void SignedRootWithoutChildren()
        {
            Element root = new ElementBuilder().WithSignature(Data.FromAsciiString("PRMT")).Build();

            Assert.IsTrue(root.Data.IsEmpty);
            Assert.AreEqual("PRMT", root.MetaData.AsAsciiString());
            Assert.AreEqual(0, root.Children.Count);
        }

        [TestMethod]
        public void SignatureIsCopied()
        {
            byte[] signature = { 1, 2, 3 };
            Element root = new ElementBuilder().WithSignature(signature).Build();

            signature[0] = 4;

            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, root.MetaData.AsByteArray());
        }

        [TestMethod]
        public void SignatureLength()
        {
            var builder = new ElementBuilder();

            builder.WithSignature(new byte[1]);
            builder.WithSignature(new byte[ElementBuilder.MaxSignatureLength]);

            Assert.ThrowsException<ArgumentException>(() => builder.WithSignature(ReadOnlyMemory<byte>.Empty));
            Assert.ThrowsException<ArgumentException>(() => builder.WithSignature(new byte[ElementBuilder.MaxSignatureLength + 1]));
        }

        [TestMethod]
        public void SignatureCanNotBeCombinedWithContent()
        {
            Assert.ThrowsException<InvalidOperationException>(() =>
                new ElementBuilder().WithSignature(Data.FromAsciiString("PRMT")).WithData(Data.FromInt(1)).Build());

            Assert.ThrowsException<InvalidOperationException>(() =>
                new ElementBuilder().WithMetaData(Data.FromInt(1)).WithSignature(Data.FromAsciiString("PRMT")).Build());
        }

        [TestMethod]
        public void OrdinaryNode()
        {
            var child1 = new Element(Data.FromAsciiString("1"), null);
            var child2 = new Element(Data.FromAsciiString("2"), null);

            Element node = new ElementBuilder()
                .WithData(Data.FromInt(42))
                .WithMetaData(Data.FromAsciiString("answer"))
                .WithChildren(child1)
                .WithChildren(child2)
                .Build();

            Assert.AreEqual(42, node.Data.AsInt());
            Assert.AreEqual("answer", node.MetaData.AsAsciiString());
            Assert.AreSame(child1, node[0]);
            Assert.AreSame(child2, node[1]);
            Assert.AreSame(node, child1.Parent);
        }

        [TestMethod]
        public void EmptyNode()
        {
            Assert.IsTrue(new ElementBuilder().Build().StructurallyEquals(new Element()));
        }
    }
}
