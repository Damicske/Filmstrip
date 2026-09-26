Option Strict On
Option Explicit On
Option Infer On

Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Windows.Forms

''' <summary>How dragging an item out of the strip is started (VB6: OLEDragMode).</summary>
Public Enum FilmStripDragMode
    ''' <summary>Only the ItemDrag event is raised; call StartDrag (or DoDragDrop) from it yourself.</summary>
    Manual = 0
    ''' <summary>ItemDrag is raised and then a drag of the selected items starts automatically.</summary>
    Automatic = 1
End Enum

''' <summary>Event data for FilmStrip item events.</summary>
Public Class FilmStripItemEventArgs
    Inherits EventArgs

    Public Sub New(index As Integer, item As FilmStripItem, button As MouseButtons)
        Me.Index = index
        Me.Item = item
        Me.Button = button
    End Sub

    ''' <summary>Index of the item in the strip.</summary>
    Public ReadOnly Property Index As Integer
    ''' <summary>The item itself.</summary>
    Public ReadOnly Property Item As FilmStripItem
    ''' <summary>Mouse button used; MouseButtons.None when raised by the back/next buttons.</summary>
    Public ReadOnly Property Button As MouseButtons
End Class

''' <summary>
''' A horizontal strip of image thumbnails with titles, back/next arrow buttons and a scrollbar.
''' VB.NET WinForms port of the VB6 FilmStrip.ocx user control.
''' </summary>
<DefaultEvent("ItemClick"), DefaultProperty("ThumbnailSize"), ToolboxItem(True),
 Description("A horizontal strip of image thumbnails with back/next buttons and a scrollbar.")>
Public Class FilmStrip
    Inherits UserControl

#Region "Private types and fields"

    Private Enum HitPart
        None
        BackButton
        NextButton
        Item
    End Enum

    Private Enum SelectionAction
        Replace
        Toggle
        Range
    End Enum

    Private Const ExifOrientationId As Integer = &H112
    Private Const VerticalPadding As Integer = 2

    Private ReadOnly _items As New List(Of FilmStripItem)()
    Private ReadOnly _itemsView As ReadOnlyCollection(Of FilmStripItem)
    Private ReadOnly _scrollBar As HScrollBar
    Private ReadOnly _toolTip As ToolTip

    ' Layout (defaults are the VB6 twip sizes converted to pixels at 96 dpi)
    Private _thumbnailSize As New Size(81, 73)      ' 1215 x 1095 twips
    Private _itemSpacing As Integer = 8             ' 120 twips
    Private _buttonWidth As Integer = 40            ' 600 twips
    Private _titleLines As Integer = 2
    Private _showTitles As Boolean = True
    Private _autoHeight As Boolean = True

    ' Behaviour
    Private _multiSelect As Boolean
    Private _itemDragMode As FilmStripDragMode = FilmStripDragMode.Manual
    Private _smallChange As Integer = 1
    Private _largeChange As Integer = 5
    Private _showItemToolTips As Boolean = True

    ' Colours (arrow colours taken from the original GIFs in UserControl1.ctx)
    Private _arrowColor As Color = Color.FromArgb(64, 64, 64)
    Private _arrowDisabledColor As Color = Color.FromArgb(204, 204, 204)
    Private _arrowHotColor As Color = SystemColors.HotTrack
    Private _selectionColor As Color = SystemColors.Highlight

    ' State
    Private _firstVisible As Integer
    Private _currentIndex As Integer = -1
    Private _anchorIndex As Integer = -1
    Private _hotIndex As Integer = -1
    Private _hotPart As HitPart = HitPart.None
    Private _pressedPart As HitPart = HitPart.None
    Private _toolTipIndex As Integer = -1
    Private _mouseDownPoint As Point
    Private _mouseDownIndex As Integer = -1
    Private _deferredSelectIndex As Integer = -1
    Private _dragStarted As Boolean
    Private _doubleClickHandled As Boolean
    Private _updatingScrollBar As Boolean
    Private _wheelDelta As Integer

#End Region

#Region "Events"

    ''' <summary>Occurs when an item is clicked, or selected with the back/next buttons (VB6: Click(Item)).</summary>
    <Category("Action"), Description("Occurs when an item is clicked, or selected with the back/next buttons.")>
    Public Event ItemClick As EventHandler(Of FilmStripItemEventArgs)

    ''' <summary>Occurs when an item is double-clicked (VB6: DblClick(Item)).</summary>
    <Category("Action"), Description("Occurs when an item is double-clicked.")>
    Public Event ItemDoubleClick As EventHandler(Of FilmStripItemEventArgs)

    ''' <summary>Occurs when the selection or the current item changes (VB6: Change).</summary>
    <Category("Behavior"), Description("Occurs when the selection or the current item changes.")>
    Public Event SelectedIndexChanged As EventHandler

    ''' <summary>Occurs when the user starts dragging an item (VB6: OLEStartDrag).</summary>
    <Category("Drag Drop"), Description("Occurs when the user starts dragging an item.")>
    Public Event ItemDrag As ItemDragEventHandler

    ' The inherited Scroll event is raised whenever the first visible item changes (VB6: Scroll).
    ' The inherited AllowDrop / DragEnter / DragOver / DragDrop / GiveFeedback / QueryContinueDrag
    ' members replace VB6 OLEDropMode / OLEDragOver / OLEDragDrop / OLEGiveFeedback / OLECompleteDrag.

#End Region

#Region "Construction / disposal"

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or
                 ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or
                 ControlStyles.ResizeRedraw Or
                 ControlStyles.Selectable Or
                 ControlStyles.SupportsTransparentBackColor, True)

        _itemsView = _items.AsReadOnly()

        _scrollBar = New HScrollBar() With {
            .Dock = DockStyle.Bottom,
            .Enabled = False,
            .TabStop = False,
            .Minimum = 0,
            .Maximum = 0,
            .SmallChange = 1,
            .LargeChange = 1
        }
        AddHandler _scrollBar.Scroll, AddressOf ScrollBar_Scroll
        Controls.Add(_scrollBar)

        _toolTip = New ToolTip()

        UpdateAutoHeight()
        UpdateScrollBar()
    End Sub

    Protected Overrides ReadOnly Property DefaultSize As Size
        Get
            Return New Size(308, 119)
        End Get
    End Property

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            If _items IsNot Nothing Then
                For Each it In _items
                    it.SetOwner(Nothing)
                    it.Dispose()
                Next
                _items.Clear()
            End If
            _toolTip?.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

    ' Overrides can be called by the base constructor before our fields exist.
    Private ReadOnly Property IsInitialized As Boolean
        Get
            Return _scrollBar IsNot Nothing
        End Get
    End Property

#End Region

#Region "Public properties - appearance / layout"

    ''' <summary>Maximum size of a thumbnail in pixels. Thumbnails are created at this size when items are added.</summary>
    <Category("Layout"), DefaultValue(GetType(Size), "81, 73"),
     Description("Maximum size of a thumbnail in pixels. Applies to items added after the change; existing thumbnails are scaled down to fit if needed.")>
    Public Property ThumbnailSize As Size
        Get
            Return _thumbnailSize
        End Get
        Set(value As Size)
            If value.Width < 8 OrElse value.Height < 8 Then Throw New ArgumentOutOfRangeException(NameOf(value), "Thumbnail size must be at least 8 x 8.")
            If value = _thumbnailSize Then Return
            _thumbnailSize = value
            OnLayoutMetricsChanged()
        End Set
    End Property

    ''' <summary>Horizontal gap between items in pixels.</summary>
    <Category("Layout"), DefaultValue(8), Description("Horizontal gap between items in pixels.")>
    Public Property ItemSpacing As Integer
        Get
            Return _itemSpacing
        End Get
        Set(value As Integer)
            ArgumentOutOfRangeException.ThrowIfNegative(value, NameOf(value))
            If value = _itemSpacing Then Return
            _itemSpacing = value
            OnLayoutMetricsChanged()
        End Set
    End Property

    ''' <summary>Width of the back and next arrow buttons in pixels. 0 hides them.</summary>
    <Category("Layout"), DefaultValue(40), Description("Width of the back and next arrow buttons in pixels. 0 hides them.")>
    Public Property NavigationButtonWidth As Integer
        Get
            Return _buttonWidth
        End Get
        Set(value As Integer)
            ArgumentOutOfRangeException.ThrowIfNegative(value, NameOf(value))
            If value = _buttonWidth Then Return
            _buttonWidth = value
            OnLayoutMetricsChanged()
        End Set
    End Property

    ''' <summary>Show the item titles under the thumbnails.</summary>
    <Category("Appearance"), DefaultValue(True), Description("Show the item titles under the thumbnails.")>
    Public Property ShowTitles As Boolean
        Get
            Return _showTitles
        End Get
        Set(value As Boolean)
            If value = _showTitles Then Return
            _showTitles = value
            OnLayoutMetricsChanged()
        End Set
    End Property

    ''' <summary>Number of text lines reserved for an item title (1-5).</summary>
    <Category("Appearance"), DefaultValue(2), Description("Number of text lines reserved for an item title (1-5).")>
    Public Property TitleLines As Integer
        Get
            Return _titleLines
        End Get
        Set(value As Integer)
            If value < 1 OrElse value > 5 Then Throw New ArgumentOutOfRangeException(NameOf(value))
            If value = _titleLines Then Return
            _titleLines = value
            OnLayoutMetricsChanged()
        End Set
    End Property

    ''' <summary>When True the control height follows the thumbnail size, title lines and font (the VB6 control had a fixed height).</summary>
    <Category("Layout"), DefaultValue(True), Description("When True the control height follows the thumbnail size, title lines and font.")>
    Public Property AutoHeight As Boolean
        Get
            Return _autoHeight
        End Get
        Set(value As Boolean)
            If value = _autoHeight Then Return
            _autoHeight = value
            UpdateAutoHeight()
        End Set
    End Property

    ''' <summary>Colour of the enabled back/next arrows.</summary>
    <Category("Appearance"), DefaultValue(GetType(Color), "64, 64, 64"), Description("Colour of the enabled back/next arrows.")>
    Public Property ArrowColor As Color
        Get
            Return _arrowColor
        End Get
        Set(value As Color)
            _arrowColor = value
            Invalidate()
        End Set
    End Property

    ''' <summary>Colour of the disabled back/next arrows.</summary>
    <Category("Appearance"), DefaultValue(GetType(Color), "204, 204, 204"), Description("Colour of the disabled back/next arrows.")>
    Public Property ArrowDisabledColor As Color
        Get
            Return _arrowDisabledColor
        End Get
        Set(value As Color)
            _arrowDisabledColor = value
            Invalidate()
        End Set
    End Property

    ''' <summary>Colour of an arrow while the mouse is over it.</summary>
    <Category("Appearance"), DefaultValue(GetType(Color), "HotTrack"), Description("Colour of an arrow while the mouse is over it.")>
    Public Property ArrowHotColor As Color
        Get
            Return _arrowHotColor
        End Get
        Set(value As Color)
            _arrowHotColor = value
            Invalidate()
        End Set
    End Property

    ''' <summary>Colour used for the selection and hover highlight.</summary>
    <Category("Appearance"), DefaultValue(GetType(Color), "Highlight"), Description("Colour used for the selection and hover highlight.")>
    Public Property SelectionColor As Color
        Get
            Return _selectionColor
        End Get
        Set(value As Color)
            _selectionColor = value
            Invalidate()
        End Set
    End Property

#End Region

#Region "Public properties - behaviour"

    ''' <summary>Allow selecting several items with Ctrl/Shift + click or Shift + arrow keys.</summary>
    <Category("Behavior"), DefaultValue(False), Description("Allow selecting several items with Ctrl/Shift + click or Shift + arrow keys.")>
    Public Property MultiSelect As Boolean
        Get
            Return _multiSelect
        End Get
        Set(value As Boolean)
            If value = _multiSelect Then Return
            _multiSelect = value
            If Not value AndAlso IsInitialized Then
                ' keep only the current item selected
                SelectCore(If(_currentIndex >= 0 AndAlso _items(_currentIndex).Selected, _currentIndex, SelectedIndex), SelectionAction.Replace)
            End If
        End Set
    End Property

    ''' <summary>How dragging an item out of the strip is started (VB6: OLEDragMode).</summary>
    <Category("Drag Drop"), DefaultValue(FilmStripDragMode.Manual), Description("How dragging an item out of the strip is started.")>
    Public Property ItemDragMode As FilmStripDragMode
        Get
            Return _itemDragMode
        End Get
        Set(value As FilmStripDragMode)
            _itemDragMode = value
        End Set
    End Property

    ''' <summary>Number of items scrolled by a scrollbar arrow or a mouse-wheel notch.</summary>
    <Category("Behavior"), DefaultValue(1), Description("Number of items scrolled by a scrollbar arrow or a mouse-wheel notch.")>
    Public Property SmallChange As Integer
        Get
            Return _smallChange
        End Get
        Set(value As Integer)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, NameOf(value))
            _smallChange = value
            UpdateScrollBar()
        End Set
    End Property

    ''' <summary>Number of items scrolled when clicking the scrollbar track.</summary>
    <Category("Behavior"), DefaultValue(5), Description("Number of items scrolled when clicking the scrollbar track.")>
    Public Property LargeChange As Integer
        Get
            Return _largeChange
        End Get
        Set(value As Integer)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, NameOf(value))
            _largeChange = value
            UpdateScrollBar()
        End Set
    End Property

    ''' <summary>Show the item's ToolTipText (by default the full path) when hovering it.</summary>
    <Category("Behavior"), DefaultValue(True), Description("Show the item's ToolTipText when hovering it.")>
    Public Property ShowItemToolTips As Boolean
        Get
            Return _showItemToolTips
        End Get
        Set(value As Boolean)
            _showItemToolTips = value
            _toolTipIndex = -1
            _toolTip.SetToolTip(Me, Nothing)
        End Set
    End Property

