Option Strict On
Option Explicit On
Option Infer On

Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Windows.Forms
Imports FilmStripControl

''' <summary>
''' Test bench for the FilmStrip control: exercises every public method, property and event
''' and logs the events in the list on the right.
''' </summary>
Public Class Form1

    Private Shared ReadOnly ImageExtensions As String() =
        {".bmp", ".gif", ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".ico", ".wmf", ".emf"}

    Private Shared ReadOnly ThumbSizes As Size() =
        {New Size(60, 54), New Size(81, 73), New Size(120, 108)}

    Private _folderCts As CancellationTokenSource
    Private _generatedCount As Integer

    <DllImport("gdi32.dll")>
    Private Shared Function DeleteObject(hObject As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

#Region "Form"

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        cboThumbSize.SelectedIndex = 1
        chkMultiSelect.Checked = FilmStrip1.MultiSelect
        chkAutoDrag.Checked = (FilmStrip1.ItemDragMode = FilmStripDragMode.Automatic)
        Log("Ready. Add images with the buttons, or drag image files from Explorer onto the strip.")
        UpdateStatus()
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        _folderCts?.Cancel()
        Dim old = picPreview.Image
        picPreview.Image = Nothing
        old?.Dispose()
    End Sub

#End Region

#Region "Toolbar - adding / removing"

    Private Sub btnAddFiles_Click(sender As Object, e As EventArgs) Handles btnAddFiles.Click
        If OpenFileDialog1.ShowDialog(Me) <> DialogResult.OK Then Return

        For Each fileName In OpenFileDialog1.FileNames
            Try
                Dim item = FilmStrip1.AddItem(fileName)
                Log($"AddItem: #{item.Index} {fileName}")
            Catch ex As Exception
                Log($"AddItem failed: {Path.GetFileName(fileName)} - {ex.Message}")
            End Try
        Next
        UpdateStatus()
    End Sub

    Private Async Sub btnAddFolder_Click(sender As Object, e As EventArgs) Handles btnAddFolder.Click
        ' Second click while loading = cancel
        If _folderCts IsNot Nothing Then
            _folderCts.Cancel()
            Return
        End If

        If FolderBrowserDialog1.ShowDialog(Me) <> DialogResult.OK Then Return

        _folderCts = New CancellationTokenSource()
        btnAddFolder.Text = "Cancel loading"
        Dim sw = Stopwatch.StartNew()
        Try
            Dim files = Directory.EnumerateFiles(FolderBrowserDialog1.SelectedPath).
                Where(Function(f) IsImageFile(f)).
                OrderBy(Function(f) f, StringComparer.OrdinalIgnoreCase).
                ToList()
            Log($"AddItemsAsync: {files.Count} image files in {FolderBrowserDialog1.SelectedPath}")

            Dim added = Await FilmStrip1.AddItemsAsync(files, _folderCts.Token)
            Log($"AddItemsAsync: {added} of {files.Count} added in {sw.ElapsedMilliseconds} ms")
        Catch ex As OperationCanceledException
            Log($"AddItemsAsync: cancelled after {sw.ElapsedMilliseconds} ms, strip now has {FilmStrip1.Count} items")
        Catch ex As Exception
            Log($"AddItemsAsync failed: {ex.Message}")
        Finally
            _folderCts.Dispose()
            _folderCts = Nothing
            btnAddFolder.Text = "Add folder..."
            UpdateStatus()
        End Try
    End Sub

    ' Tests AddItem(Image) and AddItemFromHandle without needing image files on disk.
    Private Sub btnAddGenerated_Click(sender As Object, e As EventArgs) Handles btnAddGenerated.Click
        For n = 1 To 10
            _generatedCount += 1
            Using bmp = MakeTestImage(_generatedCount)
                If _generatedCount Mod 3 = 0 Then
                    Dim hBmp = bmp.GetHbitmap()
                    Try
                        FilmStrip1.AddItemFromHandle(hBmp, $"Handle #{_generatedCount} ({bmp.Width}x{bmp.Height})")
                    Finally
                        DeleteObject(hBmp)
                    End Try
                Else
                    FilmStrip1.AddItem(bmp, $"Image #{_generatedCount} ({bmp.Width}x{bmp.Height})")
                End If
            End Using
        Next
        Log("Added 10 generated images (every 3rd one via AddItemFromHandle)")
        UpdateStatus()
    End Sub

    Private Sub btnRemove_Click(sender As Object, e As EventArgs) Handles btnRemove.Click
        Dim selected = FilmStrip1.SelectedIndices
        If selected.Length = 0 Then
            Log("RemoveItem: nothing selected")
            Return
        End If

        ' Remove from the end so the remaining indices stay valid
        For n = selected.Length - 1 To 0 Step -1
            FilmStrip1.RemoveItem(selected(n))
        Next
        Log($"RemoveItem: removed {selected.Length} item(s)")
        UpdateStatus()
    End Sub

    Private Sub btnClear_Click(sender As Object, e As EventArgs) Handles btnClear.Click
        FilmStrip1.Clear()
        Log("Clear")
        UpdateStatus()
    End Sub

#End Region

#Region "Toolbar - navigation / settings"

    Private Sub btnPrev_Click(sender As Object, e As EventArgs) Handles btnPrev.Click
        Log($"SelectPrevious returned {FilmStrip1.SelectPrevious()}")
    End Sub

    Private Sub btnNext_Click(sender As Object, e As EventArgs) Handles btnNext.Click
        Log($"SelectNext returned {FilmStrip1.SelectNext()}")
    End Sub

    Private Sub btnSaveThumb_Click(sender As Object, e As EventArgs) Handles btnSaveThumb.Click
        Dim item = FilmStrip1.SelectedItem
        If item Is Nothing OrElse item.Image Is Nothing Then
            Log("Save thumbnail: nothing selected")
            Return
        End If

        Try
            Dim folder = Path.Combine(Application.StartupPath, "Thumbnails")
            Directory.CreateDirectory(folder)

            Dim baseName = If(String.IsNullOrEmpty(item.Path), item.Text, Path.GetFileNameWithoutExtension(item.Path))
            For Each c In Path.GetInvalidFileNameChars()
                baseName = baseName.Replace(c, "_"c)
            Next
            If String.IsNullOrWhiteSpace(baseName) Then baseName = "thumbnail"

            Dim target = Path.Combine(folder, baseName & ".png")
            item.Image.Save(target, ImageFormat.Png)
            Log($"Saved {item.Image.Width}x{item.Image.Height} thumbnail to {target}")
        Catch ex As Exception
            Log($"Save thumbnail failed: {ex.Message}")
        End Try
    End Sub

    Private Sub cboThumbSize_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboThumbSize.SelectedIndexChanged
        If cboThumbSize.SelectedIndex < 0 Then Return
        FilmStrip1.ThumbnailSize = ThumbSizes(cboThumbSize.SelectedIndex)
        Log($"ThumbnailSize = {FilmStrip1.ThumbnailSize.Width}x{FilmStrip1.ThumbnailSize.Height} (applies to items added from now on)")
        UpdateStatus()
    End Sub

    Private Sub chkMultiSelect_CheckedChanged(sender As Object, e As EventArgs) Handles chkMultiSelect.CheckedChanged
        FilmStrip1.MultiSelect = chkMultiSelect.Checked
    End Sub

    Private Sub chkAutoDrag_CheckedChanged(sender As Object, e As EventArgs) Handles chkAutoDrag.CheckedChanged
        FilmStrip1.ItemDragMode = If(chkAutoDrag.Checked, FilmStripDragMode.Automatic, FilmStripDragMode.Manual)
    End Sub

    Private Sub chkShowTitles_CheckedChanged(sender As Object, e As EventArgs) Handles chkShowTitles.CheckedChanged
        FilmStrip1.ShowTitles = chkShowTitles.Checked
    End Sub

    Private Sub chkEnabled_CheckedChanged(sender As Object, e As EventArgs) Handles chkEnabled.CheckedChanged
        FilmStrip1.Enabled = chkEnabled.Checked
    End Sub

    Private Sub btnClearLog_Click(sender As Object, e As EventArgs) Handles btnClearLog.Click
        lstLog.Items.Clear()
    End Sub

#End Region

#Region "FilmStrip events"

    Private Sub FilmStrip1_ItemClick(sender As Object, e As FilmStripItemEventArgs) Handles FilmStrip1.ItemClick
        Log($"ItemClick: #{e.Index} '{e.Item.Text}' button={e.Button}")
    End Sub

    Private Sub FilmStrip1_ItemDoubleClick(sender As Object, e As FilmStripItemEventArgs) Handles FilmStrip1.ItemDoubleClick
        Log($"ItemDoubleClick: #{e.Index} '{e.Item.Text}'")
        If String.IsNullOrEmpty(e.Item.Path) OrElse Not File.Exists(e.Item.Path) Then Return
        Try
            ' Open the image in the default viewer
            Process.Start(New ProcessStartInfo(e.Item.Path) With {.UseShellExecute = True})
        Catch ex As Exception
            Log($"Open failed: {ex.Message}")
        End Try
    End Sub

    Private Sub FilmStrip1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles FilmStrip1.SelectedIndexChanged
        Log($"SelectedIndexChanged: SelectedIndex={FilmStrip1.SelectedIndex}, selected=[{String.Join(",", FilmStrip1.SelectedIndices)}]")
        ShowPreview(FilmStrip1.SelectedItem)
        UpdateStatus()
    End Sub

    Private Sub FilmStrip1_Scroll(sender As Object, e As ScrollEventArgs) Handles FilmStrip1.Scroll
        Log($"Scroll: {e.OldValue} -> {e.NewValue} ({e.Type})")
        UpdateStatus()
    End Sub

    Private Sub FilmStrip1_ItemDrag(sender As Object, e As ItemDragEventArgs) Handles FilmStrip1.ItemDrag
        Dim item = DirectCast(e.Item, FilmStripItem)
        Log($"ItemDrag: '{item.Text}' button={e.Button} mode={FilmStrip1.ItemDragMode}")

        ' In Manual mode the host starts the drag itself; in Automatic mode the control does it.
        If FilmStrip1.ItemDragMode = FilmStripDragMode.Manual AndAlso e.Button = MouseButtons.Left Then
            Dim result = FilmStrip1.StartDrag()
            Log($"StartDrag returned {result}")
        End If
    End Sub

    ' Accept image files dragged in from Explorer (AllowDrop = True in the designer)
    Private Sub FilmStrip1_DragEnter(sender As Object, e As DragEventArgs) Handles FilmStrip1.DragEnter
        e.Effect = If(e.Data IsNot Nothing AndAlso e.Data.GetDataPresent(DataFormats.FileDrop),
                      DragDropEffects.Copy, DragDropEffects.None)
    End Sub

    Private Async Sub FilmStrip1_DragDrop(sender As Object, e As DragEventArgs) Handles FilmStrip1.DragDrop
        Dim dropped = TryCast(e.Data?.GetData(DataFormats.FileDrop), String())
        If dropped Is Nothing Then Return

        ' Skip files that are already in the strip (this also ignores dropping the strip's own items back on it)
        Dim existing As New HashSet(Of String)(
            FilmStrip1.Items.Where(Function(it) Not String.IsNullOrEmpty(it.Path)).Select(Function(it) it.Path),
            StringComparer.OrdinalIgnoreCase)
        Dim toAdd = dropped.Where(Function(f) File.Exists(f) AndAlso IsImageFile(f) AndAlso Not existing.Contains(f)).ToList()

        If toAdd.Count = 0 Then
            Log("DragDrop: no new image files")
            Return
        End If

        Try
            Dim added = Await FilmStrip1.AddItemsAsync(toAdd)
            Log($"DragDrop: added {added} of {toAdd.Count} file(s)")
        Catch ex As Exception
            Log($"DragDrop failed: {ex.Message}")
        End Try
        UpdateStatus()
    End Sub

