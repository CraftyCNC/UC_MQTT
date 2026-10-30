Option Explicit Off

Imports System.Threading
Imports System.Windows.Forms
Imports System.Text
Imports System.Drawing
Imports System.Data
Imports System.IO
Imports System
Imports System.Net.Mail
Imports System.Net

Imports System.Reflection
Imports System.Xml
Imports System.Diagnostics

Imports uPLibrary.Networking.M2Mqtt
Imports uPLibrary.Networking.M2Mqtt.Messages


Public Class PluginForm

#Region "Declarations"

    Friend uccncplugin

    Public UC As Plugininterface.Entry
    Dim PluginMain As UCCNCplugin
    Dim MustClose As Boolean = False

    'Dim IsMovingFlag As Boolean = False ' Add 4-6-19, test threading
    'Dim abortflag As Boolean = False

    'Dim t As New Thread(AddressOf Me.BackgroundProcess)
    'Dim thrMyProc As New Thread(Sub() MyProbeGridThread())
    'Dim thrMyProc As Thread
    Public enablemail As Boolean
    Public LEDState(0 To 5) As Boolean


    ' message elements
    Private mMailServer As String
    Private mTo As String
    Private mFrom As String
    Private mMsg As String
    Private mSubject As String

    Private goingtoclose As Boolean = False
    Private res As DialogResult


    Dim client As MqttClient
    Dim Msg As StringBuilder = New StringBuilder(4096)

    Dim deviceName As String = My.Settings.MachineName
    Dim entityName As String
    Dim Payload As String


#End Region

#Region "Form Start/End"



    Public Sub New(CallerPluginMain As UCCNCplugin)
        Me.UC = CallerPluginMain.UC
        Me.PluginMain = CallerPluginMain
        InitializeComponent()
    End Sub

    Private Sub PluginForm_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load

        '================Tool Tray Icon
        NotifyIcon1.BalloonTipIcon = ToolTipIcon.Info
        NotifyIcon1.BalloonTipText = "UC_MQTT"
        NotifyIcon1.BalloonTipTitle = "UC MQTT is running"
        NotifyIcon1.ShowBalloonTip(2000)

        btnEngableToggle.Text = "Click to enable"
        btnEngableToggle.BackColor = Color.PaleVioletRed
        '====================


        '=====================

        enablemail = False

        '======= over-ride by loading saved settings
        Try
            Dim xmlpath As String = Application.StartupPath & "\plugins\UC MQTT.xml"

            Dim FS As New FormSerialisor
            FS.Deserialise(Me, xmlpath)
        Catch ex As Exception
            MsgBox("Error loading saved settings from XML file.")
        End Try

        tbBroker.Text = My.Settings.Broker
        tbMachineTopic.Text = My.Settings.MachineName
        tbPort.Text = My.Settings.Port
        cbAutoConnect.Checked = My.Settings.autoConnect
        chkboxAutoEnable.Checked = My.Settings.autoEnable

        ' ==== Setup buttons/LEDs

        If chkboxLED1.Checked Then
            chkboxLED1.Text = "LED ON"
            chkboxLED1.BackColor = Color.PaleGreen
            LEDState(1) = True

        Else
            chkboxLED1.Text = "LED OFF"
            chkboxLED1.BackColor = Color.PaleVioletRed
            LEDState(1) = False
        End If

        If chkboxLED2.Checked Then
            chkboxLED2.Text = "LED ON"
            chkboxLED2.BackColor = Color.PaleGreen
            LEDState(2) = True

        Else
            chkboxLED2.Text = "LED OFF"
            chkboxLED2.BackColor = Color.PaleVioletRed
            LEDState(2) = False
        End If

        If chkboxLED3.Checked Then
            chkboxLED3.Text = "LED ON"
            chkboxLED3.BackColor = Color.PaleGreen
            LEDState(3) = True

        Else
            chkboxLED3.Text = "LED OFF"
            chkboxLED3.BackColor = Color.PaleVioletRed
            LEDState(3) = False
        End If

        If chkboxLED4.Checked Then
            chkboxLED4.Text = "LED ON"
            chkboxLED4.BackColor = Color.PaleGreen
            LEDState(4) = True

        Else
            chkboxLED4.Text = "LED OFF"
            chkboxLED4.BackColor = Color.PaleVioletRed
            LEDState(4) = False
        End If

        If chkboxLED5.Checked Then
            chkboxLED5.Text = "LED ON"
            chkboxLED5.BackColor = Color.PaleGreen
            LEDState(5) = True

        Else
            chkboxLED5.Text = "LED OFF"
            chkboxLED5.BackColor = Color.PaleVioletRed
            LEDState(5) = False
        End If

        If Not CheckBox1.Checked Then
            comboBox1.Enabled = False
            chkboxLED1.Enabled = False
        Else
            comboBox1.Enabled = True
            chkboxLED1.Enabled = True
        End If

        If Not CheckBox2.Checked Then
            ComboBox2.Enabled = False
            chkboxLED2.Enabled = False
        Else
            ComboBox2.Enabled = True
            chkboxLED2.Enabled = True
        End If

        If Not CheckBox3.Checked Then
            ComboBox3.Enabled = False
            chkboxLED3.Enabled = False
        Else
            ComboBox3.Enabled = True
            chkboxLED3.Enabled = True
        End If

        If Not CheckBox4.Checked Then
            ComboBox4.Enabled = False
            chkboxLED4.Enabled = False
        Else
            ComboBox4.Enabled = True
            chkboxLED4.Enabled = True
        End If

        If Not CheckBox5.Checked Then
            ComboBox5.Enabled = False
            chkboxLED5.Enabled = False
        Else
            ComboBox5.Enabled = True
            chkboxLED5.Enabled = True
        End If
        '================================================







    End Sub


    Private Sub PluginForm_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        If My.Settings.autoConnect = True Then
            Try
                ' btnConnect.PerformClick()
                cbAutoConnect.Checked = True
                Connect()
            Catch ex As Exception

            End Try
        End If

        If My.Settings.autoEnable = True Then
            AutoEnableToolStripMenuItem.Checked = True
            chkboxAutoEnable.Checked = True

            Doenablemail()

        End If
    End Sub

    Private Sub PluginForm_FormClosing(sender As System.Object, e As System.Windows.Forms.FormClosingEventArgs) Handles MyBase.FormClosing
        ' Do not close the form when the red X button is pressed
        ' But start a Thread which will stop the Loop call from the UCCNC
        ' to prevent the form closing while there is a GUI update in the Loop event

        If goingtoclose = False Then

            res = MessageBox.Show("OK to close, Cancel to minimize to taskbar.", "Close?", MessageBoxButtons.OKCancel, MessageBoxIcon.Information)

        End If

        If res = DialogResult.Cancel Then

            e.Cancel = True
            Me.WindowState = FormWindowState.Minimized

        Else

            If Not MustClose Then
                goingtoclose = True
                e.Cancel = True
                Dim thrClose As New Thread(Sub() Closeform())
                thrClose.CurrentCulture = Thread.CurrentThread.CurrentCulture ' Preserve regional settings
                thrClose.Start()
            Else
                ' Form is closing here...

            End If

        End If


    End Sub

    Public Sub Closeform()

        btnDisconnect.PerformClick()

        ' Stop the Loop event to update the GUI
        PluginMain.LoopStop = True
        ' Wait until the loop exited
        While (PluginMain.LoopWorking)
            Thread.Sleep(10)
        End While
        ' Set the mustclose variable to true and call the .Close() function to close the Form
        MustClose = True
        Me.Close()
    End Sub





    Public Function fn(ByRef val As Double)
        Return String.Format("{0:F4}", val)
        'Return String.Format("f6", val)
    End Function

