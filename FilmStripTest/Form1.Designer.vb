<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.FilmStrip1 = New FilmStripControl.FilmStrip()
        Me.flpToolbar = New System.Windows.Forms.FlowLayoutPanel()
        Me.btnAddFiles = New System.Windows.Forms.Button()
        Me.btnAddFolder = New System.Windows.Forms.Button()
        Me.btnAddGenerated = New System.Windows.Forms.Button()
        Me.btnRemove = New System.Windows.Forms.Button()
        Me.btnClear = New System.Windows.Forms.Button()
        Me.btnPrev = New System.Windows.Forms.Button()
        Me.btnNext = New System.Windows.Forms.Button()
        Me.btnSaveThumb = New System.Windows.Forms.Button()
        Me.lblThumbSize = New System.Windows.Forms.Label()
        Me.cboThumbSize = New System.Windows.Forms.ComboBox()
        Me.chkMultiSelect = New System.Windows.Forms.CheckBox()
        Me.chkAutoDrag = New System.Windows.Forms.CheckBox()
        Me.chkShowTitles = New System.Windows.Forms.CheckBox()
        Me.chkEnabled = New System.Windows.Forms.CheckBox()
        Me.btnClearLog = New System.Windows.Forms.Button()
        Me.SplitContainer1 = New System.Windows.Forms.SplitContainer()
        Me.picPreview = New System.Windows.Forms.PictureBox()
        Me.lstLog = New System.Windows.Forms.ListBox()
        Me.StatusStrip1 = New System.Windows.Forms.StatusStrip()
        Me.lblStatus = New System.Windows.Forms.ToolStripStatusLabel()
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog()
        Me.FolderBrowserDialog1 = New System.Windows.Forms.FolderBrowserDialog()
        Me.flpToolbar.SuspendLayout()
        CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SplitContainer1.Panel1.SuspendLayout()
        Me.SplitContainer1.Panel2.SuspendLayout()
        Me.SplitContainer1.SuspendLayout()
        CType(Me.picPreview, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.StatusStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'FilmStrip1
        '
        Me.FilmStrip1.AllowDrop = True
        Me.FilmStrip1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.FilmStrip1.Dock = System.Windows.Forms.DockStyle.Top
        Me.FilmStrip1.Location = New System.Drawing.Point(0, 0)
        Me.FilmStrip1.Name = "FilmStrip1"
        Me.FilmStrip1.Size = New System.Drawing.Size(984, 119)
        Me.FilmStrip1.TabIndex = 0
        '
        'flpToolbar
        '
        Me.flpToolbar.AutoSize = True
        Me.flpToolbar.Controls.Add(Me.btnAddFiles)
        Me.flpToolbar.Controls.Add(Me.btnAddFolder)
        Me.flpToolbar.Controls.Add(Me.btnAddGenerated)
        Me.flpToolbar.Controls.Add(Me.btnRemove)
        Me.flpToolbar.Controls.Add(Me.btnClear)
        Me.flpToolbar.Controls.Add(Me.btnPrev)
        Me.flpToolbar.Controls.Add(Me.btnNext)
        Me.flpToolbar.Controls.Add(Me.btnSaveThumb)
        Me.flpToolbar.Controls.Add(Me.lblThumbSize)
        Me.flpToolbar.Controls.Add(Me.cboThumbSize)
        Me.flpToolbar.Controls.Add(Me.chkMultiSelect)
        Me.flpToolbar.Controls.Add(Me.chkAutoDrag)
        Me.flpToolbar.Controls.Add(Me.chkShowTitles)
        Me.flpToolbar.Controls.Add(Me.chkEnabled)
        Me.flpToolbar.Controls.Add(Me.btnClearLog)
        Me.flpToolbar.Dock = System.Windows.Forms.DockStyle.Top
        Me.flpToolbar.Location = New System.Drawing.Point(0, 119)
        Me.flpToolbar.Name = "flpToolbar"
        Me.flpToolbar.Padding = New System.Windows.Forms.Padding(3)
        Me.flpToolbar.Size = New System.Drawing.Size(984, 37)
        Me.flpToolbar.TabIndex = 1
        '
        'btnAddFiles
        '
        Me.btnAddFiles.AutoSize = True
        Me.btnAddFiles.Name = "btnAddFiles"
        Me.btnAddFiles.TabIndex = 0
        Me.btnAddFiles.Text = "Add files..."
        Me.btnAddFiles.UseVisualStyleBackColor = True
        '
        'btnAddFolder
        '
        Me.btnAddFolder.AutoSize = True
        Me.btnAddFolder.Name = "btnAddFolder"
        Me.btnAddFolder.TabIndex = 1
        Me.btnAddFolder.Text = "Add folder..."
        Me.btnAddFolder.UseVisualStyleBackColor = True
        '
        'btnAddGenerated
        '
        Me.btnAddGenerated.AutoSize = True
        Me.btnAddGenerated.Name = "btnAddGenerated"
        Me.btnAddGenerated.TabIndex = 2
        Me.btnAddGenerated.Text = "Add 10 generated"
        Me.btnAddGenerated.UseVisualStyleBackColor = True
        '
        'btnRemove
        '
        Me.btnRemove.AutoSize = True
        Me.btnRemove.Name = "btnRemove"
        Me.btnRemove.TabIndex = 3
        Me.btnRemove.Text = "Remove selected"
        Me.btnRemove.UseVisualStyleBackColor = True
        '
        'btnClear
        '
        Me.btnClear.AutoSize = True
        Me.btnClear.Name = "btnClear"
        Me.btnClear.TabIndex = 4
        Me.btnClear.Text = "Clear"
        Me.btnClear.UseVisualStyleBackColor = True
        '
        'btnPrev
        '
        Me.btnPrev.AutoSize = True
        Me.btnPrev.Name = "btnPrev"
        Me.btnPrev.TabIndex = 5
        Me.btnPrev.Text = "< Prev"
        Me.btnPrev.UseVisualStyleBackColor = True
        '
        'btnNext
        '
        Me.btnNext.AutoSize = True
        Me.btnNext.Name = "btnNext"
        Me.btnNext.TabIndex = 6
        Me.btnNext.Text = "Next >"
        Me.btnNext.UseVisualStyleBackColor = True
        '
        'btnSaveThumb
        '
        Me.btnSaveThumb.AutoSize = True
        Me.btnSaveThumb.Name = "btnSaveThumb"
        Me.btnSaveThumb.TabIndex = 7
        Me.btnSaveThumb.Text = "Save thumbnail"
        Me.btnSaveThumb.UseVisualStyleBackColor = True
        '
        'lblThumbSize
        '
        Me.lblThumbSize.Anchor = System.Windows.Forms.AnchorStyles.Left
        Me.lblThumbSize.AutoSize = True
        Me.lblThumbSize.Margin = New System.Windows.Forms.Padding(12, 0, 3, 0)
        Me.lblThumbSize.Name = "lblThumbSize"
        Me.lblThumbSize.TabIndex = 8
        Me.lblThumbSize.Text = "Thumb size:"
        '
        'cboThumbSize
        '
        Me.cboThumbSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboThumbSize.FormattingEnabled = True
        Me.cboThumbSize.Items.AddRange(New Object() {"Small (60 x 54)", "Default (81 x 73)", "Large (120 x 108)"})
        Me.cboThumbSize.Name = "cboThumbSize"
        Me.cboThumbSize.Size = New System.Drawing.Size(130, 23)
        Me.cboThumbSize.TabIndex = 9
        '
        'chkMultiSelect
        '
        Me.chkMultiSelect.Anchor = System.Windows.Forms.AnchorStyles.Left
        Me.chkMultiSelect.AutoSize = True
        Me.chkMultiSelect.Margin = New System.Windows.Forms.Padding(12, 3, 3, 3)
        Me.chkMultiSelect.Name = "chkMultiSelect"
        Me.chkMultiSelect.TabIndex = 10
        Me.chkMultiSelect.Text = "MultiSelect"
        Me.chkMultiSelect.UseVisualStyleBackColor = True
        '
        'chkAutoDrag
        '
        Me.chkAutoDrag.Anchor = System.Windows.Forms.AnchorStyles.Left
        Me.chkAutoDrag.AutoSize = True
        Me.chkAutoDrag.Name = "chkAutoDrag"
        Me.chkAutoDrag.TabIndex = 11
        Me.chkAutoDrag.Text = "Automatic drag"
        Me.chkAutoDrag.UseVisualStyleBackColor = True
        '
        'chkShowTitles
        '
        Me.chkShowTitles.Anchor = System.Windows.Forms.AnchorStyles.Left
        Me.chkShowTitles.AutoSize = True
        Me.chkShowTitles.Checked = True
        Me.chkShowTitles.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkShowTitles.Name = "chkShowTitles"
        Me.chkShowTitles.TabIndex = 12
        Me.chkShowTitles.Text = "Titles"
        Me.chkShowTitles.UseVisualStyleBackColor = True
        '
        'chkEnabled
        '
        Me.chkEnabled.Anchor = System.Windows.Forms.AnchorStyles.Left
        Me.chkEnabled.AutoSize = True
        Me.chkEnabled.Checked = True
        Me.chkEnabled.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkEnabled.Name = "chkEnabled"
        Me.chkEnabled.TabIndex = 13
        Me.chkEnabled.Text = "Enabled"
        Me.chkEnabled.UseVisualStyleBackColor = True
        '
        'btnClearLog
        '
        Me.btnClearLog.AutoSize = True
        Me.btnClearLog.Margin = New System.Windows.Forms.Padding(12, 3, 3, 3)
        Me.btnClearLog.Name = "btnClearLog"
        Me.btnClearLog.TabIndex = 14
        Me.btnClearLog.Text = "Clear log"
        Me.btnClearLog.UseVisualStyleBackColor = True
        '
        'SplitContainer1
        '
        Me.SplitContainer1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.SplitContainer1.Location = New System.Drawing.Point(0, 156)
        Me.SplitContainer1.Name = "SplitContainer1"
        '
        'SplitContainer1.Panel1
        '
        Me.SplitContainer1.Panel1.Controls.Add(Me.picPreview)
        '
        'SplitContainer1.Panel2
        '
        Me.SplitContainer1.Panel2.Controls.Add(Me.lstLog)
        Me.SplitContainer1.Size = New System.Drawing.Size(984, 383)
        Me.SplitContainer1.SplitterDistance = 560
        Me.SplitContainer1.TabIndex = 2
        '
        'picPreview
        '
        Me.picPreview.BackColor = System.Drawing.Color.DimGray
        Me.picPreview.Dock = System.Windows.Forms.DockStyle.Fill
        Me.picPreview.Location = New System.Drawing.Point(0, 0)
        Me.picPreview.Name = "picPreview"
        Me.picPreview.Size = New System.Drawing.Size(560, 383)
        Me.picPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.picPreview.TabIndex = 0
        Me.picPreview.TabStop = False
        '
        'lstLog
        '
        Me.lstLog.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lstLog.Font = New System.Drawing.Font("Consolas", 9.0!)
        Me.lstLog.HorizontalScrollbar = True
        Me.lstLog.IntegralHeight = False
        Me.lstLog.Location = New System.Drawing.Point(0, 0)
        Me.lstLog.Name = "lstLog"
        Me.lstLog.Size = New System.Drawing.Size(420, 383)
        Me.lstLog.TabIndex = 0
        '
        'StatusStrip1
        '
        Me.StatusStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.lblStatus})
        Me.StatusStrip1.Location = New System.Drawing.Point(0, 539)
        Me.StatusStrip1.Name = "StatusStrip1"
        Me.StatusStrip1.Size = New System.Drawing.Size(984, 22)
        Me.StatusStrip1.TabIndex = 3
        '
        'lblStatus
        '
        Me.lblStatus.Name = "lblStatus"
        Me.lblStatus.Spring = True
        Me.lblStatus.Text = "Ready"
        Me.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.Filter = "Images|*.bmp;*.gif;*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.ico;*.wmf;*.emf|All files|*.*"
        Me.OpenFileDialog1.Multiselect = True
        Me.OpenFileDialog1.Title = "Add images"
        '
        'FolderBrowserDialog1
        '
        Me.FolderBrowserDialog1.Description = "Pick a folder with images"
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(984, 561)
        Me.Controls.Add(Me.SplitContainer1)
        Me.Controls.Add(Me.flpToolbar)
        Me.Controls.Add(Me.FilmStrip1)
        Me.Controls.Add(Me.StatusStrip1)
        Me.MinimumSize = New System.Drawing.Size(640, 400)
        Me.Name = "Form1"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "FilmStrip test"
        Me.flpToolbar.ResumeLayout(False)
        Me.flpToolbar.PerformLayout()
        Me.SplitContainer1.Panel1.ResumeLayout(False)
        Me.SplitContainer1.Panel2.ResumeLayout(False)
        CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.SplitContainer1.ResumeLayout(False)
        CType(Me.picPreview, System.ComponentModel.ISupportInitialize).EndInit()
        Me.StatusStrip1.ResumeLayout(False)
        Me.StatusStrip1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents FilmStrip1 As FilmStripControl.FilmStrip
    Friend WithEvents flpToolbar As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents btnAddFiles As System.Windows.Forms.Button
    Friend WithEvents btnAddFolder As System.Windows.Forms.Button
    Friend WithEvents btnAddGenerated As System.Windows.Forms.Button
    Friend WithEvents btnRemove As System.Windows.Forms.Button
    Friend WithEvents btnClear As System.Windows.Forms.Button
    Friend WithEvents btnPrev As System.Windows.Forms.Button
    Friend WithEvents btnNext As System.Windows.Forms.Button
    Friend WithEvents btnSaveThumb As System.Windows.Forms.Button
    Friend WithEvents lblThumbSize As System.Windows.Forms.Label
    Friend WithEvents cboThumbSize As System.Windows.Forms.ComboBox
    Friend WithEvents chkMultiSelect As System.Windows.Forms.CheckBox
    Friend WithEvents chkAutoDrag As System.Windows.Forms.CheckBox
    Friend WithEvents chkShowTitles As System.Windows.Forms.CheckBox
    Friend WithEvents chkEnabled As System.Windows.Forms.CheckBox
    Friend WithEvents btnClearLog As System.Windows.Forms.Button
    Friend WithEvents SplitContainer1 As System.Windows.Forms.SplitContainer
    Friend WithEvents picPreview As System.Windows.Forms.PictureBox
    Friend WithEvents lstLog As System.Windows.Forms.ListBox
    Friend WithEvents StatusStrip1 As System.Windows.Forms.StatusStrip
    Friend WithEvents lblStatus As System.Windows.Forms.ToolStripStatusLabel
    Friend WithEvents OpenFileDialog1 As System.Windows.Forms.OpenFileDialog
    Friend WithEvents FolderBrowserDialog1 As System.Windows.Forms.FolderBrowserDialog

End Class
