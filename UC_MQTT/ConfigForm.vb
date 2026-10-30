Friend Class ConfigForm

    Private UC As Plugininterface.Entry
    Dim PluginMain As UCCNCplugin

    Public Sub New(CallerPluginMain As UCCNCplugin)
        Me.UC = CallerPluginMain.UC
        Me.PluginMain = CallerPluginMain
        InitializeComponent()
    End Sub

    Private Sub ConfigForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub



End Class