#End Region


#Region "MQTT Import"
    ' Source - https://stackoverflow.com/a
    ' Posted by Ali, modified by community. See post 'Timeline' for change history
    ' Retrieved 2025-11-28, License - CC BY-SA 4.0

    Private Sub MyWait(ByVal seconds As Integer)
        For i As Integer = 0 To seconds * 100
            System.Threading.Thread.Sleep(10)
            Application.DoEvents()
        Next
    End Sub



    Private Sub Connect()
        Dim BrokerAddress As String = tbBroker.Text
        Dim BrokerPort As Integer

        Try
            If (tbBroker.Text.Length <> 0) Then

                'Setup the port
                If tbPort.Text.Length <> 0 Then
                    BrokerPort = Convert.ToInt16(tbPort.Text)

                Else
                    BrokerPort = 1883
                End If

                Try

                    ' // Specify a custom port (e.g., 1884 instead of the default 1883)
                    ' MqttClient client = New MqttClient(IPAddress.Parse("192.168.1.100"), 1884, False, null, null, MqttSslProtocols.None); 

                    '// Or if using a hostname
                    '// MqttClient client = New MqttClient("your.mqtt.broker.com", 1884, false, null, null, MqttSslProtocols.None);

                    ' The hostname constructor also accepts an IP address string
                    client = New MqttClient(BrokerAddress, BrokerPort, False, Nothing, Nothing, MqttSslProtocols.None)


                    Dim clientId As String = Guid.NewGuid().ToString()

                    AddHandler client.MqttMsgPublishReceived, AddressOf Client_MqttMsgPublishReceived
                    AddHandler client.ConnectionClosed, AddressOf Client_Disconnect

                    client.Connect(clientId)

                    If client.IsConnected Then
                        comboBox1.SelectedIndex = 0
                        ComboBox2.SelectedIndex = 0

                        ToolStripStatusLabel1.Text = "Connected to " + "'" + tbBroker.Text + "'"
                    Else
                        ToolStripStatusLabel1.Text = "Disconnected"
                    End If

                Catch ex As Exception

                    ToolStripStatusLabel1.Text = "Error"
                    MsgBox(ex.Message(), MsgBoxStyle.Critical)
                End Try
            Else

                ToolStripStatusLabel1.Text = "Please enter a valid Broker IP address or Hostname, verify the port"

            End If
        Catch ex As Exception

        Finally

        End Try
    End Sub


    Private Sub mybtnConnect(sender As Object, e As EventArgs) Handles btnConnect.Click

        Connect()

    End Sub

    Private Sub Disconnect(sender As Object, e As EventArgs) Handles btnDisconnect.Click
        If (client IsNot Nothing AndAlso client.IsConnected()) Then
            client.Disconnect()
        Else
            ToolStripStatusLabel1.Text = "Error"
        End If
    End Sub
    Private Sub Client_Disconnect(sender As Object, e As EventArgs)
        ToolStripStatusLabel1.Text = "Connection Lost"
    End Sub

    Private Sub Client_MqttMsgPublishReceived(ByVal sender As Object, ByVal e As MqttMsgPublishEventArgs)
        Msg.AppendLine()
        Msg.Append("[" + TimeOfDay.ToString("h:mm:ss tt") + "] Topic: " + e.Topic.ToString() + ", Len: " + e.Message.Length.ToString() + ", Qos: " + e.QosLevel.ToString())
        Msg.AppendLine()
        Msg.Append("Msg : " + Encoding.Default.GetString(e.Message))
        Msg.AppendLine()

        SetText(Msg.ToString)

    End Sub

    Delegate Sub SetTextCallback(newString As String)
    Private Sub SetText(ByVal newString As String)
        ' Calling from another thread? -> Use delegate
        If Me.RichTextBox2.InvokeRequired Then
            Dim d As New SetTextCallback(AddressOf SetText)
            ' Execute delegate in the UI thread, pass args as an array
            Me.Invoke(d, New Object() {newString})
        Else ' Same thread, assign string to the textbox
            Me.RichTextBox2.Text = newString
        End If
    End Sub

    Private Sub Subscribe(sender As Object, e As EventArgs) Handles btnSubscribe.Click
        If (client IsNot Nothing AndAlso client.IsConnected()) Then
            If (TextBox3.Text.Length <> 0) Then

                Try
                    Dim Topic() As String = {TextBox3.Text}
                    Dim Qos() As Byte = {comboBox1.SelectedIndex}
                    client.Subscribe(Topic, Qos)
                    ToolStripStatusLabel1.Text = "Subscribe to " + "{" + TextBox3.Text + "}"

                Catch ex As Exception
                    ToolStripStatusLabel1.Text = "Error"
                    MsgBox(ex.Message, MsgBoxStyle.Critical)

                End Try


            Else
                ToolStripStatusLabel1.Text = "Please enter a valid topic "
            End If


        Else
            ToolStripStatusLabel1.Text = "Disconnected !! subscription procedure is not valid"
        End If


    End Sub

    Private Sub Publish(sender As Object, e As EventArgs) Handles btnPublish.Click
        If (client IsNot Nothing AndAlso client.IsConnected()) Then
            If (tbTestTopic.Text.Length <> 0) Then
                Try
                    Dim Qos As Byte = ComboBox2.SelectedIndex
                    client.Publish(tbTestTopic.Text, Encoding.Default.GetBytes(rtbTestMessage.Text), Qos, False)
                    ToolStripStatusLabel1.Text = "Publish to " + "{" + tbTestTopic.Text + "}"
                Catch ex As Exception
                    ToolStripStatusLabel1.Text = "Error"
                    MsgBox(ex.Message, MsgBoxStyle.Critical)
                End Try


            Else

                ToolStripStatusLabel1.Text = "Please enter a valid topic "
            End If
        Else
            ToolStripStatusLabel1.Text = "Disconnected !! Publish procedure is not valid"
        End If


    End Sub

    Private Sub ClearTest(sender As Object, e As EventArgs) Handles btnDeleteTest.Click
        If (client IsNot Nothing AndAlso client.IsConnected()) Then
            'If (tbMachineTopic.Text.Length <> 0) Then
            Try
                    Dim Qos As Byte = ComboBox2.SelectedIndex
                    client.Publish(tbTestTopic.Text, Encoding.Default.GetBytes(""), Qos, False)
                    ToolStripStatusLabel1.Text = "Cleared topic of " + "{" + tbTestTopic.Text + "}"
                Catch ex As Exception
                    ToolStripStatusLabel1.Text = "Error"
                    MsgBox(ex.Message, MsgBoxStyle.Critical)
                End Try


            'Else

            '    ToolStripStatusLabel1.Text = "Please enter a valid topic "
            'End If
        Else
        ToolStripStatusLabel1.Text = "Disconnected !! Publish procedure is not valid"
        End If


    End Sub



