<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class restoreform
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
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
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        DreamForm1 = New ReaLTaiizor.Forms.DreamForm()
        ForeverLabel2 = New ReaLTaiizor.Controls.ForeverLabel()
        ForeverLabel1 = New ReaLTaiizor.Controls.ForeverLabel()
        DreamButton3 = New ReaLTaiizor.Controls.DreamButton()
        BtnGame = New ReaLTaiizor.Controls.DreamButton()
        btnBkp = New ReaLTaiizor.Controls.DreamButton()
        txtppsa = New ReaLTaiizor.Controls.SmallTextBox()
        txtbkp = New ReaLTaiizor.Controls.SmallTextBox()
        ForeverLabel3 = New ReaLTaiizor.Controls.ForeverLabel()
        DreamForm1.SuspendLayout()
        SuspendLayout()
        ' 
        ' DreamForm1
        ' 
        DreamForm1.ColorA = Color.FromArgb(CByte(40), CByte(218), CByte(255))
        DreamForm1.ColorB = Color.FromArgb(CByte(63), CByte(63), CByte(63))
        DreamForm1.ColorC = Color.FromArgb(CByte(41), CByte(41), CByte(41))
        DreamForm1.ColorD = Color.FromArgb(CByte(27), CByte(27), CByte(27))
        DreamForm1.ColorE = Color.FromArgb(CByte(0), CByte(0), CByte(0), CByte(0))
        DreamForm1.ColorF = Color.FromArgb(CByte(25), CByte(255), CByte(255), CByte(255))
        DreamForm1.Controls.Add(ForeverLabel3)
        DreamForm1.Controls.Add(ForeverLabel2)
        DreamForm1.Controls.Add(ForeverLabel1)
        DreamForm1.Controls.Add(DreamButton3)
        DreamForm1.Controls.Add(BtnGame)
        DreamForm1.Controls.Add(btnBkp)
        DreamForm1.Controls.Add(txtppsa)
        DreamForm1.Controls.Add(txtbkp)
        DreamForm1.Dock = DockStyle.Fill
        DreamForm1.Location = New Point(0, 0)
        DreamForm1.Name = "DreamForm1"
        DreamForm1.Size = New Size(389, 249)
        DreamForm1.TabIndex = 0
        DreamForm1.TabStop = False
        DreamForm1.Text = "DreamForm1"
        DreamForm1.TitleAlign = HorizontalAlignment.Center
        DreamForm1.TitleHeight = 25
        ' 
        ' ForeverLabel2
        ' 
        ForeverLabel2.AutoSize = True
        ForeverLabel2.BackColor = Color.Transparent
        ForeverLabel2.Font = New Font("Segoe UI", 8F)
        ForeverLabel2.ForeColor = Color.LightGray
        ForeverLabel2.Location = New Point(52, 115)
        ForeverLabel2.Name = "ForeverLabel2"
        ForeverLabel2.Size = New Size(75, 13)
        ForeverLabel2.TabIndex = 6
        ForeverLabel2.Text = "Game Folder:"
        ' 
        ' ForeverLabel1
        ' 
        ForeverLabel1.AutoSize = True
        ForeverLabel1.BackColor = Color.Transparent
        ForeverLabel1.Font = New Font("Segoe UI", 8F)
        ForeverLabel1.ForeColor = Color.LightGray
        ForeverLabel1.Location = New Point(56, 55)
        ForeverLabel1.Name = "ForeverLabel1"
        ForeverLabel1.Size = New Size(83, 13)
        ForeverLabel1.TabIndex = 5
        ForeverLabel1.Text = "Backup Folder:"
        ' 
        ' DreamButton3
        ' 
        DreamButton3.ColorA = Color.FromArgb(CByte(31), CByte(31), CByte(31))
        DreamButton3.ColorB = Color.FromArgb(CByte(41), CByte(41), CByte(41))
        DreamButton3.ColorC = Color.FromArgb(CByte(51), CByte(51), CByte(51))
        DreamButton3.ColorD = Color.FromArgb(CByte(0), CByte(0), CByte(0), CByte(0))
        DreamButton3.ColorE = Color.FromArgb(CByte(25), CByte(255), CByte(255), CByte(255))
        DreamButton3.ForeColor = Color.FromArgb(CByte(40), CByte(218), CByte(255))
        DreamButton3.Location = New Point(134, 179)
        DreamButton3.Name = "DreamButton3"
        DreamButton3.Size = New Size(120, 40)
        DreamButton3.TabIndex = 4
        DreamButton3.Text = "Restore"
        DreamButton3.UseVisualStyleBackColor = True
        ' 
        ' BtnGame
        ' 
        BtnGame.ColorA = Color.FromArgb(CByte(31), CByte(31), CByte(31))
        BtnGame.ColorB = Color.FromArgb(CByte(41), CByte(41), CByte(41))
        BtnGame.ColorC = Color.FromArgb(CByte(51), CByte(51), CByte(51))
        BtnGame.ColorD = Color.FromArgb(CByte(0), CByte(0), CByte(0), CByte(0))
        BtnGame.ColorE = Color.FromArgb(CByte(25), CByte(255), CByte(255), CByte(255))
        BtnGame.ForeColor = Color.FromArgb(CByte(40), CByte(218), CByte(255))
        BtnGame.Location = New Point(235, 133)
        BtnGame.Name = "BtnGame"
        BtnGame.Size = New Size(85, 28)
        BtnGame.TabIndex = 3
        BtnGame.Text = ". . ."
        BtnGame.UseVisualStyleBackColor = True
        ' 
        ' btnBkp
        ' 
        btnBkp.ColorA = Color.FromArgb(CByte(31), CByte(31), CByte(31))
        btnBkp.ColorB = Color.FromArgb(CByte(41), CByte(41), CByte(41))
        btnBkp.ColorC = Color.FromArgb(CByte(51), CByte(51), CByte(51))
        btnBkp.ColorD = Color.FromArgb(CByte(0), CByte(0), CByte(0), CByte(0))
        btnBkp.ColorE = Color.FromArgb(CByte(25), CByte(255), CByte(255), CByte(255))
        btnBkp.ForeColor = Color.FromArgb(CByte(40), CByte(218), CByte(255))
        btnBkp.Location = New Point(235, 77)
        btnBkp.Name = "btnBkp"
        btnBkp.Size = New Size(85, 28)
        btnBkp.TabIndex = 2
        btnBkp.Text = ". . ."
        btnBkp.UseVisualStyleBackColor = True
        ' 
        ' txtppsa
        ' 
        txtppsa.BackColor = Color.Transparent
        txtppsa.BorderColor = Color.FromArgb(CByte(180), CByte(180), CByte(180))
        txtppsa.CustomBGColor = Color.White
        txtppsa.Enabled = False
        txtppsa.Font = New Font("Tahoma", 11F)
        txtppsa.ForeColor = Color.DimGray
        txtppsa.Location = New Point(50, 133)
        txtppsa.MaxLength = 32767
        txtppsa.Multiline = False
        txtppsa.Name = "txtppsa"
        txtppsa.ReadOnly = False
        txtppsa.Size = New Size(167, 28)
        txtppsa.SmoothingType = Drawing2D.SmoothingMode.AntiAlias
        txtppsa.TabIndex = 1
        txtppsa.TextAlignment = HorizontalAlignment.Left
        txtppsa.UseSystemPasswordChar = False
        ' 
        ' txtbkp
        ' 
        txtbkp.BackColor = Color.Transparent
        txtbkp.BorderColor = Color.FromArgb(CByte(180), CByte(180), CByte(180))
        txtbkp.CustomBGColor = Color.White
        txtbkp.Enabled = False
        txtbkp.Font = New Font("Tahoma", 11F)
        txtbkp.ForeColor = Color.DimGray
        txtbkp.Location = New Point(50, 77)
        txtbkp.MaxLength = 32767
        txtbkp.Multiline = False
        txtbkp.Name = "txtbkp"
        txtbkp.ReadOnly = False
        txtbkp.Size = New Size(167, 28)
        txtbkp.SmoothingType = Drawing2D.SmoothingMode.AntiAlias
        txtbkp.TabIndex = 0
        txtbkp.TextAlignment = HorizontalAlignment.Left
        txtbkp.UseSystemPasswordChar = False
        ' 
        ' ForeverLabel3
        ' 
        ForeverLabel3.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        ForeverLabel3.AutoSize = True
        ForeverLabel3.BackColor = Color.Transparent
        ForeverLabel3.Font = New Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ForeverLabel3.ForeColor = Color.LightGray
        ForeverLabel3.Location = New Point(364, 5)
        ForeverLabel3.Name = "ForeverLabel3"
        ForeverLabel3.Size = New Size(17, 17)
        ForeverLabel3.TabIndex = 7
        ForeverLabel3.Text = "X"
        ' 
        ' restoreform
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(389, 249)
        Controls.Add(DreamForm1)
        FormBorderStyle = FormBorderStyle.None
        Name = "restoreform"
        StartPosition = FormStartPosition.CenterScreen
        Text = "restoreform"
        DreamForm1.ResumeLayout(False)
        DreamForm1.PerformLayout()
        ResumeLayout(False)
    End Sub

    Friend WithEvents DreamForm1 As ReaLTaiizor.Forms.DreamForm
    Friend WithEvents txtbkp As ReaLTaiizor.Controls.SmallTextBox
    Friend WithEvents BtnGame As ReaLTaiizor.Controls.DreamButton
    Friend WithEvents btnBkp As ReaLTaiizor.Controls.DreamButton
    Friend WithEvents txtppsa As ReaLTaiizor.Controls.SmallTextBox
    Friend WithEvents DreamButton3 As ReaLTaiizor.Controls.DreamButton
    Friend WithEvents ForeverLabel2 As ReaLTaiizor.Controls.ForeverLabel
    Friend WithEvents ForeverLabel1 As ReaLTaiizor.Controls.ForeverLabel
    Friend WithEvents ForeverLabel3 As ReaLTaiizor.Controls.ForeverLabel
End Class
