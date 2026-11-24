# PStudio

PStudio (Promptuarium Studio) is a graphical editor for Promptuarium trees, for Windows, Linux and macOS. It does what [`pedit`](../PEdit/README.md) does, with a user interface.

## Running

```sh
dotnet run --project tools/PStudio [FILE]
# or, after building:
dotnet tools/PStudio/bin/Debug/net10.0/pstudio.dll [FILE]
```

## Windows installer

Every [release](https://github.com/fuszenecker/Promptuarium/releases) has a Windows installer (x64; .NET is not needed, it is self-contained): `pstudio-<version>-win-x64-setup.exe`. It installs PStudio and [`pedit`](../PEdit/README.md), its command line counterpart, for every user (in `Program Files\PStudio`), with

* Start Menu entries: PStudio, and a command prompt in the folder of pedit;
* the `.prmt` file association (optional): Explorer opens the containers with PStudio, with its icon;
* a desktop shortcut (optional);
* an entry in Apps (Add or Remove Programs), to uninstall it. Installing a new version removes the old one first.

To build it (needs PowerShell 7 and NSIS; the result is in `artifacts/`):

```sh
tools/PStudio/packaging/windows/build-windows-installer.ps1 [-Version 8.1.0]
```

The release builds it on Windows, because only a build on Windows puts the icon and the version into `pstudio.exe`; on Linux (`apt install nsis`), the installer is the same, with the default icon of the executables.

## Debian package

Every [release](https://github.com/fuszenecker/Promptuarium/releases) has a Debian package (amd64; .NET is not needed, the package is self-contained) of PStudio and pedit:

```sh
sudo apt install ./pstudio_8.1.0_amd64.deb
```

The package

* installs `pstudio` and `pedit` (`/usr/bin/pstudio`, `/usr/bin/pedit`, the applications in `/usr/lib/pstudio`, sharing the runtime) with the desktop entry and the icon of PStudio, and the man page of pedit; it replaces the former `pedit` package;
* registers the MIME type `application/x-promptuarium` (shared-mime-info): files with the `.prmt` extension, and files with a `PRMT` signature ([BINARY_FORMAT.md](../../BINARY_FORMAT.md), 10) whatever their name is, so file managers show them as Promptuarium containers and open them with PStudio;
* adds the rules of [`misc/promptuarium.magic`](../../misc/promptuarium.magic) to `/etc/magic`, between `# BEGIN pstudio` and `# END pstudio`, so that `file` recognizes the containers and shows their tags (`photo.prmt: Promptuarium container, IMGZ5.6`). Removing the package removes the rules.

To build the package (needs `dpkg-deb`; the result is in `artifacts/`):

```sh
tools/PStudio/packaging/build-deb.sh [--version 8.1.0] [--runtime linux-x64|linux-arm64]
```

## RPM package

Every [release](https://github.com/fuszenecker/Promptuarium/releases) has an RPM package (x86_64; self-contained) of PStudio and pedit too, for Fedora, RHEL, openSUSE and the like:

```sh
sudo dnf install ./pstudio-8.1.0-1.x86_64.rpm      # Fedora, RHEL
sudo zypper install ./pstudio-8.1.0-1.x86_64.rpm   # openSUSE
```

It installs the same files as the Debian package, and adds the rules of the containers to `/etc/magic` in the same way (also when `file`, which brings `/etc/magic`, is installed after PStudio); removing the package removes them. A pre-release (`8.1.0-rc1`) is version `8.1.0~rc1`.

To build the package (needs `rpmbuild`: `dnf install rpm-build`, or `apt install rpm`; the result is in `artifacts/`):

```sh
tools/PStudio/packaging/build-rpm.sh [--version 8.1.0] [--runtime linux-x64|linux-arm64]
```

## Features

* **Tree view**: every node shows its index and a short preview of its data and, in brackets, its metadata.
* **Editing the tree**: add a child or a sibling, delete, move up and down, indent (under the previous sibling) and outdent (after the parent); cut, copy and paste nodes with their descendants, also between windows; the forms chosen for their contents are copied with them. Everything is in the context menu of the tree too.
* **Drag and drop**: a node is dropped before (upper quarter of the row), after (lower quarter) or into (middle) another node.
* **Contents**: the metadata and the data of the selected node are edited in any form the `Data` class knows: hex, base64, UTF-8, ASCII, UTF-16, UTF-32, bool, byte, char, short, ushort, int, uint, long, ulong, VarInt, VarUInt, float, double, decimal, Guid, DateTime, DateTimeOffset and TimeSpan; the forms of the values of version 2 of the [serializer format](../../SERIALIZER_FORMAT.md): sbyte, Int128, UInt128, Half, BigInteger, UUID (RFC 9562, the byte order of its text, not that of `Guid`), date and date-time (since 1970), time of day, and timestamp (the local time with its offset, `2026-11-22T19:30:15+01:00`); and the tags and type codes of the serializer format by name (NAME, TYPE, ITEM, VALUE, COMMENT; NULL, BOOLEAN, INT32, …, TIMESTAMP, BLOB, CONTAINER). Only the forms that give the content back byte for byte are offered (e.g. no int for 2 bytes, no UTF-8 for invalid UTF-8; every form for an empty content, so a new value can be typed in any of them), so a content is never changed by accident. The form is chosen automatically (UTF-8 for text and for an empty content, hex otherwise), and it can be changed while editing: a valid edit is converted, otherwise the text is taken as meant in the new form. An edit is stored by Apply (Ctrl+Enter) or when another node is selected, and dropped by Revert (Escape). Contents longer than 1 MiB are not shown, but they can be imported and exported.
* **Serializer layout by default**: a one-byte metadata is shown as a tag of the [serializer format](../../SERIALIZER_FORMAT.md) (and so is an empty one in the layout, e.g. of a new member, so its tag can be typed in: a child of a holder, a sibling of a tagged node, a member of a root signed `PRMT:GEN1-LE`, `PRMT:GEN2-LE`, `PRMT:CFG1-LE` or `PRMT:CFG2-LE`), and the data follows it, also when the tag is changed: a NAME and a COMMENT as UTF-8, a TYPE as its type code, a VALUE by the type in the TYPE node of its holder (BOOLEAN as bool, the integers as sbyte … Int128, byte … UInt128 and BigInteger, FLOAT16, FLOAT32 and FLOAT64 as Half, float and double, DECIMAL and UTF8_STRING as UTF-8, UUID as UUID, DATE, TIME, DATETIME and TIMESTAMP as date, time of day, date-time and timestamp, DURATION as TimeSpan, BLOB and CONTAINER as hex). A NaN typed in is the canonical one of the format. A form that can not show the content (e.g. a byte that is not a tag) is not used; a form chosen by the user wins.
* **Remembered forms**: the form chosen for the metadata or the data of a node is remembered for the file, by the path of the node, and used again when the file is opened (if it can still show the content). The forms are stored when the file is saved, or when the window closes or opens another tree without unsaved changes (the paths are the ones of the file). A nested tree has no file: its forms are kept with its node when it is saved into the node or closed without changes, remembered with the file of the host (by the path of the node, also for the trees nested deeper), and used again when the data of the same node is opened as a nested tree; they move and are copied with the node. The memory is `~/.config/PStudio/forms.prmt` (on Windows in `%APPDATA%\PStudio`), written with [Promptuarium.Serializer](../../src/Promptuarium.Serializer/README.md), signed `PRMT:PSTNT1`; it keeps the last 200 files. View / Reset Types forgets the forms chosen for this tree and for the trees nested in it only (the other files keep theirs), so every content gets its default form again.
* **Recent files**: File / Open Recent lists the last 10 files opened or saved (with Clear Recent Files); a file that can not be opened any more is dropped from the list. Every window follows the list.
* **Settings**: the settings of PStudio (whether the chunk panel and the serializer view are shown, the recent files) are kept in `~/.config/PStudio/settings.prmt`, a configuration file (`PRMT:CFG1-LE`, [SERIALIZER_FORMAT.md](../../SERIALIZER_FORMAT.md)), stored when they change and used by every new window. A setting that the file does not have gets its default, and a setting that this version does not know is ignored.
* **Import and export**: a content can be loaded from a file and saved to a file.
* **Node summary**: the path, the number of children, the sizes of the contents, and how the node is saved (its chunks, their size and offset).
* **Serializer view** (View / Serializer, F7): the tree as the values of the [serializer format](../../SERIALIZER_FORMAT.md), on a tab of its own beside the tab of the raw tree (whose background is darker): the members of the objects with their names, the elements of the arrays, and for each its type and its value, edited in place, and beside it, instead of the editors of the metadata and the data, the tree as YAML or JSON, colored (the keys, the strings, the numbers, true/false/null, the tags and the comments; the choice is remembered). The type is chosen from the types of the format; another type converts the value by its text (42 from INT32 to INT64 or to UTF8_STRING, otherwise the default of the type), an object becomes an array of its values and an array an object of members named by the indexes (a non-empty object or array does not become a scalar: its members are not dropped by accident). The rows are shown as texts; a double click or F2 edits a whole row, its name, its type, its value and its comment (in a box of three wrapped lines under them, as wide as they are; Shift+Enter breaks a line of it; Enter or leaving the row applies the row, Escape reverts it); a comment is the tooltip of its row (the comment of the file that of the root). A value is edited in the form of its type (a check box for a BOOLEAN, numbers, UUIDs, dates and times as text, hex for a BLOB) and applied by Enter or by leaving the box, Escape reverts it; a value that is not one of its type is not applied (the box turns red, the status bar tells why). Add (Insert) adds a member or an element into the selected object or array (or after the selected value), Add After (Shift+Insert), Delete, Up and Down (Shift+Up, Shift+Down) are there too. The view is shown only when the tree is serializable: `PromptuariumSerializer.Validate` finds no error in its structure (the tags, the types, the holders, the values, the comments; the signature does not matter, an unsigned tree or one with the signature of an application is shown too). A change in the raw tree that breaks the structure closes the view (the status bar tells why), and fixing it opens it again. The selections of the two views follow each other (a TYPE, VALUE or COMMENT node selects its holder), and every edit is made in the raw tree, so they can not differ. A serializable tree is opened in it (a new one too); switching it on brings its tab to the front; switching it off (or a tree that is not serializable any more) hides its tab and shows the raw tree. Switching it off is remembered, as the chunk panel.
* **Layout**: the size, the place and the maximized state of the window, where the splitters were dragged (between the trees and the editors, above the chunk panel, between the editors of the metadata and the data) and the panels shown are remembered for the next session.
* **Chunk panel** (View / Chunks, F6): a table with resizable columns of the chunks of the tree as it would be saved (offset, control byte, direction, type, length, kind, depth, payload), decoded with the decoder of `pedit`, directly from the [specification](../../BINARY_FORMAT.md). The chunks of the selected node are highlighted, and a click on a chunk selects its node.
* **Nested trees**: if the data of a node is itself a tree, Node / Open Data as Nested Tree opens it in its own window; Save stores the tree in the data of the node. The VALUE of a CONTAINER of the serializer format is such a tree (raw bytes like a BLOB, shown as hex, but known to be a tree).
* **Signed roots**: the signature of a signed root ([BINARY_FORMAT.md](../../BINARY_FORMAT.md), 10) is shown in the node information.
* **Version**: in the title of the window (`tree.prmt - Promptuarium Studio 8.4.0`), and in Help / About, with the commit it was built from and the .NET runtime. The command line tools print theirs with `--version` (`pcfg 8.4.0 (4c18335)`).
* Files are saved atomically (a temporary file is written, then renamed), and unsaved changes are asked about before closing or opening another file.

## Keys

| Key | Action |
| --- | --- |
| Ctrl+N, Ctrl+O, Ctrl+S, Ctrl+Shift+S | New, Open, Save, Save As |
| Insert, Shift+Insert | Add a child, add a sibling |
| Delete | Delete the node |
| Shift+Up, Shift+Down | Move up, move down |
| Shift+Right, Shift+Left | Indent, outdent |
| Ctrl+X, Ctrl+C | Cut, copy the node |
| Ctrl+V, Ctrl+Shift+V | Paste as the last child, paste after the node |
| Ctrl+Enter, Escape (in a content) | Apply, revert the edit |
| F6 | Show or hide the chunk panel |
| F7 | Show or hide the serializer view |
| Insert, Shift+Insert, Delete, Shift+Up, Shift+Down (in the serializer view) | Add, add after, delete, move up, move down a value |
| F2, double click (on a row of the serializer view) | Edit the row: its name, type and value |
| Enter, Escape (in an edited row of the serializer view) | Apply, revert the edit of the row |

## Architecture

An MVVM application with [Avalonia](https://avaloniaui.net/) and [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/):

```text
tools/PStudio/
  Views/          # AXAML and minimal code-behind
  ViewModels/     # [ObservableProperty] and [RelayCommand]; the commands keep their CanExecute
  Presentation/   # windows, file pickers and message boxes, the view model factory
  Services/       # files, chunk analysis, node clipboard, content forms, the memory of the chosen forms, the settings
  Interfaces/     # the abstractions of the services
  Models/         # plain data
  Composition/    # dependency injection (the container is built in App.axaml.cs)
tools/PStudio.Tests/            # MSTest and NSubstitute: services and view models
tools/PStudio.Headless.Tests/   # Avalonia headless: the real windows, rendered by Skia, operated by keys
```

`NodeViewModel` wraps an `Element`, and every change of its children changes the element too, so the tree view and the saved tree can not differ.

The headless tests need no display. With `PSTUDIO_SCREENSHOTS` set to a directory, they save pictures of the windows there.