#Region "Discovery"


    Private Sub SetupDiscoveryForAll()

        If tbMachineTopic.Text <> "" Then
            deviceName = tbMachineTopic.Text
            My.Settings.MachineName = deviceName
            My.Settings.Save()


            'deviceName = tbMachineTopic.Text
            MsgBox("Updated Device ID to: " & deviceName.ToString)
        Else
            MsgBox("Device ID is: " & deviceName.ToString)
        End If


        ' Dim i As Integer = 1

        'Dim Device As String = "CNCRouter"
        Dim entityList As New List(Of String) From {"estop_status", "limit_status", "run_status", "spindle_status", "run_time", "current_line", "percent_complete"}
        'Dim entityList As New List(Of String) From {"estop_status"}


        For Each entity In entityList
            Dim entityID As String
            entityID = entity

            PublishDiscoveryMessage(deviceName, entity, entityID)
            'i = i + 1


        Next





    End Sub


    Private Sub PublishDiscoveryMessage(devicename As String, entityname As String, entityIdentifier As String)
        ' Home Assistant MQTT Discovery topic format:
        ' <discovery_prefix>/<component>/[<node_id>/]<object_id>/config
        ' Default discovery prefix is "homeassistant"


        Dim uniqueId As String = devicename & "_" & entityIdentifier ' "my_custom_device_UCCNC_001"
        ' Dim entityName As String = "run_status"
        ' Dim entityIdentifier As String = "UCCNC_001"
        ' Dim deviceName = "CNC_Router"
        Dim mfg = "CraftyCNC"


        Dim discoveryTopic As String = "homeassistant/sensor/" & devicename & "/" & entityname & "/config"
        Dim stateTopic As String = devicename & "/" & entityname & "/state"

        '' Create the discovery payload as a JSON string
        'Dim discoveryPayload As New With {
        '    .name = entityname,
        '    .unique_id = uniqueId,
        '    .state_topic = stateTopic,
        '    .unit_of_measurement = "",
        '.device_class = "",
        '.device = New With {
        '        .identifiers = "UCMQTT",
        '        .name = devicename,
        '        .manufacturer = mfg
        '    }
        '}


        ' Create the discovery payload as a JSON string, Deleted UOM and DeviceClass based on testing
        Dim discoveryPayload As New With {
            .name = entityname,
            .unique_id = uniqueId,
            .state_topic = stateTopic,
            .device = New With {
                .identifiers = "UCMQTT",
                .name = devicename,
                .manufacturer = mfg
            }
        }

        Dim jsonPayload As String = Newtonsoft.Json.JsonConvert.SerializeObject(discoveryPayload)
        Dim payloadBytes As Byte() = Encoding.UTF8.GetBytes(jsonPayload)

        ' Publish the message with the RETAIN flag set to True
        client.Publish(discoveryTopic, payloadBytes, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, True)
        ToolStripStatusLabel1.Text = ($"Published discovery message To topic: {discoveryTopic}")

        ' After discovery, you can start publishing state updates to the stateTopic
        ' For example:
        ' Dim statePayload As Byte() = Encoding.UTF8.GetBytes("22.5")
        ' client.Publish(stateTopic, statePayload, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, False)
    End Sub

