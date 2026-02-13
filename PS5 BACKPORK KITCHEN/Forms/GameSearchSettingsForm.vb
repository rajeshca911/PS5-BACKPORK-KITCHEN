Imports PS5_BACKPORK_KITCHEN.Services.GameSearch
Imports System.Windows.Forms

''' <summary>
''' Settings form for game search providers
''' Configure credentials and test connections
''' </summary>
Public Class GameSearchSettingsForm
    Inherits Form

    Private _searchManager As GameSearchManager
    Private _selectedProvider As IGameSearchProvider

    ' UI Controls
    Private lstProviders As ListBox
    Private pnlProviderDetails As Panel
    Private lblProviderName As Label
    Private lblProviderStatus As Label
    Private txtUsername As TextBox
    Private txtPassword As TextBox
    Private btnLogin As Button
    Private btnLogout As Button
    Private btnTestConnection As Button
    Private btnSave As Button
    Private btnClose As Button

    Public Sub New(searchManager As GameSearchManager)
        _searchManager = searchManager
        InitializeComponent()
        LoadProviders()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Search Provider Settings"
        Me.Size = New Size(500, 400)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        ' Provider list
        Dim lblProviders As New Label With {
            .Text = "Providers:",
            .Location = New Point(10, 10),
            .AutoSize = True
        }
        Me.Controls.Add(lblProviders)

        lstProviders = New ListBox With {
            .Location = New Point(10, 30),
            .Size = New Size(150, 280)
        }
        AddHandler lstProviders.SelectedIndexChanged, AddressOf lstProviders_SelectedIndexChanged
        Me.Controls.Add(lstProviders)

        ' Provider details panel
        pnlProviderDetails = New Panel With {
            .Location = New Point(170, 30),
            .Size = New Size(310, 280),
            .BorderStyle = BorderStyle.FixedSingle
        }
        Me.Controls.Add(pnlProviderDetails)

        ' Provider name
        lblProviderName = New Label With {
            .Location = New Point(10, 10),
            .Size = New Size(290, 25),
            .Font = New Font("Segoe UI", 12, FontStyle.Bold)
        }
        pnlProviderDetails.Controls.Add(lblProviderName)

        ' Provider status
        lblProviderStatus = New Label With {
            .Location = New Point(10, 40),
            .Size = New Size(290, 20),
            .ForeColor = Color.Gray
        }
        pnlProviderDetails.Controls.Add(lblProviderStatus)

        ' Username
        Dim lblUsername As New Label With {
            .Text = "Username:",
            .Location = New Point(10, 75),
            .AutoSize = True
        }
        pnlProviderDetails.Controls.Add(lblUsername)

        txtUsername = New TextBox With {
            .Location = New Point(10, 95),
            .Size = New Size(290, 25)
        }
        pnlProviderDetails.Controls.Add(txtUsername)

        ' Password
        Dim lblPassword As New Label With {
            .Text = "Password:",
            .Location = New Point(10, 125),
            .AutoSize = True
        }
        pnlProviderDetails.Controls.Add(lblPassword)

        txtPassword = New TextBox With {
            .Location = New Point(10, 145),
            .Size = New Size(290, 25),
            .UseSystemPasswordChar = True
        }
        pnlProviderDetails.Controls.Add(txtPassword)

        ' Login button
        btnLogin = New Button With {
            .Text = "Login",
            .Location = New Point(10, 180),
            .Size = New Size(90, 28),
            .BackColor = Color.FromArgb(0, 122, 204),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnLogin.Click, AddressOf btnLogin_Click
        pnlProviderDetails.Controls.Add(btnLogin)

        ' Logout button
        btnLogout = New Button With {
            .Text = "Logout",
            .Location = New Point(110, 180),
            .Size = New Size(90, 28),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnLogout.Click, AddressOf btnLogout_Click
        pnlProviderDetails.Controls.Add(btnLogout)

        ' Test connection button
        btnTestConnection = New Button With {
            .Text = "Test Connection",
            .Location = New Point(10, 215),
            .Size = New Size(120, 28),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnTestConnection.Click, AddressOf btnTestConnection_Click
        pnlProviderDetails.Controls.Add(btnTestConnection)

        ' Save button
        btnSave = New Button With {
            .Text = "Save",
            .Location = New Point(10, 250),
            .Size = New Size(90, 28),
            .BackColor = Color.FromArgb(0, 150, 0),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnSave.Click, AddressOf btnSave_Click
        pnlProviderDetails.Controls.Add(btnSave)

        ' Close button
        btnClose = New Button With {
            .Text = "Close",
            .Location = New Point(390, 325),
            .Size = New Size(90, 30),
            .DialogResult = DialogResult.Cancel
        }
        AddHandler btnClose.Click, AddressOf btnClose_Click
        Me.Controls.Add(btnClose)

        Me.AcceptButton = btnSave
        Me.CancelButton = btnClose
    End Sub

    Private Sub LoadProviders()
        lstProviders.Items.Clear()

        For Each provider In _searchManager.Providers.Values
            Dim displayText = provider.DisplayName
            If provider.RequiresAuthentication Then
                displayText &= If(provider.IsLoggedIn, " ✓", " ✗")
            Else
                displayText &= " (Public)"
            End If
            lstProviders.Items.Add(displayText)
        Next

        If lstProviders.Items.Count > 0 Then
            lstProviders.SelectedIndex = 0
        End If
    End Sub

    Private Sub lstProviders_SelectedIndexChanged(sender As Object, e As EventArgs)
        If lstProviders.SelectedIndex < 0 Then Return

        Dim providers = _searchManager.Providers.Values.ToList()
        If lstProviders.SelectedIndex < providers.Count Then
            _selectedProvider = providers(lstProviders.SelectedIndex)
            DisplayProviderDetails()
        End If
    End Sub

    Private Sub DisplayProviderDetails()
        If _selectedProvider Is Nothing Then Return

        lblProviderName.Text = _selectedProvider.DisplayName

        ' Show status
        If _selectedProvider.RequiresAuthentication Then
            If _selectedProvider.IsLoggedIn Then
                lblProviderStatus.Text = "Status: Logged In"
                lblProviderStatus.ForeColor = Color.Green
            Else
                lblProviderStatus.Text = "Status: Not logged in"
                lblProviderStatus.ForeColor = Color.Red
            End If
        Else
            lblProviderStatus.Text = "Status: Public (no login required)"
            lblProviderStatus.ForeColor = Color.Gray
        End If

        ' Show/hide login controls
        Dim showLogin = _selectedProvider.RequiresAuthentication
        txtUsername.Enabled = showLogin
        txtPassword.Enabled = showLogin
        btnLogin.Enabled = showLogin AndAlso Not _selectedProvider.IsLoggedIn
        btnLogout.Enabled = showLogin AndAlso _selectedProvider.IsLoggedIn

        ' Load saved credentials
        Dim creds = _searchManager.GetProviderCredentials(_selectedProvider.Name)
        If creds IsNot Nothing Then
            txtUsername.Text = creds.Username
            txtPassword.Text = creds.Password
        Else
            txtUsername.Text = ""
            txtPassword.Text = ""
        End If

        ' Show last error if any
        If Not String.IsNullOrEmpty(_selectedProvider.Status.LastError) Then
            lblProviderStatus.Text &= $" - Error: {_selectedProvider.Status.LastError}"
        End If
    End Sub

    Private Async Sub btnLogin_Click(sender As Object, e As EventArgs)
        If _selectedProvider Is Nothing OrElse Not _selectedProvider.RequiresAuthentication Then Return

        If String.IsNullOrWhiteSpace(txtUsername.Text) OrElse String.IsNullOrWhiteSpace(txtPassword.Text) Then
            MessageBox.Show("Please enter username and password.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Save credentials
        Dim creds As New ProviderCredentials With {
            .Username = txtUsername.Text.Trim(),
            .Password = txtPassword.Text
        }
        _searchManager.SetProviderCredentials(_selectedProvider.Name, creds)

        btnLogin.Enabled = False
        btnLogin.Text = "Logging in..."

        Try
            Dim success = Await _searchManager.LoginProviderAsync(_selectedProvider.Name)

            If success Then
                MessageBox.Show("Login successful!", "Login", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                MessageBox.Show($"Login failed: {_selectedProvider.Status.LastError}", "Login", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If

            LoadProviders()
            DisplayProviderDetails()

        Catch ex As Exception
            MessageBox.Show($"Login failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            btnLogin.Text = "Login"
            btnLogin.Enabled = True
        End Try
    End Sub

    Private Sub btnLogout_Click(sender As Object, e As EventArgs)
        If _selectedProvider Is Nothing Then Return

        _selectedProvider.Logout()
        LoadProviders()
        DisplayProviderDetails()
        MessageBox.Show("Logged out.", "Logout", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Async Sub btnTestConnection_Click(sender As Object, e As EventArgs)
        If _selectedProvider Is Nothing Then Return

        btnTestConnection.Enabled = False
        btnTestConnection.Text = "Testing..."

        Try
            Dim success = Await _selectedProvider.TestConnectionAsync()

            If success Then
                MessageBox.Show("Connection successful!", "Test Connection", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                MessageBox.Show($"Connection failed: {_selectedProvider.Status.LastError}", "Test Connection", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If

        Catch ex As Exception
            MessageBox.Show($"Connection failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            btnTestConnection.Text = "Test Connection"
            btnTestConnection.Enabled = True
        End Try
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs)
        If _selectedProvider Is Nothing OrElse Not _selectedProvider.RequiresAuthentication Then
            Me.DialogResult = DialogResult.OK
            Me.Close()
            Return
        End If

        ' Save credentials
        Dim creds As New ProviderCredentials With {
            .Username = txtUsername.Text.Trim(),
            .Password = txtPassword.Text
        }
        _searchManager.SetProviderCredentials(_selectedProvider.Name, creds)

        MessageBox.Show("Settings saved.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Me.DialogResult = DialogResult.OK
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub
End Class
