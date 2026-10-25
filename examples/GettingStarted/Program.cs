// Getting started: build a tree, save it, load it, and read the contents back.
//
// A tree is made of elements; every element has a data, a metadata (both are byte arrays, empty by default)
// and children. The Data class converts values to and from bytes.

using Promptuarium;

// A small tree: a book with its chapters. The metadata tells what the data is.
var book = new Element(Data.FromUtf8String("The Hitchhiker's Guide"), Data.FromAsciiString("title"),
    new Element(Data.FromInt(42), Data.FromAsciiString("answer")),
    new Element(Data.FromDateTime(new DateTime(1979, 10, 12)), Data.FromAsciiString("published")),
    new Element(null, Data.FromAsciiString("chapters"),
        new Element(Data.FromUtf8String("Chapter 1"), null),
        new Element(Data.FromUtf8String("Chapter 2"), null)));

// Children can also be added later
book[2].Add(new Element(Data.FromUtf8String("Chapter 3"), null));

// Save to a file, and load it back
string path = Path.Combine(Path.GetTempPath(), $"getting-started-{Guid.NewGuid():N}.prmt");
await book.SaveAsync(path);
Element loaded = await Element.LoadAsync(path);
File.Delete(path);

// The structure and the contents are the same
Console.WriteLine(loaded.TreeToString("  "));
Console.WriteLine($"Same tree: {loaded.StructurallyEquals(book)}");

// The contents are read back with the same type they were written with
Console.WriteLine($"Title:     {loaded.Data.AsUtf8String()}");
Console.WriteLine($"Answer:    {loaded[0].Data.AsInt()}");
Console.WriteLine($"Published: {loaded[1].Data.AsDateTime():yyyy-MM-dd}");
Console.WriteLine($"Chapters:  {string.Join(", ", loaded[2].Children.Select(chapter => chapter.Data.AsUtf8String()))}");

// The tree can also be converted to bytes (or base64) in memory, without a stream
byte[] bytes = book.ToBytes();
Console.WriteLine($"{bytes.Length} bytes, base64: {book.ToBase64String()[..32]}...");
Console.WriteLine(loaded.GetStatistics());

return loaded.StructurallyEquals(book) && loaded[0].Data.AsInt() == 42 ? 0 : 1;