#End Region





    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles btnClear.Click
        Msg.Clear()
        RichTextBox2.Clear()
    End Sub



    Private Sub timerUpdate_Tick(sender As Object, e As EventArgs) Handles timerUpdate.Tick

        ' deviceName = tbTopic.Text

        faultcheck()

        If PluginMain.filefinished = True Then

            DC_CommandOFF()


            PluginMain.filefinished = False

        End If


    End Sub


    Private Sub DC_CommandOFF()

        If (client IsNot Nothing AndAlso client.IsConnected()) Then
            Dim DCTopic As String = "DustCollector"


            Try
                Dim Qos As Byte = ComboBox2.SelectedIndex
                client.Publish(DCTopic, Encoding.Default.GetBytes("OFF"), Qos, False)
                ToolStripStatusLabel1.Text = "Publish to " + "{" + DCTopic + "}"
            Catch ex As Exception
                ToolStripStatusLabel1.Text = "Error"
                MsgBox(ex.Message, MsgBoxStyle.Critical)

            End Try

        Else
            ToolStripStatusLabel1.Text = "Disconnected !! Publish procedure is not valid"
        End If
    End Sub




    Private Sub SendMQTT(deviceName As String, entityName As String, payload As String)

        If (client IsNot Nothing AndAlso client.IsConnected()) Then
            If (Me.deviceName.Length <> 0) Then

                'Dim entityName As String = "run_status"
                'Dim entityIdentifier As String = "UCCNC_001"
                'Dim deviceName = "CNC_Router"
                'Dim mfg = "CraftyCNC"
                'Dim discoveryTopic As String = "homeassistant/sensor/" & deviceName & "/" & entityName & "/config"
                'Dim stateTopic As String = deviceName & "/" & entityName & "/state"

                Try
                    Dim messageTopic As String
                    'Dim deviceName As String = "CNC_Router"

                    messageTopic = deviceName & "/" & entityName & "/state"

                    Dim Qos As Byte = cbQOS.SelectedIndex
                    client.Publish(messageTopic, Encoding.Default.GetBytes(payload), Qos, False)
                    ToolStripStatusLabel1.Text = "Publish to " + "{" + tbMachineTopic.Text + "}"
                Catch ex As Exception
                    ToolStripStatusLabel1.Text = "Error Sending Message"
                    MsgBox(ex.Message, MsgBoxStyle.Critical)
                End Try


            Else

                ToolStripStatusLabel1.Text = "Please enter a valid topic "
            End If
        Else
            ToolStripStatusLabel1.Text = "Disconnected !! Publish procedure is not valid"
        End If


    End Sub

    Public Function faultcheck() As Boolean

        faultcheck = False

        If ChkBoxEstop.Checked Then

            entityName = "estop_status"

            If UC.GetLED(25) Or UC.GetLED(36) Then ' Check for  Estop (36) and reset (25)
                Try

                    Payload = "Estop"
                    ' UC.AddStatusmessage("UC MQTT Detected EStop Fault!!")

                Catch ex As Exception

                End Try

            Else

                Payload = "Cleared"

            End If

            SendMQTT(deviceName, entityName, Payload)

        End If





        If chkboxLimit.Checked Then
            entityName = "limit_status"

            If UC.GetLED(26) Then ' Check for  Limit (26)
                Try

                    Payload = "Hard Limit"
                    ' UC.AddStatusmessage("UC MQTT Detected Hard Limit")

                Catch ex As Exception

                End Try

            ElseIf UC.GetLED(268) Then
                Try

                    Payload = "Soft Limit"
                    'UC.AddStatusmessage("UC MQTT Detected Soft Limit")

                Catch ex As Exception

                End Try

            Else

                Payload = "Cleared"

            End If

            SendMQTT(deviceName, entityName, Payload)

        End If




        If chkboxCycleDone.Checked Then
            entityName = "run_status"


            If UC.GetLED(54) Then ' Check for  in Cucle (54)
                Try

                    Payload = "Running"
                    ' UC.AddStatusmessage("UC MQTT Detected Hard Limit")

                Catch ex As Exception

                End Try

            Else

                Payload = "Idle"

            End If

            SendMQTT(deviceName, entityName, Payload)

        End If


        If chkboxSpindle.Checked Then
            entityName = "spindle_status"


            If UC.GetLED(50) Or UC.GetLED(51) Then ' Check for  Limit (26)
                Try

                    Payload = "Running"
                    'UC.AddStatusmessage("UC MQTT Detected Hard Limit")

                Catch ex As Exception

                End Try

            Else

                Payload = "Idle"

            End If

            SendMQTT(deviceName, entityName, Payload)




        End If


        Try
            entityName = "current_line"
            Payload = UC.Getcurrentgcodelinenumber.ToString
            SendMQTT(deviceName, entityName, Payload)



            Dim pctcomplete As String
            If UC.GetLED(54) Then ' Check for  in Cucle (54)
                entityName = "percent_complete"
                Dim curline As Integer = Convert.ToInt32(UC.Getcurrentgcodelinenumber)
                Dim totallines As Integer = Convert.ToInt32(UC.Getfield(True, 894)) ' total object field from job status page


                If totallines <> 0 Then
                    pctcomplete = fn(curline / totallines * 100)
                    Payload = FormatNumber(pctcomplete, 2).ToString

                Else
                    pctcomplete = "NA"
                    Payload = pctcomplete

                End If
            Else
                pctcomplete = "NA"
                Payload = pctcomplete

            End If
            SendMQTT(deviceName, entityName, Payload)



        Catch ex As Exception

        End Try


        Try
            entityName = "run_time"

            Payload = (UC.Getfield(True, 898)) ' Time field

            SendMQTT(deviceName, entityName, Payload)
        Catch ex As Exception

        End Try








        'If CheckBox1.Checked Then

        '        If UC.GetLED(Convert.ToInt16(comboBox1.Text)) <> chkboxLED1.Checked Then ' 
        '            Try

        '                faultcheck = True
        '                UC.AddStatusmessage("UC MQTT Detected LED " & comboBox1.Text & " as a trigger")

        '                If txtboxLED1Msg.Text <> "" Then
        '                    msgmessage = txtboxLED1Msg.Text
        '                Else
        '                    msgmessage = "LED " & comboBox1.Text & " triggered"
        '                End If

        '                fullmsg = fullmsg & vbNewLine & msgmessage

        '            Catch ex As Exception

        '            End Try

        '        End If

        '    End If


        '    If CheckBox2.Checked Then

        '        If UC.GetLED(Convert.ToInt16(ComboBox2.Text)) <> chkboxLED2.Checked Then ' 
        '            Try

        '                faultcheck = True
        '                UC.AddStatusmessage("UC MQTT Detected LED " & ComboBox2.Text & " as a trigger")

        '                If txtboxLED2Msg.Text <> "" Then
        '                    msgmessage = txtboxLED2Msg.Text
        '                Else
        '                    msgmessage = "LED " & ComboBox2.Text & " triggered"
        '                End If

        '                fullmsg = fullmsg & vbNewLine & msgmessage

        '            Catch ex As Exception

        '            End Try

        '        End If

        '    End If

        '    If CheckBox3.Checked Then

        '        If UC.GetLED(Convert.ToInt16(ComboBox3.Text)) <> chkboxLED3.Checked Then ' 
        '            Try

        '                faultcheck = True
        '                UC.AddStatusmessage("UC MQTT Detected LED " & ComboBox3.Text & " as a trigger")

        '                If txtboxLED3Msg.Text <> "" Then
        '                    msgmessage = txtboxLED3Msg.Text
        '                Else
        '                    msgmessage = "LED " & ComboBox3.Text & " triggered"
        '                End If

        '                fullmsg = fullmsg & vbNewLine & msgmessage

        '            Catch ex As Exception

        '            End Try

        '        End If

        '    End If

        '    If CheckBox4.Checked Then

        '        If UC.GetLED(Convert.ToInt16(ComboBox4.Text)) <> chkboxLED4.Checked Then ' 
        '            Try

        '                faultcheck = True
        '                UC.AddStatusmessage("UC MQTT Detected LED " & ComboBox4.Text & " as a trigger")

        '                If txtboxLED4Msg.Text <> "" Then
        '                    msgmessage = txtboxLED4Msg.Text
        '                Else
        '                    msgmessage = "LED " & ComboBox4.Text & " triggered"
        '                End If

        '                fullmsg = fullmsg & vbNewLine & msgmessage

        '            Catch ex As Exception

        '            End Try

        '        End If

        '    End If



        '    If CheckBox5.Checked Then

        '        If UC.GetLED(Convert.ToInt16(ComboBox5.Text)) <> chkboxLED5.Checked Then ' 
        '            Try

        '                faultcheck = True
        '                UC.AddStatusmessage("UC MQTT Detected LED " & ComboBox5.Text & " as a trigger")

        '                If txtboxLED5Msg.Text <> "" Then
        '                    msgmessage = txtboxLED5Msg.Text
        '                Else
        '                    msgmessage = "LED " & ComboBox5.Text & " triggered"
        '                End If

        '                fullmsg = fullmsg & vbNewLine & msgmessage

        '            Catch ex As Exception

        '            End Try

        '        End If

        '    End If









        'If faultcheck = True Then

        '        ' sendMessage(msgsubject, fullmsg)

        '        UC.AddStatusmessage("Message Sent, disabling UC MQTT!")

        '        Dodisablemail()
        '    End If







    End Function



