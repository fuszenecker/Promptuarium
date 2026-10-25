// pxml: converts an XML document to a Promptuarium file, and back, without losing a node. See PXmlFormat.cs
// for the layout, and README.md.
//
//   pxml pack XML-FILE FILE      converts the XML document to a pxml file
//   pxml unpack FILE XML-FILE    converts a pxml file back to XML

using System.Text;
using System.Xml;
using Promptuarium;
using PXml;

try
{
    switch (args)
    {
        case ["pack", string xml, string file]:
            Element tree = XmlConverter.ToTree(XmlConverter.Load(xml));
            await tree.SaveAsync(file);
            Console.WriteLine($"{file}: {tree.GetStatistics().Nodes} nodes, {new FileInfo(file).Length:N0} bytes (XML: {new FileInfo(xml).Length:N0} bytes)");
            return 0;

        case ["unpack", string file, string xml]:
            // The file may come from anywhere: the tree can not be larger than the file
            Element loaded = await Element.LoadAsync(file, new LoadOptions { MaxBytes = new FileInfo(file).Length }, CancellationToken.None);
            XmlDocument document = XmlConverter.ToXml(loaded);

            // The XML declaration is a node of the document: it is written as it was, and only if the document had one
            // (an XmlWriter would always write its own)
            using (var text = new StreamWriter(xml, false, new UTF8Encoding(false)))
            using (var writer = XmlWriter.Create(text, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true }))
            {
                foreach (XmlNode node in document.ChildNodes)
                {
                    if (node is XmlDeclaration declaration)
                    {
                        writer.Flush();
                        text.WriteLine($"<?xml {declaration.InnerText}?>");
                    }
                    else
                    {
                        node.WriteTo(writer);
                    }
                }

                writer.Flush();
                text.WriteLine();
            }

            return 0;

        default:
            Console.Error.WriteLine("Usage: pxml pack XML-FILE FILE | unpack FILE XML-FILE");
            return 2;
    }
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PromptuariumException or InvalidDataException or XmlException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