#End Region

#Region "Helpers"

    Private Shared Function IsImageFile(fileName As String) As Boolean
        Return ImageExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant())
    End Function

    ' Shows the full-size image (or the thumbnail for generated items) in the preview box.
    ' Always a copy, because the control disposes an item's thumbnail when the item is removed.
    Private Sub ShowPreview(item As FilmStripItem)
        Dim old = picPreview.Image
        picPreview.Image = Nothing
        old?.Dispose()
        If item Is Nothing Then Return

        Try
            If Not String.IsNullOrEmpty(item.Path) AndAlso File.Exists(item.Path) Then
                Using ms As New MemoryStream(File.ReadAllBytes(item.Path)), img = Image.FromStream(ms)
                    picPreview.Image = New Bitmap(img)
                End Using
            ElseIf item.Image IsNot Nothing Then
                picPreview.Image = New Bitmap(item.Image)
            End If
        Catch ex As Exception
            Log($"Preview failed: {ex.Message}")
        End Try
    End Sub

    Private Sub UpdateStatus()
        lblStatus.Text = $"{FilmStrip1.Count} items  |  SelectedIndex {FilmStrip1.SelectedIndex}  |  " &
                         $"{FilmStrip1.SelectedIndices.Length} selected  |  first visible {FilmStrip1.FirstVisibleIndex}  |  " &
                         $"{FilmStrip1.VisibleCount} fit  |  back {If(FilmStrip1.CanSelectPrevious, "on", "off")}, next {If(FilmStrip1.CanSelectNext, "on", "off")}"
    End Sub

    Private Sub Log(message As String)
        lstLog.Items.Add($"{DateTime.Now:HH:mm:ss.fff}  {message}")
        If lstLog.Items.Count > 500 Then lstLog.Items.RemoveAt(0)
        lstLog.TopIndex = lstLog.Items.Count - 1
    End Sub

    ' Gradient with a big number, random size/aspect ratio so the thumbnail fitting gets tested.
    Private Shared Function MakeTestImage(number As Integer) As Bitmap
        Dim rnd As New Random(number)
        Dim w = rnd.Next(120, 800)
        Dim h = rnd.Next(120, 800)
        Dim bmp As New Bitmap(w, h)
        Using g = Graphics.FromImage(bmp),
              brush As New LinearGradientBrush(New Rectangle(0, 0, w, h), ColorFromHue(number * 37), ColorFromHue(number * 37 + 120), 45.0F),
              fnt As New Font("Segoe UI", Math.Min(w, h) / 2.5F, FontStyle.Bold, GraphicsUnit.Pixel),
              sf As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
            g.FillRectangle(brush, 0, 0, w, h)
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias
            g.DrawString(number.ToString(), fnt, Brushes.White, New RectangleF(0, 0, w, h), sf)
        End Using
        Return bmp
    End Function

    Private Shared Function ColorFromHue(hue As Integer) As Color
        Const saturation As Double = 0.7
        Const value As Double = 230.0

        Dim wrapped = ((hue Mod 360) + 360) Mod 360
        Dim hh = wrapped / 60.0
        Dim sector = CInt(Math.Floor(hh)) Mod 6
        Dim f = hh - Math.Floor(hh)
        Dim p = value * (1 - saturation)
        Dim q = value * (1 - saturation * f)
        Dim t = value * (1 - saturation * (1 - f))

        Dim r, g, b As Double
        Select Case sector
            Case 0 : r = value : g = t : b = p
            Case 1 : r = q : g = value : b = p
            Case 2 : r = p : g = value : b = t
            Case 3 : r = p : g = q : b = value
            Case 4 : r = t : g = p : b = value
            Case Else : r = value : g = p : b = q
        End Select
        Return Color.FromArgb(CInt(r), CInt(g), CInt(b))
    End Function

#End Region

End Class