#End Region



#Region "TextBoxes & Form stuff"
    '=================================== Text Box input filtering =====================

    'Private Sub TextBox12_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs)
    '    If (Not Char.IsControl(e.KeyChar) _
    '                 AndAlso (Not Char.IsDigit(e.KeyChar) _
    '                 AndAlso (e.KeyChar <> Microsoft.VisualBasic.ChrW(46)))) Then
    '        e.Handled = True
    '    End If
    'End Sub







    Protected Overrides Function ProcessCmdKey(ByRef msg As System.Windows.Forms.Message, ByVal keyData As System.Windows.Forms.Keys) As Boolean
        Select Case keyData
            Case Keys.F2
                ' Do something 

            Case Keys.F3
                ' Do more

            Case Keys.Escape
                ' Crap

            Case Keys.F12
                'RecordPoint()

                'Case Keys.Space
                'Button10.PerformClick()


            Case Else
                Return MyBase.ProcessCmdKey(msg, keyData)



        End Select

        Return True
    End Function

#End Region

#Region "Buttons"

    '
    Private Sub btnSaveSettings_Click(sender As Object, e As EventArgs) Handles btnSaveSettings.Click
        formsave()
    End Sub








#End Region


#Region "Checkbox toggles"


    Private Sub button4_Click(sender As Object, e As EventArgs) Handles btnLEDHelp.Click
        linkLabel1.LinkVisited = True
        ' //Call the Process.Start method to open the default browser 
        ' //with a URL:
        System.Diagnostics.Process.Start(Application.StartupPath & "\documentation\LEDs_by_number.htm")
    End Sub

    Private Sub CheckBox6_CheckedChanged(sender As Object, e As EventArgs) Handles chkboxLED1.CheckedChanged


        If Me.chkboxLED1.Checked = True Then
            Me.chkboxLED1.Text = "LED ON"
            Me.chkboxLED1.BackColor = Color.PaleGreen
            LEDState(1) = True
        Else
            Me.chkboxLED1.Text = "LED OFF"
            Me.chkboxLED1.BackColor = Color.PaleVioletRed
            LEDState(1) = False
        End If

    End Sub

    Private Sub chkboxLED2_CheckedChanged(sender As Object, e As EventArgs) Handles chkboxLED2.CheckedChanged


        If Me.chkboxLED2.Checked = True Then
            Me.chkboxLED2.Text = "LED ON"
            Me.chkboxLED2.BackColor = Color.PaleGreen
            LEDState(2) = True
        Else
            Me.chkboxLED2.Text = "LED OFF"
            Me.chkboxLED2.BackColor = Color.PaleVioletRed
            LEDState(2) = False
        End If

        'Label5.Text = LEDState(i)
    End Sub

    Private Sub chkboxLED3_CheckedChanged(sender As Object, e As EventArgs) Handles chkboxLED3.CheckedChanged


        If Me.chkboxLED3.Checked = True Then
            Me.chkboxLED3.Text = "LED ON"
            Me.chkboxLED3.BackColor = Color.PaleGreen
            LEDState(3) = True
        Else
            Me.chkboxLED3.Text = "LED OFF"
            Me.chkboxLED3.BackColor = Color.PaleVioletRed
            LEDState(3) = False
        End If
    End Sub

    Private Sub chkboxLED4_CheckedChanged(sender As Object, e As EventArgs) Handles chkboxLED4.CheckedChanged
        If Me.chkboxLED4.Checked = True Then
            Me.chkboxLED4.Text = "LED ON"
            Me.chkboxLED4.BackColor = Color.PaleGreen
            LEDState(4) = True
        Else
            Me.chkboxLED4.Text = "LED OFF"
            Me.chkboxLED4.BackColor = Color.PaleVioletRed
            LEDState(4) = False
        End If
    End Sub

    Private Sub chkboxLED5_CheckedChanged(sender As Object, e As EventArgs) Handles chkboxLED5.CheckedChanged
        If Me.chkboxLED5.Checked = True Then
            Me.chkboxLED5.Text = "LED ON"
            Me.chkboxLED5.BackColor = Color.PaleGreen
            LEDState(5) = True
        Else
            Me.chkboxLED5.Text = "LED OFF"
            Me.chkboxLED5.BackColor = Color.PaleVioletRed
            LEDState(5) = False
        End If
    End Sub






    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged
        If Not CheckBox1.Checked Then
            comboBox1.Enabled = False
            chkboxLED1.Enabled = False
        Else
            comboBox1.Enabled = True
            chkboxLED1.Enabled = True
        End If
    End Sub

    Private Sub CheckBox2_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox2.CheckedChanged
        If Not CheckBox2.Checked Then
            ComboBox2.Enabled = False
            chkboxLED2.Enabled = False
        Else
            ComboBox2.Enabled = True
            chkboxLED2.Enabled = True
        End If
    End Sub

    Private Sub CheckBox3_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox3.CheckedChanged
        If Not CheckBox3.Checked Then
            ComboBox3.Enabled = False
            chkboxLED3.Enabled = False
        Else
            ComboBox3.Enabled = True
            chkboxLED3.Enabled = True
        End If
    End Sub

    Private Sub CheckBox4_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox4.CheckedChanged
        If Not CheckBox4.Checked Then
            ComboBox4.Enabled = False
            chkboxLED4.Enabled = False
        Else
            ComboBox4.Enabled = True
            chkboxLED4.Enabled = True
        End If
    End Sub

    Private Sub CheckBox5_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox5.CheckedChanged
        If Not CheckBox5.Checked Then
            ComboBox5.Enabled = False
            chkboxLED5.Enabled = False
        Else
            ComboBox5.Enabled = True
            chkboxLED5.Enabled = True
        End If
    End Sub