#End Region

#Region "Public properties - runtime only"

    ''' <summary>The items in the strip (VB6: List / ListCount). Use AddItem / InsertItem / RemoveItem / Clear to change it.</summary>
    <Browsable(False), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public ReadOnly Property Items As IReadOnlyList(Of FilmStripItem)
        Get
            Return _itemsView
        End Get
    End Property

    ''' <summary>Number of items (VB6: ListCount).</summary>
    <Browsable(False)>
    Public ReadOnly Property Count As Integer
        Get
            Return _items.Count
        End Get
    End Property

    ''' <summary>Index of the selected item, or -1 (VB6: ListIndex). With MultiSelect this is the current item if it is selected, otherwise the first selected item.</summary>
    <Browsable(False), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property SelectedIndex As Integer
        Get
            If _currentIndex >= 0 AndAlso _currentIndex < _items.Count AndAlso _items(_currentIndex).Selected Then Return _currentIndex
            Return _items.FindIndex(Function(it) it.Selected)
        End Get
        Set(value As Integer)
            If value < -1 OrElse value >= _items.Count Then Throw New ArgumentOutOfRangeException(NameOf(value))
            SelectCore(value, SelectionAction.Replace)
            EnsureVisible(value)
        End Set
    End Property

    ''' <summary>The selected item, or Nothing.</summary>
    <Browsable(False), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property SelectedItem As FilmStripItem
        Get
            Dim idx = SelectedIndex
            Return If(idx >= 0, _items(idx), Nothing)
        End Get
        Set(value As FilmStripItem)
            If value Is Nothing Then
                SelectedIndex = -1
            Else
                Dim idx = _items.IndexOf(value)
                If idx < 0 Then Throw New ArgumentException("The item is not in this FilmStrip.", NameOf(value))
                SelectedIndex = idx
            End If
        End Set
    End Property

    ''' <summary>All selected items, in strip order.</summary>
    <Browsable(False)>
    Public ReadOnly Property SelectedItems As IReadOnlyList(Of FilmStripItem)
        Get
            Return _items.Where(Function(it) it.Selected).ToList().AsReadOnly()
        End Get
    End Property

    ''' <summary>Indices of all selected items, in ascending order.</summary>
    <Browsable(False)>
    Public ReadOnly Property SelectedIndices As Integer()
        Get
            Return Enumerable.Range(0, _items.Count).Where(Function(n) _items(n).Selected).ToArray()
        End Get
    End Property

    ''' <summary>Index of the left-most visible item (VB6: the scrollbar Value / IconStart).</summary>
    <Browsable(False), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property FirstVisibleIndex As Integer
        Get
            Return _firstVisible
        End Get
        Set(value As Integer)
            SetFirstVisible(value, ScrollEventType.ThumbPosition)
        End Set
    End Property

    ''' <summary>Number of items that fit in the visible area.</summary>
    <Browsable(False)>
    Public ReadOnly Property VisibleCount As Integer
        Get
            If Not IsInitialized Then Return 1
            Dim area = ItemAreaBounds.Width
            Return Math.Max(1, (area + _itemSpacing) \ ItemPitch)
        End Get
    End Property

    ''' <summary>True when the back button is enabled.</summary>
    <Browsable(False)>
    Public ReadOnly Property CanSelectPrevious As Boolean
        Get
            Return _currentIndex > 0
        End Get
    End Property

    ''' <summary>True when the next button is enabled.</summary>
    <Browsable(False)>
    Public ReadOnly Property CanSelectNext As Boolean
        Get
            Return _items.Count > 0 AndAlso _currentIndex < _items.Count - 1
        End Get
    End Property

