Option Strict On
Option Explicit On
Option Infer On

Imports System.ComponentModel
Imports System.Drawing

''' <summary>
''' One frame (thumbnail + title) in a <see cref="FilmStrip"/>.
''' Items are created by the FilmStrip's AddItem / InsertItem methods.
''' The thumbnail image is owned by the item and is disposed when the item
''' is removed from the strip (RemoveItem / Clear) or the strip is disposed.
''' </summary>
Public NotInheritable Class FilmStripItem
    Implements IDisposable

    Private _image As Image
    Private _text As String
    Private _toolTipText As String
    Private _selected As Boolean
    Private _owner As FilmStrip

    Friend Sub New(path As String, text As String, thumbnail As Image)
        Me.Path = path
        _text = text
        _image = thumbnail
    End Sub

    ''' <summary>Full path of the source file, or Nothing when the item was added from an Image or bitmap handle.</summary>
    Public ReadOnly Property Path As String

    ''' <summary>Title shown under the thumbnail. Defaults to the file name (without folder) of <see cref="Path"/>.</summary>
    Public Property Text As String
        Get
            If _text IsNot Nothing Then Return _text
            If Not String.IsNullOrEmpty(Path) Then Return System.IO.Path.GetFileName(Path)
            Return String.Empty
        End Get
        Set(value As String)
            If String.Equals(_text, value, StringComparison.Ordinal) Then Return
            _text = value
            _owner?.InvalidateItem(Me)
        End Set
    End Property

    ''' <summary>Tooltip shown when hovering the item. Defaults to the full path, or the title when there is no path.</summary>
    Public Property ToolTipText As String
        Get
            If _toolTipText IsNot Nothing Then Return _toolTipText
            If Not String.IsNullOrEmpty(Path) Then Return Path
            Return Text
        End Get
        Set(value As String)
            _toolTipText = value
        End Set
    End Property

    ''' <summary>The thumbnail image. Owned by the item; do not dispose it yourself.</summary>
    Public ReadOnly Property Image As Image
        Get
            Return _image
        End Get
    End Property

    ''' <summary>Any user data you want to attach to the item.</summary>
    Public Property Tag As Object

    ''' <summary>True when the item is selected. Use FilmStrip.SetSelected / SelectedIndex to change it.</summary>
    Public Property Selected As Boolean
        Get
            Return _selected
        End Get
        Friend Set(value As Boolean)
            _selected = value
        End Set
    End Property

    ''' <summary>Position of the item in its FilmStrip, or -1 when it is not in a strip.</summary>
    Public ReadOnly Property Index As Integer
        Get
            If _owner Is Nothing Then Return -1
            Return _owner.IndexOf(Me)
        End Get
    End Property

    ''' <summary>The FilmStrip that contains this item, or Nothing.</summary>
    Public ReadOnly Property Owner As FilmStrip
        Get
            Return _owner
        End Get
    End Property

    Friend Sub SetOwner(owner As FilmStrip)
        _owner = owner
    End Sub

    Public Overrides Function ToString() As String
        Return Text
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        If _image IsNot Nothing Then
            _image.Dispose()
            _image = Nothing
        End If
    End Sub

End Class