#End Region


#Region "Toggle mail active"

    Private Sub NotifyIcon1_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles NotifyIcon1.MouseDoubleClick

        toggleEnable()


        'MessageBox.Show("Someone just double clicked on the icon!")
        'NotifyIcon1.Visible = False
    End Sub

    Private Sub Enable(sender As Object, e As EventArgs) Handles btnEngableToggle.Click
        toggleEnable()
        PluginMain.filefinished = False
    End Sub


    Private Sub toggleEnable()
        If enablemail = True Then
            Dodisablemail()
        Else
            Doenablemail()
            ' timerUpdate.Enabled = False

        End If
    End Sub


    Sub Dodisablemail()
        enablemail = False

        NotifyIcon1.BalloonTipTitle = "UC MQTT - Disabled"
        NotifyIcon1.BalloonTipText = "Disabling email sending"
        NotifyIcon1.Text = "UC MQTT Disabling"
        NotifyIcon1.ShowBalloonTip(5000)
        NotifyIcon1.Icon = My.Resources.Red

        btnEngableToggle.Text = "Click to enable"
        btnEngableToggle.BackColor = Color.PaleVioletRed

        timerUpdate.Enabled = False

    End Sub

    Sub Doenablemail()
        Try
            'MyWait(2)

            If (Me.deviceName.Length <> 0) Then
                If (client IsNot Nothing AndAlso client.IsConnected()) Then

                    enablemail = True
                    NotifyIcon1.BalloonTipTitle = "UC MQTT - Enabled"
                    NotifyIcon1.BalloonTipText = "Enabling email sending"
                    NotifyIcon1.Text = "UC MQTT Enabled"
                    NotifyIcon1.ShowBalloonTip(5000)
                    NotifyIcon1.Icon = My.Resources.Green

                    btnEngableToggle.Text = "Click to disable"
                    btnEngableToggle.BackColor = Color.PaleGreen
                    'new icon
                    '            NotifyIcon1.Icon = proper

                    timerUpdate.Interval = nudUpdateRate.Value * 1000
                    timerUpdate.Enabled = True

                Else ' Isn't yet connected, try again

                    'MyWait(2)


                    If (client IsNot Nothing AndAlso client.IsConnected()) Then

                        enablemail = True

                        NotifyIcon1.BalloonTipTitle = "UC MQTT - Enabled"
                        NotifyIcon1.BalloonTipText = "Enabling email sending"
                        NotifyIcon1.Text = "UC MQTT Enabled"
                        NotifyIcon1.ShowBalloonTip(5000)
                        NotifyIcon1.Icon = My.Resources.Green

                        btnEngableToggle.Text = "Click to disable"
                        btnEngableToggle.BackColor = Color.PaleGreen
                        'new icon
                        '            NotifyIcon1.Icon = proper

                        timerUpdate.Interval = nudUpdateRate.Value * 1000
                        timerUpdate.Enabled = True

                    Else

                        MessageBox.Show("Need to  Connect to MQTT Servier...")
                    End If

                End If
                MessageBox.Show("Need to setup Device Name and Connect to MQTT Servier")

            End If


            'enablemail = True

            'NotifyIcon1.BalloonTipTitle = "UC MQTT - Enabled"
            'NotifyIcon1.BalloonTipText = "Enabling email sending"
            'NotifyIcon1.Text = "UC MQTT Enabled"
            'NotifyIcon1.ShowBalloonTip(5000)
            'NotifyIcon1.Icon = My.Resources.Green

            'btnEngableToggle.Text = "Click to disable"
            'btnEngableToggle.BackColor = Color.PaleGreen
            ''new icon
            ''            NotifyIcon1.Icon = proper

            'timerUpdate.Interval = nudUpdateRate.Value * 1000
            'timerUpdate.Enabled = True
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub

#End Region





    Public Sub formsave()

        Try
            Dim xmlpath As String = Application.StartupPath & "\plugins\UC MQTT.xml"
            Dim FS As New FormSerialisor

            FS.Serialise(Me, xmlpath)

        Catch ex As Exception
            MsgBox("Error saving settings to XML file.")

        End Try

        My.Settings.MachineName = tbMachineTopic.Text
        My.Settings.Broker = tbBroker.Text
        My.Settings.Port = tbPort.Text
        My.Settings.autoConnect = cbAutoConnect.Checked
        My.Settings.autoEnable = chkboxAutoEnable.Checked


        My.Settings.Save()
        ToolStripStatusLabel1.Text = ("Settings Saved")
    End Sub



    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        Me.Close()
    End Sub

    Private Sub ShowPluginToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ShowPluginToolStripMenuItem.Click
        Me.Show()
        Me.WindowState = FormWindowState.Normal
        Me.ShowInTaskbar = True
    End Sub

    Private Sub EnableToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles EnableToolStripMenuItem.Click
        Doenablemail()
    End Sub

    Private Sub DisableToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DisableToolStripMenuItem.Click
        Dodisablemail()
    End Sub

    Private Sub PluginForm_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        If (Me.WindowState = FormWindowState.Minimized) Then
            NotifyIcon1.Visible = True
            Me.Hide()
        End If

    End Sub

    Private Sub AutoEnableToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AutoEnableToolStripMenuItem.Click
        If chkboxAutoEnable.Checked Then
            chkboxAutoEnable.Checked = False
            AutoEnableToolStripMenuItem.Checked = False

        Else
            chkboxAutoEnable.Checked = True
            AutoEnableToolStripMenuItem.Checked = True

        End If
    End Sub

    Private Sub nudUpdateRate_ValueChanged(sender As Object, e As EventArgs) Handles nudUpdateRate.ValueChanged
        timerUpdate.Interval = nudUpdateRate.Value * 1000
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If (client IsNot Nothing AndAlso client.IsConnected()) Then
            SetupDiscoveryForAll()

        Else
            MessageBox.Show("Connect to MQTT server first.")
        End If


    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        DC_CommandOFF()
    End Sub


    'Private Sub cbAutoConnect_CheckedChanged(sender As Object, e As EventArgs) Handles cbAutoConnect.CheckedChanged
    '    If (client IsNot Nothing AndAlso client.IsConnected()) Then
    '        'My.Settings.autoConnect = True
    '        'My.Settings.Save()

    '    Else
    '        cbAutoConnect.Checked = False
    '        ' MessageBox.Show("Connect to MQTT server and save settings first.")

    '    End If


    'End Sub

    'Private Sub chkboxAutoEnable_CheckedChanged(sender As Object, e As EventArgs) Handles chkboxAutoEnable.CheckedChanged
    '    If (client IsNot Nothing AndAlso client.IsConnected()) Then
    '        'My.Settings.autoEnable = True
    '        'My.Settings.Save()
    '    Else
    '        chkboxAutoEnable.Checked = False
    '        ' MessageBox.Show("Connect to MQTT server and save settings first.")

    '    End If
    'End Sub



















    'Private Sub chkboxAutoEnable_CheckedChanged(sender As Object, e As EventArgs) Handles chkboxAutoEnable.CheckedChanged
    '    If chkboxAutoEnable.Checked Then
    '        NumericUpDown1.Enabled = True
    '    Else
    '        NumericUpDown1.Enabled = False
    '    End If
    'End Sub
End Class

' THIS WORKS