#End Region

#Region "Public methods - items"

    ''' <summary>Adds an image file to the end of the strip (VB6: AddItem). Throws when the file can't be read as an image.</summary>
    ''' <param name="fileName">Path of the image (bmp, gif, jpg, png, tif, ico, wmf, emf).</param>
    ''' <param name="text">Title; defaults to the file name.</param>
    Public Function AddItem(fileName As String, Optional text As String = Nothing) As FilmStripItem
        Return InsertItem(_items.Count, fileName, text)
    End Function

    ''' <summary>Inserts an image file at the given position (VB6: AddItem with Index).</summary>
    Public Function InsertItem(index As Integer, fileName As String, Optional text As String = Nothing) As FilmStripItem
        If String.IsNullOrEmpty(fileName) Then Throw New ArgumentNullException(NameOf(fileName))
        If index < 0 OrElse index > _items.Count Then Throw New ArgumentOutOfRangeException(NameOf(index))
        Dim thumb = LoadThumbnail(fileName, _thumbnailSize)
        Return InsertCore(index, New FilmStripItem(fileName, text, thumb))
    End Function

    ''' <summary>Adds an in-memory image (VB6: AddItemFromPicStd / AddItemFromDibSection). A thumbnail copy is made; you keep ownership of <paramref name="image"/>.</summary>
    Public Function AddItem(image As Image, Optional text As String = Nothing) As FilmStripItem
        Return InsertItem(_items.Count, image, text)
    End Function

    ''' <summary>Inserts an in-memory image at the given position. A thumbnail copy is made; you keep ownership of <paramref name="image"/>.</summary>
    Public Function InsertItem(index As Integer, image As Image, Optional text As String = Nothing) As FilmStripItem
        ArgumentNullException.ThrowIfNull(image, NameOf(image))
        If index < 0 OrElse index > _items.Count Then Throw New ArgumentOutOfRangeException(NameOf(index))
        Dim thumb = CreateThumbnail(image, _thumbnailSize)
        Return InsertCore(index, New FilmStripItem(Nothing, If(text, String.Empty), thumb))
    End Function

    ''' <summary>Adds an image from a GDI HBITMAP handle (VB6: AddItemFromHandle). The handle is copied; you keep ownership of it.</summary>
    Public Function AddItemFromHandle(hBitmap As IntPtr, Optional text As String = Nothing) As FilmStripItem
        If hBitmap = IntPtr.Zero Then Throw New ArgumentNullException(NameOf(hBitmap))
        Using img = Image.FromHbitmap(hBitmap)
            Return AddItem(img, text)
        End Using
    End Function

    ''' <summary>
    ''' Adds many image files, creating the thumbnails on a background thread so the UI stays responsive.
    ''' Files that can't be read as images are skipped. Returns the number of items added.
    ''' Call it from the UI thread.
    ''' </summary>
    Public Async Function AddItemsAsync(fileNames As IEnumerable(Of String),
                                        Optional cancellationToken As CancellationToken = Nothing) As Task(Of Integer)
        ArgumentNullException.ThrowIfNull(fileNames, NameOf(fileNames))
        Dim size = _thumbnailSize
        Dim added = 0
        For Each fileName In fileNames
            cancellationToken.ThrowIfCancellationRequested()
            If String.IsNullOrEmpty(fileName) Then Continue For
            Dim f = fileName
            Dim thumb = Await Task.Run(Function() TryLoadThumbnail(f, size), cancellationToken)
            If thumb Is Nothing Then Continue For
            If IsDisposed Then
                thumb.Dispose()
                Exit For
            End If
            InsertCore(_items.Count, New FilmStripItem(f, Nothing, thumb))
            added += 1
        Next
        Return added
    End Function

    ''' <summary>Removes (and disposes) the item at the given index (VB6: RemoveItem).</summary>
    Public Sub RemoveItem(index As Integer)
        If index < 0 OrElse index >= _items.Count Then Throw New ArgumentOutOfRangeException(NameOf(index))

        Dim item = _items(index)
        Dim selectionChanged = item.Selected OrElse index = _currentIndex

        _items.RemoveAt(index)

        If _currentIndex = index Then
            _currentIndex = -1
        ElseIf _currentIndex > index Then
            _currentIndex -= 1
        End If
        If _anchorIndex = index Then
            _anchorIndex = -1
        ElseIf _anchorIndex > index Then
            _anchorIndex -= 1
        End If
        ResetHotState()

        item.SetOwner(Nothing)
        item.Dispose()

        UpdateScrollBar()
        Invalidate()
        If selectionChanged Then OnSelectedIndexChanged(EventArgs.Empty)
    End Sub

    ''' <summary>Removes (and disposes) the given item.</summary>
    Public Sub RemoveItem(item As FilmStripItem)
        Dim idx = _items.IndexOf(item)
        If idx >= 0 Then RemoveItem(idx)
    End Sub

    ''' <summary>Removes (and disposes) all items (VB6: Clear).</summary>
    Public Sub Clear()
        Dim hadSelection = _currentIndex >= 0 OrElse _items.Any(Function(it) it.Selected)

        For Each it In _items
            it.SetOwner(Nothing)
            it.Dispose()
        Next
        _items.Clear()

        _currentIndex = -1
        _anchorIndex = -1
        _firstVisible = 0
        ResetHotState()

        UpdateScrollBar()
        Invalidate()
        If hadSelection Then OnSelectedIndexChanged(EventArgs.Empty)
    End Sub

    ''' <summary>Index of the item, or -1.</summary>
    Public Function IndexOf(item As FilmStripItem) As Integer
        Return _items.IndexOf(item)
    End Function

    ' Common insert used by AddItem / InsertItem / AddItemsAsync.
    Private Function InsertCore(index As Integer, item As FilmStripItem) As FilmStripItem
        ArgumentNullException.ThrowIfNull(item, NameOf(item))
        If index < 0 OrElse index > _items.Count Then
            item.Dispose()
            Throw New ArgumentOutOfRangeException(NameOf(index))
        End If

        item.SetOwner(Me)
        _items.Insert(index, item)

        ' Keep the current/anchor item pointing at the same item after the shift
        If _currentIndex >= index Then _currentIndex += 1
        If _anchorIndex >= index Then _anchorIndex += 1

        ' Hover/tooltip indices may now point at a different item
        ResetHotState()

        UpdateScrollBar()
        Invalidate()
        Return item
    End Function

