# PStudio

PStudio (Promptuarium Studio) is a graphical editor for Promptuarium trees, for Windows, Linux and macOS. It does what [`pedit`](../PEdit/README.md) does, with a user interface.

## Running

```sh
dotnet run --project tools/PStudio [FILE]
# or, after building:
dotnet tools/PStudio/bin/Debug/net10.0/pstudio.dll [FILE]
```

## Features

* **Tree view**: every node shows its index and a short preview of its data and, in brackets, its metadata.
* **Editing the tree**: add a child or a sibling, delete, move up and down, indent (under the previous sibling) and outdent (after the parent); cut, copy and paste nodes with their descendants, also between windows. Everything is in the context menu of the tree too.
* **Drag and drop**: a node is dropped before (upper quarter of the row), after (lower quarter) or into (middle) another node.
* **Contents**: the data and the metadata of the selected node are edited in any form the `Data` class knows: hex, base64, UTF-8, ASCII, UTF-16, UTF-32, bool, byte, char, short, ushort, int, uint, long, ulong, VarInt, VarUInt, float, double, decimal, Guid, DateTime, DateTimeOffset and TimeSpan. The form is chosen automatically (UTF-8 for text and for an empty content, hex otherwise), and it can be changed while editing: a valid edit is converted, otherwise the text is taken as meant in the new form. An edit is stored by Apply (Ctrl+Enter) or when another node is selected, and dropped by Revert (Escape). A content that the chosen form can not give back byte for byte (e.g. invalid UTF-8, or 2 bytes as an int) is shown as hex, read-only, so it is never changed by accident. Contents longer than 1 MiB are not shown, but they can be imported and exported.
* **Import and export**: a content can be loaded from a file and saved to a file.
* **Node summary**: the path, the number of children, the sizes of the contents, and how the node is saved (its chunks, their size and offset).
* **Chunk panel** (View / Chunks, F6): the chunks of the tree as it would be saved (offset, control byte, direction, type, length, kind, depth, payload), decoded with the decoder of `pedit`, directly from the [specification](../../BINARY_FORMAT.md). The chunks of the selected node are highlighted, and a click on a chunk selects its node.
* **Nested trees**: if the data of a node is itself a tree, Node / Open Data as Nested Tree opens it in its own window; Save stores the tree in the data of the node.
* **Signed roots**: the signature of a signed root ([BINARY_FORMAT.md](../../BINARY_FORMAT.md), 10) is shown in the node information.
* Files are saved atomically (a temporary file is written, then renamed), and unsaved changes are asked about before closing or opening another file.

## Keys

| Key | Action |
| --- | --- |
| Ctrl+N, Ctrl+O, Ctrl+S, Ctrl+Shift+S | New, Open, Save, Save As |
| Insert, Ctrl+Insert | Add a child, add a sibling |
| Delete | Delete the node |
| Alt+Up, Alt+Down | Move up, move down |
| Alt+Right, Alt+Left | Indent, outdent |
| Ctrl+X, Ctrl+C | Cut, copy the node |
| Ctrl+V, Ctrl+Shift+V | Paste as the last child, paste after the node |
| Ctrl+Enter, Escape (in a content) | Apply, revert the edit |
| F6 | Show or hide the chunk panel |

## Architecture

An MVVM application with [Avalonia](https://avaloniaui.net/) and [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/):

```text
tools/PStudio/
  Views/          # AXAML and minimal code-behind
  ViewModels/     # [ObservableProperty] and [RelayCommand]; the commands keep their CanExecute
  Presentation/   # windows, file pickers and message boxes, the view model factory
  Services/       # files, chunk analysis, node clipboard, content forms
  Interfaces/     # the abstractions of the services
  Models/         # plain data
  Composition/    # dependency injection (the container is built in App.axaml.cs)
tools/PStudio.Tests/            # MSTest and NSubstitute: services and view models
tools/PStudio.Headless.Tests/   # Avalonia headless: the real windows, rendered by Skia, operated by keys
```

`NodeViewModel` wraps an `Element`, and every change of its children changes the element too, so the tree view and the saved tree can not differ.

The headless tests need no display. With `PSTUDIO_SCREENSHOTS` set to a directory, they save pictures of the windows there.
