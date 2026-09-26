# FilmStrip

A horizontal thumbnail strip control for Windows Forms, written in VB.NET. It's a port of the old VB6 `FilmStrip.ocx` user control: a row of image thumbnails with titles, back/next arrow buttons on the sides and a scrollbar underneath.

## Solution layout

```
FilmStrip.sln
├── FilmStrip/            Control library (FilmStrip.dll)
│   ├── FilmStrip.vb      The control
│   ├── FilmStripItem.vb  One thumbnail + title
│   └── FilmStrip.vbproj
└── FilmStripTest/        Test app (startup project)
    ├── Form1.vb / Form1.Designer.vb
    ├── Program.vb
    └── FilmStripTest.vbproj
```

## Requirements

- .NET 8 or newer (`net8.0-windows`), Windows Forms
- Visual Studio 2022 17.8 or newer

The control uses `ArgumentNullException.ThrowIfNull` and related helpers that don't exist before .NET 7, so it won't build for .NET Framework.

## Getting started

1. Open `FilmStrip.sln` and build.
2. Press F5 to run the test app, or reference `FilmStrip.vbproj` (or `FilmStrip.dll`) from your own project.
3. FilmStrip appears in the Toolbox after the first build. Drop it on a form.

```vb
Imports FilmStripControl

' Add a single file (throws if it isn't a readable image)
FilmStrip1.AddItem("C:\Photos\holiday.jpg")

' Add a whole folder; thumbnails are made on a background thread
Dim files = IO.Directory.GetFiles("C:\Photos", "*.jpg")
Dim added = Await FilmStrip1.AddItemsAsync(files)

' Add an in-memory image with a title
FilmStrip1.AddItem(myBitmap, "Screenshot 1")

Private Sub FilmStrip1_ItemClick(sender As Object, e As FilmStripItemEventArgs) Handles FilmStrip1.ItemClick
    Debug.WriteLine($"Clicked #{e.Index}: {e.Item.Path}")
End Sub
```

## Features

- High-quality thumbnails that keep their proportions; small images aren't enlarged
- Camera photos are shown the right way up (EXIF orientation)
- Image files aren't locked while they're shown in the strip
- Back/next arrow buttons, a scrollbar, the mouse wheel and the keyboard (arrows, Home/End, PgUp/PgDn)
- Optional multi-select: Ctrl+click, Shift+click, Shift+arrows, Ctrl+A
- Hover highlight and a tooltip with the full path
- Drag items out to Explorer or other apps (real file drops); accept drops with the standard `AllowDrop` / `DragDrop`
- Height follows the thumbnail size, title lines and font (`AutoHeight`)
- Layout scales with DPI along with the form
- Supports bmp, gif, jpg, png, tif, ico, wmf and emf

## API

### Methods

| Method | Description |
|---|---|
| `AddItem(fileName, [text])` | Adds an image file. Returns the new `FilmStripItem`. |
| `AddItem(image, [text])` | Adds an in-memory image. A thumbnail copy is made; you keep ownership of `image`. |
| `AddItemFromHandle(hBitmap, [text])` | Adds a GDI `HBITMAP`. The handle is copied; you keep ownership of it. |
| `InsertItem(index, fileName \| image, [text])` | Inserts at a position. |
| `AddItemsAsync(fileNames, [cancellationToken])` | Adds many files with background thumbnailing. Skips non-images. Returns the number added. |
| `RemoveItem(index \| item)` | Removes and disposes an item. |
| `Clear()` | Removes and disposes all items. |
| `SelectPrevious()` / `SelectNext()` | Same as clicking the arrow buttons. Returns `False` at the ends. |
| `SetSelected(index, value)`, `SelectAll()`, `ClearSelection()` | Selection control. |
| `EnsureVisible(index)` | Scrolls the item into view. |
| `HitTest(point)` | Item index at a client point, or -1. |
| `GetItemRectangle(index)` | Client rectangle of an item. |
| `StartDrag([allowedEffects])` | Starts a drag of the selected items. |
| `IndexOf(item)` | Index of an item, or -1. |
| `FilmStrip.LoadThumbnail(fileName, maxSize)` | Shared helper: file → thumbnail bitmap. |
| `FilmStrip.CreateThumbnail(image, maxSize)` | Shared helper: image → thumbnail bitmap. |

### Properties (designer)