#End Region

#Region "Public methods - selection / navigation"

    ''' <summary>Selects the previous item, like clicking the back button. Returns False when there is none.</summary>
    Public Function SelectPrevious() As Boolean
        If Not CanSelectPrevious Then Return False
        SelectCore(_currentIndex - 1, SelectionAction.Replace)
        EnsureVisible(_currentIndex)
        Return True
    End Function

    ''' <summary>Selects the next item, like clicking the next button. Returns False when there is none.</summary>
    Public Function SelectNext() As Boolean
        If Not CanSelectNext Then Return False
        SelectCore(_currentIndex + 1, SelectionAction.Replace)
        EnsureVisible(_currentIndex)
        Return True
    End Function

    ''' <summary>Selects or deselects one item. Without MultiSelect, selecting an item deselects the others.</summary>
    Public Sub SetSelected(index As Integer, value As Boolean)
        If index < 0 OrElse index >= _items.Count Then Throw New ArgumentOutOfRangeException(NameOf(index))
        If _items(index).Selected = value Then Return
        If Not _multiSelect Then
            SelectCore(If(value, index, -1), SelectionAction.Replace)
        Else
            SelectCore(index, SelectionAction.Toggle)
        End If
    End Sub

    ''' <summary>Selects all items (only with MultiSelect).</summary>
    Public Sub SelectAll()
        If Not _multiSelect OrElse _items.Count = 0 Then Return
        Dim changed = False
        For Each it In _items
            If Not it.Selected Then
                it.Selected = True
                changed = True
            End If
        Next
        If changed Then
            Invalidate()
            OnSelectedIndexChanged(EventArgs.Empty)
        End If
    End Sub

    ''' <summary>Deselects all items.</summary>
    Public Sub ClearSelection()
        SelectCore(-1, SelectionAction.Replace)
    End Sub

    ''' <summary>Scrolls so the item at <paramref name="index"/> is fully visible.</summary>
    Public Sub EnsureVisible(index As Integer)
        If index < 0 OrElse index >= _items.Count Then Return
        If index < _firstVisible Then
            SetFirstVisible(index, ScrollEventType.ThumbPosition)
        ElseIf index > _firstVisible + VisibleCount - 1 Then
            SetFirstVisible(index - VisibleCount + 1, ScrollEventType.ThumbPosition)
        End If
    End Sub

    ''' <summary>Returns the index of the item at the given client point, or -1.</summary>
    Public Function HitTest(pt As Point) As Integer
        Dim idx = -1
        Return If(HitTestCore(pt, idx) = HitPart.Item, idx, -1)
    End Function

    ''' <summary>Returns the index of the item at the given client coordinates, or -1.</summary>
    Public Function HitTest(x As Integer, y As Integer) As Integer
        Return HitTest(New Point(x, y))
    End Function

    ''' <summary>Client rectangle of an item (thumbnail + title). May lie outside the visible area.</summary>
    Public Function GetItemRectangle(index As Integer) As Rectangle
        If index < 0 OrElse index >= _items.Count Then Throw New ArgumentOutOfRangeException(NameOf(index))
        Return GetItemBounds(index)
    End Function

    ''' <summary>
    ''' Starts a drag of the selected items (VB6: OLEDrag). File-based items are offered as a
    ''' file drop list (so Explorer and other apps accept them) plus text; a single item also carries its thumbnail.
    ''' </summary>
    Public Function StartDrag(Optional allowedEffects As DragDropEffects = DragDropEffects.Copy Or DragDropEffects.Link) As DragDropEffects
        Dim selection = SelectedItems
        If selection.Count = 0 Then Return DragDropEffects.None

        Dim data As New DataObject()
        Dim files = selection.Where(Function(it) Not String.IsNullOrEmpty(it.Path) AndAlso File.Exists(it.Path)).
                              Select(Function(it) it.Path).ToArray()
        If files.Length > 0 Then
            Dim fileList As New System.Collections.Specialized.StringCollection()
            fileList.AddRange(files)
            data.SetFileDropList(fileList)
            data.SetText(String.Join(Environment.NewLine, files))
        Else
            data.SetText(String.Join(Environment.NewLine, selection.Select(Function(it) it.Text)))
        End If
        If selection.Count = 1 AndAlso selection(0).Image IsNot Nothing Then
            data.SetImage(selection(0).Image)
        End If

        _toolTip.Hide(Me)
        Return DoDragDrop(data, allowedEffects)
    End Function

    ''' <summary>Recalculates the layout and repaints the control (VB6: Refresh).</summary>
    Public Overrides Sub Refresh()
        UpdateScrollBar()
        MyBase.Refresh()
    End Sub

#End Region