Public Class FormSerialisor
    Sub Serialise(ByVal c As Control, ByVal XmlFileName As String)
        Dim xmlSerialisedForm As XmlTextWriter = New XmlTextWriter(XmlFileName, System.Text.Encoding.[Default])
        xmlSerialisedForm.Formatting = Formatting.Indented
        xmlSerialisedForm.WriteStartDocument()
        xmlSerialisedForm.WriteStartElement("ChildForm")
        AddChildControls(xmlSerialisedForm, c)
        xmlSerialisedForm.WriteEndElement()
        xmlSerialisedForm.WriteEndDocument()
        xmlSerialisedForm.Flush()
        xmlSerialisedForm.Close()
    End Sub

    Private Sub AddChildControls(ByVal xmlSerialisedForm As XmlTextWriter, ByVal c As Control)
        For Each childCtrl As Control In c.Controls
            If childCtrl.Tag <> "ignore" Then

                If Not (TypeOf childCtrl Is Label) Then
                    xmlSerialisedForm.WriteStartElement("Control")
                    xmlSerialisedForm.WriteAttributeString("Type", childCtrl.[GetType]().ToString())
                    xmlSerialisedForm.WriteAttributeString("Name", childCtrl.Name)

                    If TypeOf childCtrl Is TextBox Then
                        xmlSerialisedForm.WriteElementString("Text", (CType(childCtrl, TextBox)).Text)

                    ElseIf TypeOf childCtrl Is ComboBox Then
                        xmlSerialisedForm.WriteElementString("Text", (CType(childCtrl, ComboBox)).Text)
                        xmlSerialisedForm.WriteElementString("SelectedIndex", (CType(childCtrl, ComboBox)).SelectedIndex.ToString())
                    ElseIf TypeOf childCtrl Is ListBox Then
                        Dim lst As ListBox = CType(childCtrl, ListBox)

                        If lst.SelectedIndex = -1 Then
                            xmlSerialisedForm.WriteElementString("SelectedIndex", "-1")
                        Else

                            For i As Integer = 0 To lst.SelectedIndices.Count - 1
                                xmlSerialisedForm.WriteElementString("SelectedIndex", (lst.SelectedIndices(i).ToString()))
                            Next
                        End If
                    ElseIf TypeOf childCtrl Is CheckBox Then
                        xmlSerialisedForm.WriteElementString("Checked", (CType(childCtrl, CheckBox)).Checked.ToString())
                    ElseIf TypeOf childCtrl Is NumericUpDown Then
                        xmlSerialisedForm.WriteElementString("Value", (CType(childCtrl, NumericUpDown)).Value.ToString())
                    ElseIf TypeOf childCtrl Is RadioButton Then
                        xmlSerialisedForm.WriteElementString("Checked", (CType(childCtrl, RadioButton)).Checked.ToString())
                    End If

                    Dim visible As Boolean = CBool(GetType(Control).GetMethod("GetState", BindingFlags.Instance Or BindingFlags.NonPublic).Invoke(childCtrl, New Object() {2}))
                    xmlSerialisedForm.WriteElementString("Visible", visible.ToString())

                    If childCtrl.HasChildren Then
                        If Not TypeOf childCtrl Is NumericUpDown Then
                            If TypeOf childCtrl Is SplitContainer Then
                                AddChildControls(xmlSerialisedForm, (CType(childCtrl, SplitContainer)).Panel1)
                                AddChildControls(xmlSerialisedForm, (CType(childCtrl, SplitContainer)).Panel2)
                            Else
                                AddChildControls(xmlSerialisedForm, childCtrl)
                            End If
                        End If
                    End If

                    xmlSerialisedForm.WriteEndElement()
                End If
            End If

        Next
    End Sub

    Sub Deserialise(ByVal c As Control, ByVal XmlFileName As String)
        If File.Exists(XmlFileName) Then
            Dim xmlSerialisedForm As XmlDocument = New XmlDocument()
            xmlSerialisedForm.Load(XmlFileName)
            Dim topLevel As XmlNode = xmlSerialisedForm.ChildNodes(1)

            For Each n As XmlNode In topLevel.ChildNodes

                Try
                    SetControlProperties(CType(c, Control), n)
                Catch
                End Try
            Next
        End If
    End Sub

    Private Sub SetControlProperties(ByVal currentCtrl As Control, ByVal n As XmlNode)
        Dim controlName As String = n.Attributes("Name").Value
        Dim controlType As String = n.Attributes("Type").Value
        Dim ctrl As Control() = currentCtrl.Controls.Find(controlName, True)

        If ctrl.Length = 0 Then
            MessageBox.Show("can't find control")
        Else
            Dim ctrlToSet As Control = GetImmediateChildControl(ctrl, currentCtrl)

            If ctrlToSet IsNot Nothing Then

                If ctrlToSet.[GetType]().ToString() = controlType Then

                    Select Case controlType
                        Case "System.Windows.Forms.TextBox"
                            CType(ctrlToSet, System.Windows.Forms.TextBox).Text = n("Text").InnerText
                        Case "System.Windows.Forms.ComboBox"
                            CType(ctrlToSet, System.Windows.Forms.ComboBox).Text = n("Text").InnerText
                        Case "System.Windows.Forms.ListBox"
                            Dim lst As ListBox = CType(ctrlToSet, ListBox)
                            Dim xnlSelectedIndex As XmlNodeList = n.SelectNodes("SelectedIndex")

                            For i As Integer = 0 To xnlSelectedIndex.Count - 1
                                lst.SelectedIndex = Convert.ToInt32(xnlSelectedIndex(i).InnerText)
                            Next

                        Case "System.Windows.Forms.CheckBox"
                            CType(ctrlToSet, System.Windows.Forms.CheckBox).Checked = Convert.ToBoolean(n("Checked").InnerText)
                        Case "System.Windows.Forms.NumericUpDown"
                            CType(ctrlToSet, System.Windows.Forms.NumericUpDown).Value = Convert.ToDecimal(n("Value").InnerText)
                        Case "System.Windows.Forms.RadioButton"
                            CType(ctrlToSet, System.Windows.Forms.RadioButton).Checked = Convert.ToBoolean(n("Checked").InnerText)
                    End Select

                    ctrlToSet.Visible = Convert.ToBoolean(n("Visible").InnerText)

                    If n.HasChildNodes AndAlso ctrlToSet.HasChildren Then
                        Dim xnlControls As XmlNodeList = n.SelectNodes("Control")

                        For Each n2 As XmlNode In xnlControls
                            SetControlProperties(ctrlToSet, n2)
                        Next
                    End If
                Else
                    MessageBox.Show("wrong type")
                End If
            Else
                MessageBox.Show("can't find")
            End If
        End If
    End Sub

    Private Function GetImmediateChildControl(ByVal ctrl As Control(), ByVal currentCtrl As Control) As Control
        Dim c As Control = Nothing

        For i As Integer = 0 To ctrl.Length - 1

            If (ctrl(i).Parent.Name = currentCtrl.Name) OrElse (TypeOf currentCtrl Is SplitContainer AndAlso ctrl(i).Parent.Parent.Name = currentCtrl.Name) Then
                c = ctrl(i)
                Exit For
            End If
        Next

        Return c
    End Function
End Class