| Property | Default | Description |
|---|---|---|
| `ThumbnailSize` | 81 × 73 | Maximum thumbnail size. Applies to items added after the change. |
| `ItemSpacing` | 8 | Gap between items, in pixels. |
| `NavigationButtonWidth` | 40 | Width of the arrow buttons; 0 hides them. |
| `ShowTitles` / `TitleLines` | True / 2 | Titles under the thumbnails. |
| `AutoHeight` | True | Height follows the content. |
| `MultiSelect` | False | Ctrl/Shift selection. |
| `ItemDragMode` | Manual | `Manual`: handle `ItemDrag` yourself. `Automatic`: the control starts the drag. |
| `SmallChange` / `LargeChange` | 1 / 5 | Scroll steps, in items. |
| `ShowItemToolTips` | True | Tooltips on hover. |
| `ArrowColor`, `ArrowDisabledColor`, `ArrowHotColor`, `SelectionColor` | | Colours. |

The standard `BackColor` (including `Transparent`), `ForeColor`, `Font`, `BorderStyle`, `Enabled` and `AllowDrop` properties work as usual.

### Properties (runtime)

`Items`, `Count`, `SelectedIndex`, `SelectedItem`, `SelectedItems`, `SelectedIndices`, `FirstVisibleIndex`, `VisibleCount`, `CanSelectPrevious`, `CanSelectNext`

### Events

| Event | Raised when |
|---|---|
| `ItemClick` | An item is clicked, or selected with the arrow buttons (`e.Button` is `None` then). |
| `ItemDoubleClick` | An item is double-clicked. |
| `SelectedIndexChanged` | The selection or the current item changes (mouse, keyboard or code). |
| `ItemDrag` | The user starts dragging an item. |
| `Scroll` | The first visible item changes. |

### FilmStripItem

| Member | Description |
|---|---|
| `Path` | Source file, or `Nothing` for in-memory images. |
| `Text` | Title. Defaults to the file name. |
| `ToolTipText` | Defaults to the full path. |
| `Image` | The thumbnail. Owned by the item and disposed when it's removed, so copy it if you need to keep it. |
| `Tag` | Your own data. |
| `Selected`, `Index`, `Owner` | Read-only state. |

## Migrating from the VB6 control

| VB6 | VB.NET |
|---|---|
| `AddItem path, index` | `AddItem(path)` / `InsertItem(index, path)` |
| `AddItemFromPicStd`, `AddItemFromDibSection` (empty in VB6) | `AddItem(image)` |
| `AddItemFromHandle` (empty in VB6) | `AddItemFromHandle(hBitmap)` |
| `ListIndex` / `ListCount` / `List(i)` | `SelectedIndex` / `Count` / `Items(i).Path` |
| `Click(Item)` / `DblClick(Item)` | `ItemClick` / `ItemDoubleClick` (`e.Index`, `e.Item`) |
| `Change` (never raised in VB6) | `SelectedIndexChanged` |
| `MultiSelect` (not implemented in VB6) | `MultiSelect` (works) |
| `OLEDropMode`, `OLEDragOver`, `OLEDragDrop` | `AllowDrop`, `DragOver`, `DragDrop` |
| `OLEDragMode` / `OLEDrag` / `OLEStartDrag` | `ItemDragMode` / `StartDrag()` / `ItemDrag` |
| `OLEGiveFeedback`, `OLECompleteDrag` | `GiveFeedback`, `QueryContinueDrag`, return value of `StartDrag()` |
| `BackStyle` = transparent | `BackColor = Color.Transparent` |
| `cDIBSection` resampling | Built in (GDI+ bicubic) |

Behaviour differences:

- The back/next buttons scroll only as far as needed to show the selected item, instead of making it the first visible one.
- The height isn't fixed at 1785 twips; it follows `ThumbnailSize`, `TitleLines` and `Font` (turn off `AutoHeight` to size it yourself).
- `ItemClick` is raised on mouse-up rather than mouse-down; `MouseDown` is still raised as before.

## Test app

The FilmStripTest window has the strip at the top, a toolbar, a large preview of the selected image and an event log. You can:

- add files, a whole folder (click again to cancel), or 10 generated test images (no files needed)
- remove, clear, and use Prev/Next
- save the selected thumbnail as a PNG under `Application.StartupPath\Thumbnails`
- toggle MultiSelect, automatic dragging, titles and Enabled; switch the thumbnail size
- drag image files from Explorer onto the strip, or drag items out
- double-click an item to open it in the default image viewer
