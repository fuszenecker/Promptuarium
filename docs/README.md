#### [Promptuarium](README.md 'README')

## Promptuarium Assembly
### Namespaces

<a name='Promptuarium'></a>

## Promptuarium Namespace

| Classes | |
| :--- | :--- |
| [Data](Data.md 'Promptuarium\.Data') | Conversions between values and contents \(data or metadata\)\. |
| [Element](Element.md 'Promptuarium\.Element') | A node of a tree: a data, a metadata \(both are byte arrays, empty by default\) and children\. A tree is saved and loaded as a whole, with its structure; see BINARY\_FORMAT\.md for the binary form\. |
| [ElementBuilder](ElementBuilder.md 'Promptuarium\.ElementBuilder') | Builds a node step by step: its data, metadata, children, or a signature \(see BINARY\_FORMAT\.md, "File identification"\)\. |
| [LoadOptions](LoadOptions.md 'Promptuarium\.LoadOptions') | Limits for loading a tree, e\.g\. from an untrusted source\. |
| [PromptuariumException](PromptuariumException.md 'Promptuarium\.PromptuariumException') | Base class for all Promptuarium exceptions |
| [Statistics](Statistics.md 'Promptuarium\.Statistics') | Statistics of the tree |

| Delegates | |
| :--- | :--- |
| [Element\.WalkHandler\(Element, IReadOnlyCollection&lt;Element&gt;\)](Element.WalkHandler(Element,IReadOnlyCollection_Element_).md 'Promptuarium\.Element\.WalkHandler\(Promptuarium\.Element, System\.Collections\.Generic\.IReadOnlyCollection\<Promptuarium\.Element\>\)') | The method is called for each nodes\. |