#Region "Public shared helpers"

    ''' <summary>
    ''' Loads an image file and returns a thumbnail no larger than <paramref name="maxSize"/>.
    ''' The file is read into memory first so it is not left locked. EXIF rotation is applied.
    ''' </summary>
    Public Shared Function LoadThumbnail(fileName As String, maxSize As Size) As Bitmap
        Dim bytes = File.ReadAllBytes(fileName)
        Try
            Using ms As New MemoryStream(bytes), img = Image.FromStream(ms)
                ApplyExifOrientation(img)
                Return CreateThumbnail(img, maxSize)
            End Using
        Catch ex As ArgumentException
            Throw New InvalidDataException($"'{fileName}' is not a supported image file.", ex)
        End Try
    End Function

    ''' <summary>
    ''' Returns a high-quality scaled copy of <paramref name="source"/> that fits inside <paramref name="maxSize"/>,
    ''' keeping the aspect ratio. Small images are not enlarged. Replaces cDIBSection.Resample.
    ''' </summary>
    Public Shared Function CreateThumbnail(source As Image, maxSize As Size) As Bitmap
        ArgumentNullException.ThrowIfNull(source, NameOf(source))
        Dim srcW = Math.Max(1, source.Width)
        Dim srcH = Math.Max(1, source.Height)

        Dim scale = 1.0
        If maxSize.Width > 0 AndAlso maxSize.Height > 0 Then
            scale = Math.Min(1.0, Math.Min(maxSize.Width / srcW, maxSize.Height / srcH))
        End If
        Dim w = Math.Max(1, CInt(Math.Round(srcW * scale)))
        Dim h = Math.Max(1, CInt(Math.Round(srcH * scale)))

        Dim bmp As New Bitmap(w, h, PixelFormat.Format32bppPArgb)
        Try
            Using g = Graphics.FromImage(bmp), attrs As New ImageAttributes()
                g.Clear(Color.Transparent)
                g.InterpolationMode = InterpolationMode.HighQualityBicubic
                g.PixelOffsetMode = PixelOffsetMode.HighQuality
                g.CompositingQuality = CompositingQuality.HighQuality
                g.SmoothingMode = SmoothingMode.HighQuality
                attrs.SetWrapMode(WrapMode.TileFlipXY) ' avoids a faint border on the edges
                g.DrawImage(source, New Rectangle(0, 0, w, h), 0, 0, srcW, srcH, GraphicsUnit.Pixel, attrs)
            End Using
        Catch
            bmp.Dispose()
            Throw
        End Try
        Return bmp
    End Function

#End Region

#Region "Protected event raisers"

    Protected Overridable Sub OnItemClick(e As FilmStripItemEventArgs)
        RaiseEvent ItemClick(Me, e)
    End Sub

    Protected Overridable Sub OnItemDoubleClick(e As FilmStripItemEventArgs)
        RaiseEvent ItemDoubleClick(Me, e)
    End Sub

    Protected Overridable Sub OnSelectedIndexChanged(e As EventArgs)
        RaiseEvent SelectedIndexChanged(Me, e)
    End Sub

    Protected Overridable Sub OnItemDrag(e As ItemDragEventArgs)
        RaiseEvent ItemDrag(Me, e)
    End Sub

#End Region

#Region "Layout"

    Private ReadOnly Property TitleHeight As Integer
        Get
            If Not _showTitles Then Return 0
            Return Font.Height * _titleLines + 4
        End Get
    End Property

    Private ReadOnly Property ItemPitch As Integer
        Get
            Return Math.Max(1, _thumbnailSize.Width + _itemSpacing)
        End Get
    End Property

    ' Client height above the scrollbar
    Private ReadOnly Property StripAreaHeight As Integer
        Get
            Return Math.Max(0, ClientSize.Height - _scrollBar.Height)
        End Get
    End Property

    Private ReadOnly Property BackButtonBounds As Rectangle
        Get
            Return New Rectangle(0, 0, _buttonWidth, StripAreaHeight)
        End Get
    End Property

    Private ReadOnly Property NextButtonBounds As Rectangle
        Get
            Return New Rectangle(ClientSize.Width - _buttonWidth, 0, _buttonWidth, StripAreaHeight)
        End Get
    End Property

    Private ReadOnly Property ItemAreaBounds As Rectangle
        Get
            Dim x = _buttonWidth + _itemSpacing
            Dim w = Math.Max(0, ClientSize.Width - 2 * x)
            Return New Rectangle(x, 0, w, StripAreaHeight)
        End Get
    End Property

    Private ReadOnly Property MaxFirstVisibleIndex As Integer
        Get
            Return Math.Max(0, _items.Count - VisibleCount)
        End Get
    End Property

    Private Function GetItemBounds(index As Integer) As Rectangle
        Dim cellHeight = _thumbnailSize.Height + TitleHeight
        Dim top = Math.Max(0, (StripAreaHeight - cellHeight) \ 2)
        Dim x = ItemAreaBounds.X + (index - _firstVisible) * ItemPitch
        Return New Rectangle(x, top, _thumbnailSize.Width, cellHeight)
    End Function

    Private Function GetPreferredHeight() As Integer
        Dim nonClient = Height - ClientSize.Height
        Return _thumbnailSize.Height + TitleHeight + 2 * VerticalPadding + _scrollBar.Height + nonClient
    End Function

    Private Function AutoHeightApplies() As Boolean
        Return _autoHeight AndAlso Dock <> DockStyle.Fill AndAlso Dock <> DockStyle.Left AndAlso Dock <> DockStyle.Right
    End Function

    Private Sub UpdateAutoHeight()
        If Not IsInitialized OrElse Not AutoHeightApplies() Then Return
        Dim h = GetPreferredHeight()
        If Height <> h Then Height = h
    End Sub

    Protected Overrides Sub SetBoundsCore(x As Integer, y As Integer, width As Integer, height As Integer, specified As BoundsSpecified)
        If IsInitialized AndAlso AutoHeightApplies() Then height = GetPreferredHeight()
        MyBase.SetBoundsCore(x, y, width, height, specified)
    End Sub

    Private Sub OnLayoutMetricsChanged()
        If Not IsInitialized Then Return
        UpdateAutoHeight()
        UpdateScrollBar()
        Invalidate()
    End Sub

    Private Sub UpdateScrollBar()
        If Not IsInitialized Then Return
        Dim maxFirst = MaxFirstVisibleIndex
        If _firstVisible > maxFirst Then _firstVisible = maxFirst

        _updatingScrollBar = True
        Try
            ' A .NET scrollbar can only reach Maximum - LargeChange + 1, so add LargeChange - 1.
            _scrollBar.Minimum = 0
            _scrollBar.Maximum = maxFirst + _largeChange - 1
            _scrollBar.LargeChange = _largeChange
            _scrollBar.SmallChange = _smallChange
            _scrollBar.Value = _firstVisible
            _scrollBar.Enabled = Enabled AndAlso maxFirst > 0
        Finally
            _updatingScrollBar = False
        End Try
    End Sub

    Private Sub SetFirstVisible(value As Integer, type As ScrollEventType)
        If value < 0 Then value = 0
        Dim maxFirst = MaxFirstVisibleIndex
        If value > maxFirst Then value = maxFirst
        If value = _firstVisible Then Return

        Dim oldValue = _firstVisible
        _firstVisible = value

        If _scrollBar.Value <> value Then
            _updatingScrollBar = True
            Try
                _scrollBar.Value = value
            Finally
                _updatingScrollBar = False
            End Try
        End If

        Invalidate()
        If IsHandleCreated Then UpdateHotTracking(PointToClient(Cursor.Position))
        OnScroll(New ScrollEventArgs(type, oldValue, value, ScrollOrientation.HorizontalScroll))
    End Sub

    Private Sub ScrollBar_Scroll(sender As Object, e As ScrollEventArgs)
        If _updatingScrollBar Then Return
        SetFirstVisible(e.NewValue, e.Type)
    End Sub

    Protected Overrides Sub OnClientSizeChanged(e As EventArgs)
        MyBase.OnClientSizeChanged(e)
        If Not IsInitialized Then Return
        UpdateAutoHeight() ' picks up border style changes
        UpdateScrollBar()
        Invalidate()
    End Sub

    Protected Overrides Sub OnDockChanged(e As EventArgs)
        MyBase.OnDockChanged(e)
        UpdateAutoHeight()
    End Sub

    Protected Overrides Sub OnFontChanged(e As EventArgs)
        MyBase.OnFontChanged(e)
        OnLayoutMetricsChanged()
    End Sub

    Protected Overrides Sub OnEnabledChanged(e As EventArgs)
        MyBase.OnEnabledChanged(e)
        UpdateScrollBar()
        Invalidate()
    End Sub

    ''' <summary>Scales the pixel-based layout properties along with the form (DPI / font auto-scaling).</summary>
    Protected Overrides Sub ScaleControl(factor As SizeF, specified As BoundsSpecified)
        If IsInitialized Then
            If (specified And BoundsSpecified.Width) <> 0 AndAlso factor.Width <> 1.0F Then
                _itemSpacing = CInt(Math.Round(_itemSpacing * factor.Width))
                _buttonWidth = CInt(Math.Round(_buttonWidth * factor.Width))
                _thumbnailSize.Width = Math.Max(8, CInt(Math.Round(_thumbnailSize.Width * factor.Width)))
            End If
            If (specified And BoundsSpecified.Height) <> 0 AndAlso factor.Height <> 1.0F Then
                _thumbnailSize.Height = Math.Max(8, CInt(Math.Round(_thumbnailSize.Height * factor.Height)))
            End If
        End If
        MyBase.ScaleControl(factor, specified)
        OnLayoutMetricsChanged()
    End Sub

#End Region

#Region "Selection core"

    Private Sub SelectCore(index As Integer, action As SelectionAction)
        If Not _multiSelect Then action = SelectionAction.Replace
        Dim changed = False

        Select Case action
            Case SelectionAction.Replace
                For n = 0 To _items.Count - 1
                    Dim want = (n = index)
                    If _items(n).Selected <> want Then
                        _items(n).Selected = want
                        changed = True
                    End If
                Next
                _anchorIndex = index

            Case SelectionAction.Toggle
                If index >= 0 Then
                    _items(index).Selected = Not _items(index).Selected
                    changed = True
                End If
                _anchorIndex = index

            Case SelectionAction.Range
                Dim anchor = If(_anchorIndex >= 0 AndAlso _anchorIndex < _items.Count, _anchorIndex, index)
                Dim lo = Math.Min(anchor, index)
                Dim hi = Math.Max(anchor, index)
                For n = 0 To _items.Count - 1
                    Dim want = (n >= lo AndAlso n <= hi)
                    If _items(n).Selected <> want Then
                        _items(n).Selected = want
                        changed = True
                    End If
                Next
                If _anchorIndex < 0 Then _anchorIndex = index
        End Select

        If _currentIndex <> index Then
            _currentIndex = index
            changed = True
        End If

        If changed Then
            Invalidate()
            OnSelectedIndexChanged(EventArgs.Empty)
        End If
    End Sub

#End Region

#Region "Painting"

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        If Not IsInitialized Then Return
        Dim g = e.Graphics

        DrawNavigationButton(g, BackButtonBounds, True, CanSelectPrevious, HitPart.BackButton)
        DrawNavigationButton(g, NextButtonBounds, False, CanSelectNext, HitPart.NextButton)

        If _items.Count = 0 Then Return
        Dim area = ItemAreaBounds
        If area.Width <= 0 OrElse area.Height <= 0 Then Return

        Dim state = g.Save()
        Try
            g.SetClip(area, CombineMode.Intersect)
            Dim last = Math.Min(_items.Count - 1, _firstVisible + VisibleCount - 1)
            For n = _firstVisible To last
                Dim r = GetItemBounds(n)
                If r.IntersectsWith(e.ClipRectangle) Then DrawItem(g, n, r)
            Next
        Finally
            g.Restore(state)
        End Try
    End Sub

    Private Sub DrawNavigationButton(g As Graphics, bounds As Rectangle, pointsLeft As Boolean, canNavigate As Boolean, part As HitPart)
        If bounds.Width <= 0 OrElse bounds.Height <= 0 Then Return

        Dim active = canNavigate AndAlso Enabled
        Dim arrow As Color
        If Not active Then
            arrow = _arrowDisabledColor
        ElseIf _hotPart = part Then
            arrow = _arrowHotColor
        Else
            arrow = _arrowColor
        End If

        ' Same proportions as the original 40x73 GIFs: a 20 x 54 triangle.
        Dim h = bounds.Height * 0.74F
        Dim w = h * 20.0F / 54.0F
        If w > bounds.Width * 0.6F Then
            w = bounds.Width * 0.6F
            h = w * 54.0F / 20.0F
        End If
        Dim cx = bounds.X + bounds.Width / 2.0F
        Dim cy = bounds.Y + bounds.Height / 2.0F
        If active AndAlso _pressedPart = part AndAlso _hotPart = part Then
            cx += 1
            cy += 1
        End If

        Dim pts As PointF()
        If pointsLeft Then
            pts = {New PointF(cx - w / 2, cy), New PointF(cx + w / 2, cy - h / 2), New PointF(cx + w / 2, cy + h / 2)}
        Else
            pts = {New PointF(cx + w / 2, cy), New PointF(cx - w / 2, cy - h / 2), New PointF(cx - w / 2, cy + h / 2)}
        End If

        Dim oldMode = g.SmoothingMode
        g.SmoothingMode = SmoothingMode.AntiAlias
        Using b As New SolidBrush(arrow)
            g.FillPolygon(b, pts)
        End Using
        g.SmoothingMode = oldMode
    End Sub

    Private Sub DrawItem(g As Graphics, index As Integer, bounds As Rectangle)
        Dim item = _items(index)
        Dim thumbRect As New Rectangle(bounds.X, bounds.Y, _thumbnailSize.Width, _thumbnailSize.Height)
        Dim titleRect As New Rectangle(bounds.X, thumbRect.Bottom, bounds.Width, TitleHeight)
        Dim isHot = Enabled AndAlso index = _hotIndex

        ' background highlight
        If item.Selected Then
            Using b As New SolidBrush(Color.FromArgb(If(Focused, 70, 40), _selectionColor))
                g.FillRectangle(b, bounds)
            End Using
        ElseIf isHot Then
            Using b As New SolidBrush(Color.FromArgb(25, _selectionColor))
                g.FillRectangle(b, bounds)
            End Using
        End If

        ' thumbnail
        Dim img = item.Image
        If img IsNot Nothing Then
            Dim dest = FitRectangle(img.Size, Rectangle.Inflate(thumbRect, -2, -2))
            If Not dest.IsEmpty Then
                g.InterpolationMode = InterpolationMode.HighQualityBicubic
                g.PixelOffsetMode = PixelOffsetMode.Half
                g.DrawImage(img, dest)
                If Not Enabled Then
                    Using b As New SolidBrush(Color.FromArgb(140, BackColor))
                        g.FillRectangle(b, dest)
                    End Using
                End If
            End If
        End If

        ' title
        If _showTitles AndAlso titleRect.Height > 0 Then
            Const flags As TextFormatFlags = TextFormatFlags.HorizontalCenter Or TextFormatFlags.Top Or
                                             TextFormatFlags.WordBreak Or TextFormatFlags.EndEllipsis Or
                                             TextFormatFlags.NoPrefix Or TextFormatFlags.TextBoxControl Or
                                             TextFormatFlags.PreserveGraphicsClipping
            TextRenderer.DrawText(g, item.Text, Font, titleRect, If(Enabled, ForeColor, SystemColors.GrayText), flags)
        End If

        ' border
        If item.Selected Then
            Using p As New Pen(_selectionColor)
                g.DrawRectangle(p, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1)
            End Using
        ElseIf isHot Then
            Using p As New Pen(Color.FromArgb(120, _selectionColor))
                g.DrawRectangle(p, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1)
            End Using
        End If

        If index = _currentIndex AndAlso Focused AndAlso ShowFocusCues Then
            ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(bounds, -2, -2))
        End If
    End Sub

    Private Shared Function FitRectangle(imageSize As Size, box As Rectangle) As Rectangle
        If imageSize.Width <= 0 OrElse imageSize.Height <= 0 OrElse box.Width <= 0 OrElse box.Height <= 0 Then Return Rectangle.Empty
        Dim scale = Math.Min(1.0, Math.Min(box.Width / imageSize.Width, box.Height / imageSize.Height))
        Dim w = Math.Max(1, CInt(Math.Round(imageSize.Width * scale)))
        Dim h = Math.Max(1, CInt(Math.Round(imageSize.Height * scale)))
        Return New Rectangle(box.X + (box.Width - w) \ 2, box.Y + (box.Height - h) \ 2, w, h)
    End Function

    Friend Sub InvalidateItem(item As FilmStripItem)
        Dim idx = _items.IndexOf(item)
        If idx >= 0 Then Invalidate(GetItemBounds(idx))
    End Sub

    Protected Overrides Sub OnGotFocus(e As EventArgs)
        MyBase.OnGotFocus(e)
        Invalidate()
    End Sub

    Protected Overrides Sub OnLostFocus(e As EventArgs)
        MyBase.OnLostFocus(e)
        Invalidate()
    End Sub

#End Region

#Region "Mouse"

    Private Function HitTestCore(pt As Point, ByRef index As Integer) As HitPart
        index = -1
        If Not IsInitialized Then Return HitPart.None
        If BackButtonBounds.Contains(pt) Then Return HitPart.BackButton
        If NextButtonBounds.Contains(pt) Then Return HitPart.NextButton
        Dim area = ItemAreaBounds
        If Not area.Contains(pt) Then Return HitPart.None
        Dim last = Math.Min(_items.Count - 1, _firstVisible + VisibleCount - 1)
        For n = _firstVisible To last
            If GetItemBounds(n).Contains(pt) Then
                index = n
                Return HitPart.Item
            End If
        Next
        Return HitPart.None
    End Function

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e) ' raises the MouseDown event (VB6 parity)
        If Not Focused Then Focus()

        Dim idx = -1
        Dim part = HitTestCore(e.Location, idx)
        _pressedPart = part
        _mouseDownPoint = e.Location
        _mouseDownIndex = -1
        _deferredSelectIndex = -1
        _dragStarted = False
        _doubleClickHandled = False

        Select Case part
            Case HitPart.BackButton
                Invalidate(BackButtonBounds)
            Case HitPart.NextButton
                Invalidate(NextButtonBounds)
            Case HitPart.Item
                _mouseDownIndex = idx
                Dim mods = ModifierKeys
                If _multiSelect AndAlso (mods And Keys.Control) = Keys.Control Then
                    SelectCore(idx, SelectionAction.Toggle)
                ElseIf _multiSelect AndAlso (mods And Keys.Shift) = Keys.Shift Then
                    SelectCore(idx, SelectionAction.Range)
                ElseIf _items(idx).Selected AndAlso MoreThanOneSelected() Then
                    ' Clicking inside a multi-selection: wait for mouse-up so the user can drag the whole
                    ' selection. A right-click keeps the selection (for context menus).
                    If e.Button = MouseButtons.Left Then _deferredSelectIndex = idx
                Else
                    SelectCore(idx, SelectionAction.Replace)
                End If
        End Select
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)

        If _mouseDownIndex >= 0 AndAlso e.Button <> MouseButtons.None AndAlso Not _dragStarted Then
            Dim dragSize = SystemInformation.DragSize
            Dim dragRect As New Rectangle(_mouseDownPoint.X - dragSize.Width \ 2, _mouseDownPoint.Y - dragSize.Height \ 2, dragSize.Width, dragSize.Height)
            If Not dragRect.Contains(e.Location) Then
                _dragStarted = True
                Dim dragIndex = _mouseDownIndex
                If _deferredSelectIndex >= 0 Then
                    ' dragging a multi-selection: keep it
                    _deferredSelectIndex = -1
                    If _currentIndex <> dragIndex Then
                        _currentIndex = dragIndex
                        Invalidate()
                    End If
                End If
                _toolTip.Hide(Me)
                OnItemDrag(New ItemDragEventArgs(e.Button, _items(dragIndex)))
                If _itemDragMode = FilmStripDragMode.Automatic AndAlso e.Button = MouseButtons.Left Then
                    StartDrag()
                End If
                ' DoDragDrop swallows the mouse-up, so reset here.
                _mouseDownIndex = -1
                _pressedPart = HitPart.None
                Return
            End If
        End If

        UpdateHotTracking(e.Location)
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        MyBase.OnMouseUp(e)

        Dim idx = -1
        Dim part = HitTestCore(e.Location, idx)
        Dim pressed = _pressedPart
        Dim downIndex = _mouseDownIndex
        Dim deferred = _deferredSelectIndex
        Dim wasDoubleClick = _doubleClickHandled

        _pressedPart = HitPart.None
        _mouseDownIndex = -1
        _deferredSelectIndex = -1
        _doubleClickHandled = False

        If _dragStarted Then
            _dragStarted = False
            Return
        End If

        Select Case pressed
            Case HitPart.BackButton
                Invalidate(BackButtonBounds)
                If part = HitPart.BackButton AndAlso e.Button = MouseButtons.Left AndAlso SelectPrevious() Then
                    OnItemClick(New FilmStripItemEventArgs(_currentIndex, _items(_currentIndex), MouseButtons.None))
                End If

            Case HitPart.NextButton
                Invalidate(NextButtonBounds)
                If part = HitPart.NextButton AndAlso e.Button = MouseButtons.Left AndAlso SelectNext() Then
                    OnItemClick(New FilmStripItemEventArgs(_currentIndex, _items(_currentIndex), MouseButtons.None))
                End If

            Case HitPart.Item
                If part = HitPart.Item AndAlso idx = downIndex Then
                    If deferred = idx Then SelectCore(idx, SelectionAction.Replace)
                    If Not wasDoubleClick Then
                        OnItemClick(New FilmStripItemEventArgs(idx, _items(idx), e.Button))
                    End If
                End If
        End Select
    End Sub

    Protected Overrides Sub OnMouseDoubleClick(e As MouseEventArgs)
        MyBase.OnMouseDoubleClick(e)
        Dim idx = -1
        If HitTestCore(e.Location, idx) = HitPart.Item Then
            _doubleClickHandled = True
            OnItemDoubleClick(New FilmStripItemEventArgs(idx, _items(idx), e.Button))
        End If
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        UpdateHotTracking(New Point(-1, -1))
    End Sub

    Protected Overrides Sub OnMouseWheel(e As MouseEventArgs)
        MyBase.OnMouseWheel(e)
        If _items.Count = 0 Then Return

        _wheelDelta += e.Delta
        Dim notches = _wheelDelta \ SystemInformation.MouseWheelScrollDelta
        If notches = 0 Then Return
        _wheelDelta -= notches * SystemInformation.MouseWheelScrollDelta

        SetFirstVisible(_firstVisible - notches * _smallChange,
                        If(notches > 0, ScrollEventType.SmallDecrement, ScrollEventType.SmallIncrement))

        Dim handled = TryCast(e, HandledMouseEventArgs)
        If handled IsNot Nothing Then handled.Handled = True
    End Sub

    Private Sub UpdateHotTracking(pt As Point)
        Dim idx = -1
        Dim part = If(ClientRectangle.Contains(pt), HitTestCore(pt, idx), HitPart.None)
        If part <> HitPart.Item Then idx = -1

        If part <> _hotPart OrElse idx <> _hotIndex Then
            InvalidatePart(_hotPart, _hotIndex)
            _hotPart = part
            _hotIndex = idx
            InvalidatePart(part, idx)
        End If

        UpdateToolTip(idx)
    End Sub

    Private Sub InvalidatePart(part As HitPart, index As Integer)
        Select Case part
            Case HitPart.BackButton
                Invalidate(BackButtonBounds)
            Case HitPart.NextButton
                Invalidate(NextButtonBounds)
            Case HitPart.Item
                If index >= 0 AndAlso index < _items.Count Then Invalidate(GetItemBounds(index))
        End Select
    End Sub

    Private Sub UpdateToolTip(index As Integer)
        If Not _showItemToolTips Then Return
        If index = _toolTipIndex Then Return
        _toolTipIndex = index
        _toolTip.SetToolTip(Me, If(index >= 0, _items(index).ToolTipText, Nothing))
    End Sub

    Private Function MoreThanOneSelected() As Boolean
        Return _items.Where(Function(it) it.Selected).Take(2).Count() > 1
    End Function

    Private Sub ResetHotState()
        _hotIndex = -1
        _hotPart = HitPart.None
        _mouseDownIndex = -1
        _deferredSelectIndex = -1
        _toolTipIndex = -1
        _toolTip.SetToolTip(Me, Nothing)
    End Sub

#End Region

#Region "Keyboard"

    Protected Overrides Function IsInputKey(keyData As Keys) As Boolean
        Select Case keyData And Keys.KeyCode
            Case Keys.Left, Keys.Right, Keys.Home, Keys.End, Keys.PageUp, Keys.PageDown
                Return True
        End Select
        Return MyBase.IsInputKey(keyData)
    End Function

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        If e.Handled OrElse _items.Count = 0 Then Return

        Dim lastIndex = _items.Count - 1
        Dim cur = _currentIndex
        Dim target = -1

        Select Case e.KeyCode
            Case Keys.Left
                target = If(cur < 0, 0, Math.Max(0, cur - 1))
            Case Keys.Right
                target = If(cur < 0, 0, Math.Min(lastIndex, cur + 1))
            Case Keys.Home
                target = 0
            Case Keys.End
                target = lastIndex
            Case Keys.PageUp
                target = Math.Max(0, cur - VisibleCount)
            Case Keys.PageDown
                target = Math.Min(lastIndex, Math.Max(0, cur) + VisibleCount)
            Case Keys.Space
                If _multiSelect AndAlso e.Control AndAlso cur >= 0 Then
                    SelectCore(cur, SelectionAction.Toggle)
                    e.Handled = True
                End If
            Case Keys.A
                If _multiSelect AndAlso e.Control Then
                    SelectAll()
                    e.Handled = True
                End If
        End Select

        If target >= 0 Then
            SelectCore(target, If(_multiSelect AndAlso e.Shift, SelectionAction.Range, SelectionAction.Replace))
            EnsureVisible(target)
            e.Handled = True
        End If
    End Sub

#End Region

#Region "Image helpers"

    Private Shared Function TryLoadThumbnail(fileName As String, maxSize As Size) As Bitmap
        Try
            Return LoadThumbnail(fileName, maxSize)
        Catch ex As Exception When TypeOf ex Is IOException OrElse
                                   TypeOf ex Is InvalidDataException OrElse
                                   TypeOf ex Is UnauthorizedAccessException OrElse
                                   TypeOf ex Is OutOfMemoryException OrElse
                                   TypeOf ex Is ArgumentException OrElse
                                   TypeOf ex Is NotSupportedException
            Return Nothing
        End Try
    End Function

    ' Rotates camera JPEGs the right way up (EXIF orientation tag).
    Private Shared Sub ApplyExifOrientation(img As Image)
        Try
            If Array.IndexOf(img.PropertyIdList, ExifOrientationId) < 0 Then Return
            Dim prop = img.GetPropertyItem(ExifOrientationId)
            If prop Is Nothing OrElse prop.Value Is Nothing OrElse prop.Value.Length < 2 Then Return

            Dim flip As RotateFlipType
            Select Case BitConverter.ToUInt16(prop.Value, 0)
                Case 2US : flip = RotateFlipType.RotateNoneFlipX
                Case 3US : flip = RotateFlipType.Rotate180FlipNone
                Case 4US : flip = RotateFlipType.Rotate180FlipX
                Case 5US : flip = RotateFlipType.Rotate90FlipX
                Case 6US : flip = RotateFlipType.Rotate90FlipNone
                Case 7US : flip = RotateFlipType.Rotate270FlipX
                Case 8US : flip = RotateFlipType.Rotate270FlipNone
                Case Else : Return
            End Select
            img.RotateFlip(flip)
        Catch ex As Exception When TypeOf ex Is ArgumentException OrElse TypeOf ex Is ExternalException
            ' no usable EXIF data - leave the image as it is
        End Try
    End Sub

#End Region

End Class
