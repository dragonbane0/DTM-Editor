Imports System.Text

Public Class Main

    Dim DTMPath As String
    Dim ZE_Version As Integer
    Dim Loaded As Boolean = False
    Dim Header As DTMHeader_Version3
    Dim Inputs As New List(Of ControllerState_Version3)
    Dim CurrentFrame As UInt64 = 1
    Dim IgnoreChanges As Boolean = True


    Private Sub Button_Browse_Click(sender As Object, e As EventArgs) Handles Button_Browse.Click

        If (OpenFileDialog1.ShowDialog() = DialogResult.OK) Then

            TextBox_DTMPath.Text = OpenFileDialog1.FileName

        End If

    End Sub

    Private Sub Button_Load_Click(sender As Object, e As EventArgs) Handles Button_Load.Click

        DTMPath = TextBox_DTMPath.Text
        ZE_Version = TextBox_ZEVersion.Text

        If (ZE_Version < 1 Or ZE_Version > 3) Then

            MsgBox("Invalid ZE Version!")

            Exit Sub

        End If

        If DTMPath = Nothing Or IsNumeric(DTMPath) Then

            MsgBox("Invalid Path!")

            Exit Sub

        End If

        Dim FileInfo As New IO.FileInfo(DTMPath)

        If (FileInfo.Exists = False) Then

            MsgBox("Invalid Path to file!")

            Exit Sub

        End If

        Inputs.Clear()

        Dim FileContent As Byte()

        FileContent = My.Computer.FileSystem.ReadAllBytes(DTMPath)

        ReadHeader(FileContent)
        ReadInputs(FileContent)

        Loaded = True

        TextBox_GoToFrame.Text = "1"

    End Sub

    Private Sub Button_Save_Click(sender As Object, e As EventArgs) Handles Button_Save.Click

        DTMPath = TextBox_DTMPath.Text
        ZE_Version = TextBox_ZEVersion.Text

        If (Loaded = False) Then

            MsgBox("No DTM loaded!")
            Exit Sub

        End If

        If (ZE_Version < 1 Or ZE_Version > 3) Then

            MsgBox("Invalid ZE Version!")
            Exit Sub

        End If

        If DTMPath = Nothing Or IsNumeric(DTMPath) Then

            MsgBox("Invalid Path!")
            Exit Sub

        End If

        Dim FileInfo As New IO.FileInfo(DTMPath)

        If FileInfo.Directory.Parent.Exists = False Then

            MsgBox("Invalid Path to file!")
            Exit Sub

        End If

        If FileInfo.Directory.Exists = False Then

            FileInfo.Directory.Create()

        End If

        Dim Size As UInt32 = 256 + (Header.inputCount * GetBytesPerInputFrame())

        Dim OutputFile(Size - 1) As Byte

        WriteHeader(OutputFile)
        WriteInputs(OutputFile)

        My.Computer.FileSystem.WriteAllBytes(DTMPath, OutputFile, False)

    End Sub

    'Helper Read Functions
    Private Function Read8(Data() As Byte, Offset As Integer) As Byte

        Return Buffer.GetByte(Data, Offset)

    End Function

    Private Function Read16(Data() As Byte, Offset As Integer) As UInt16

        Dim Output(1) As Byte

        Output(1) = Buffer.GetByte(Data, Offset + 1)
        Output(0) = Buffer.GetByte(Data, Offset) << 8

        Return BitConverter.ToUInt16(Output, 0)

    End Function

    Private Function Read32(Data() As Byte, Offset As Integer) As UInt32

        Dim Output(3) As Byte

        Output(3) = Buffer.GetByte(Data, Offset + 3)
        Output(2) = Buffer.GetByte(Data, Offset + 2) << 8
        Output(1) = Buffer.GetByte(Data, Offset + 1) << 16
        Output(0) = Buffer.GetByte(Data, Offset) << 24

        Return BitConverter.ToUInt32(Output, 0)

    End Function

    Private Function Read64(Data() As Byte, Offset As Integer) As UInt64

        Dim Output(7) As Byte

        Output(7) = Buffer.GetByte(Data, Offset + 7)
        Output(6) = Buffer.GetByte(Data, Offset + 6) << 8
        Output(5) = Buffer.GetByte(Data, Offset + 5) << 16
        Output(4) = Buffer.GetByte(Data, Offset + 4) << 24
        Output(3) = Buffer.GetByte(Data, Offset + 3) << 32
        Output(2) = Buffer.GetByte(Data, Offset + 2) << 40
        Output(1) = Buffer.GetByte(Data, Offset + 1) << 48
        Output(0) = Buffer.GetByte(Data, Offset) << 56

        Return BitConverter.ToUInt64(Output, 0)

    End Function

    Private Function ReadFloat(Data() As Byte, Offset As Integer) As Single

        Dim Output(3) As Byte

        Output(3) = Buffer.GetByte(Data, Offset + 3)
        Output(2) = Buffer.GetByte(Data, Offset + 2) << 8
        Output(1) = Buffer.GetByte(Data, Offset + 1) << 16
        Output(0) = Buffer.GetByte(Data, Offset) << 24

        Return BitConverter.ToSingle(Output, 0)

    End Function

    Private Function ReadString(Data() As Byte, Offset As Integer, Length As Integer) As String

        Dim output As String = ""
        Dim i As Integer = 0

        While (i < Length)

            Dim address As Integer = Offset + i
            Dim result As Char = ""

            Dim var As Byte = Data(address)

            If var = 0 Then

                Exit While

            End If

            result = ChrW(var)

            output = output & result

            i = i + 1

        End While

        Return output

    End Function

    Function ReadStringFull(Data() As Byte, Offset As Integer, dataSize As UInt32) As String

        If (Convert.ToUInt32(Offset) >= dataSize) Then

            Return ""

        End If

        Dim startOffset As Integer
        Dim Length As Integer = 0

        While (Data(Offset) = 0)

            Offset = Offset + 1

        End While

        startOffset = Offset

        While (Data(Offset) <> 0)

            Offset = Offset + 1
            Length = Length + 1

        End While

        Return ReadString(Data, startOffset, Length)

    End Function

    'Helper Write Functions
    Private Sub Write8(Data() As Byte, Offset As Integer, Value As Byte)

        Data(Offset) = Value

    End Sub

    Private Sub Write16(Data() As Byte, Offset As Integer, Value As UInt16)

        Dim BitStream As Byte() = BitConverter.GetBytes(Value)

        For i As Integer = 0 To BitStream.Length - 1

            'Write8(Data, Offset + i, BitStream((BitStream.Length - 1) - i))
            Write8(Data, Offset + i, BitStream(i))

        Next

    End Sub

    Private Sub Write32(Data() As Byte, Offset As Integer, Value As UInt32)

        Dim BitStream As Byte() = BitConverter.GetBytes(Value)

        For i As Integer = 0 To BitStream.Length - 1

            'Write8(Data, Offset + i, BitStream((BitStream.Length - 1) - i))
            Write8(Data, Offset + i, BitStream(i))

        Next

    End Sub

    Private Sub Write64(Data() As Byte, Offset As Integer, Value As UInt64)

        Dim BitStream As Byte() = BitConverter.GetBytes(Value)

        For i As Integer = 0 To BitStream.Length - 1

            'Write8(Data, Offset + i, BitStream((BitStream.Length - 1) - i))
            Write8(Data, Offset + i, BitStream(i))

        Next

    End Sub

    Private Sub WriteFloat(Data() As Byte, Offset As Integer, Value As Single)

        Dim ValueBytes As Byte() = BitConverter.GetBytes(Value)

        Write32(Data, Offset, BitConverter.ToUInt32(ValueBytes, 0))

    End Sub

    Private Sub WriteString(Data() As Byte, Offset As Integer, Str As String)

        WriteStringFull(Data, Offset, Str, Str.Length)

    End Sub

    Private Sub WriteStringFull(Data() As Byte, Offset As Integer, Str As String, Length As Integer)

        For i As Integer = 0 To Length - 1

            Data(Offset + i) = Convert.ToByte(Str(i))

        Next

    End Sub

    Public Sub ReadHeader(FileContent() As Byte)

        Dim DTMHead As New DTMHeader_Version3

        If (ZE_Version = 3) Then

            DTMHead.numGBAs = Read8(FileContent, 154)
            DTMHead.bSyncGPU = Read8(FileContent, 155)
            DTMHead.bNetPlay = Read8(FileContent, 156)

            Buffer.BlockCopy(FileContent, 157, DTMHead.reserved, 0, 12)

        Else

            DTMHead.numGBAs = 0
            DTMHead.bSyncGPU = Read8(FileContent, 154)
            DTMHead.bNetPlay = Read8(FileContent, 155)

            Buffer.BlockCopy(FileContent, 156, DTMHead.reserved, 0, 13)

        End If

        'All Versions
        Buffer.BlockCopy(FileContent, 0, DTMHead.filetype, 0, 4)
        Buffer.BlockCopy(FileContent, 4, DTMHead.gameID, 0, 6)

        DTMHead.bWii = Read8(FileContent, 10)
        DTMHead.numControllers = Read8(FileContent, 11)
        DTMHead.bFromSaveState = Read8(FileContent, 12)
        DTMHead.frameCount = Read64(FileContent, 13)
        DTMHead.inputCount = Read64(FileContent, 21)
        DTMHead.lagCount = Read64(FileContent, 29)
        DTMHead.uniqueID = Read64(FileContent, 37)
        DTMHead.numRerecords = Read32(FileContent, 45)

        Buffer.BlockCopy(FileContent, 49, DTMHead.author, 0, 32)
        Buffer.BlockCopy(FileContent, 81, DTMHead.videoBackend, 0, 16)
        Buffer.BlockCopy(FileContent, 97, DTMHead.audioEmulator, 0, 16)
        Buffer.BlockCopy(FileContent, 113, DTMHead.md5, 0, 16)

        DTMHead.recordingStartTime = Read64(FileContent, 129)
        DTMHead.bSaveConfig = Read8(FileContent, 137)
        DTMHead.bSkipIdle = Read8(FileContent, 138)
        DTMHead.bDualCore = Read8(FileContent, 139)
        DTMHead.bProgressive = Read8(FileContent, 140)
        DTMHead.bDSPHLE = Read8(FileContent, 141)
        DTMHead.bFastDiscSpeed = Read8(FileContent, 142)
        DTMHead.CPUCore = Read8(FileContent, 143)
        DTMHead.bEFBAccessEnable = Read8(FileContent, 144)
        DTMHead.bEFBCopyEnable = Read8(FileContent, 145)
        DTMHead.bCopyEFBToTexture = Read8(FileContent, 146)
        DTMHead.bEFBCopyCacheEnable = Read8(FileContent, 147)
        DTMHead.bEFBEmulateFormatChanges = Read8(FileContent, 148)
        DTMHead.bUseXFB = Read8(FileContent, 149)
        DTMHead.bUseRealXFB = Read8(FileContent, 150)
        DTMHead.memcards = Read8(FileContent, 151)
        DTMHead.bClearSave = Read8(FileContent, 152)
        DTMHead.bongos = Read8(FileContent, 153)

        Buffer.BlockCopy(FileContent, 169, DTMHead.discChange, 0, 40)
        Buffer.BlockCopy(FileContent, 209, DTMHead.revision, 0, 20)

        DTMHead.DSPiromHash = Read32(FileContent, 229)
        DTMHead.DSPcoefHash = Read32(FileContent, 233)
        DTMHead.tickCount = Read64(FileContent, 237)

        Buffer.BlockCopy(FileContent, 245, DTMHead.reserved2, 0, 11)

        Header = DTMHead

        'Apply Data

        'First Row
        TextBox_GameID.Text = System.Text.Encoding.ASCII.GetString(Header.gameID)
        TextBox_author.Text = System.Text.Encoding.UTF8.GetString(Header.author)
        TextBox_recodingstarttime.Text = Header.recordingStartTime
        TextBox_discChange.Text = System.Text.Encoding.ASCII.GetString(Header.discChange)
        TextBox_md5.Text = BitConverter.ToString(Header.md5)
        TextBox_revision.Text = BitConverter.ToString(Header.revision)


        'Second Row
        TextBox_framecount.Text = Header.frameCount
        TextBox_inputcount.Text = Header.inputCount
        TextBox_lagCount.Text = Header.lagCount
        TextBox_tickCount.Text = Header.tickCount
        TextBox_rerecords.Text = Header.numRerecords

        'Third Row
        TextBox_videoBackend.Text = System.Text.Encoding.UTF8.GetString(Header.videoBackend)
        TextBox_audioEmulator.Text = System.Text.Encoding.UTF8.GetString(Header.audioEmulator)
        TextBox_CPUCore.Text = Header.CPUCore
        TextBox_DSPiromHash.Text = Header.DSPiromHash
        TextBox_DSPcoefHash.Text = Header.DSPcoefHash

        'Fourth Row
        TextBox_numControllers.Text = Header.numControllers
        TextBox_numGBAs.Text = Header.numGBAs
        TextBox_bongos.Text = Header.bongos
        TextBox_memcards.Text = Header.memcards


        'Checkboxes
        CheckBox_Wii.Checked = Header.bWii
        CheckBox_NetPlay.Checked = Header.bNetPlay
        CheckBox_fromState.Checked = Header.bFromSaveState
        CheckBox_SaveConfig.Checked = Header.bSaveConfig

        CheckBox_dualCore.Checked = Header.bDualCore
        CheckBox_skipIdle.Checked = Header.bSkipIdle
        CheckBox_HLE.Checked = Header.bDSPHLE
        CheckBox_progressive.Checked = Header.bProgressive
        CheckBox_fastDiscSpeed.Checked = Header.bFastDiscSpeed

        CheckBox_EFBAccess.Checked = Header.bEFBAccessEnable
        CheckBox_EFBCopy.Checked = Header.bEFBCopyEnable
        CheckBox_EFBToText.Checked = Header.bCopyEFBToTexture
        CheckBox_EFBCopyCache.Checked = Header.bEFBCopyCacheEnable
        CheckBox_EFBEmuFormat.Checked = Header.bEFBEmulateFormatChanges

        CheckBox_UseXFB.Checked = Header.bUseXFB
        CheckBox_RealXFB.Checked = Header.bUseRealXFB
        CheckBox_ClearSave.Checked = Header.bClearSave
        CheckBox_SyncGPU.Checked = Header.bSyncGPU

    End Sub

    Public Sub WriteHeader(FileContent() As Byte)

        'Grab Data

        'First Row
        Header.gameID = System.Text.Encoding.ASCII.GetBytes(TextBox_GameID.Text)

        Dim author As Byte() = System.Text.Encoding.UTF8.GetBytes(TextBox_author.Text)
        Buffer.BlockCopy(author, 0, Header.author, 0, author.Length)

        Header.recordingStartTime = TextBox_recodingstarttime.Text

        Dim discChange As Byte() = System.Text.Encoding.ASCII.GetBytes(TextBox_discChange.Text)
        Buffer.BlockCopy(discChange, 0, Header.discChange, 0, discChange.Length)

        Dim md5 As Byte() = ConvertHexStringToByteArray(TextBox_md5.Text)
        Buffer.BlockCopy(md5, 0, Header.md5, 0, md5.Length)

        Dim revision As Byte() = ConvertHexStringToByteArray(TextBox_revision.Text)
        Buffer.BlockCopy(revision, 0, Header.revision, 0, revision.Length)

        'Second Row
        Header.frameCount = TextBox_framecount.Text
        Header.inputCount = TextBox_inputcount.Text
        Header.lagCount = TextBox_lagCount.Text
        Header.tickCount = TextBox_tickCount.Text
        Header.numRerecords = TextBox_rerecords.Text

        'Third Row
        Dim videoBackend As Byte() = System.Text.Encoding.UTF8.GetBytes(TextBox_videoBackend.Text)
        Buffer.BlockCopy(videoBackend, 0, Header.videoBackend, 0, videoBackend.Length)

        Dim audioEmulator As Byte() = System.Text.Encoding.UTF8.GetBytes(TextBox_audioEmulator.Text)
        Buffer.BlockCopy(audioEmulator, 0, Header.audioEmulator, 0, audioEmulator.Length)

        Header.CPUCore = TextBox_CPUCore.Text
        Header.DSPiromHash = TextBox_DSPiromHash.Text
        Header.DSPcoefHash = TextBox_DSPcoefHash.Text

        'Fourth Row
        Header.numControllers = TextBox_numControllers.Text
        Header.numGBAs = TextBox_numGBAs.Text
        Header.bongos = TextBox_bongos.Text
        Header.memcards = TextBox_memcards.Text

        'Checkboxes
        Header.bWii = CheckBox_Wii.Checked
        Header.bNetPlay = CheckBox_NetPlay.Checked
        Header.bFromSaveState = CheckBox_fromState.Checked
        Header.bSaveConfig = CheckBox_SaveConfig.Checked

        Header.bDualCore = CheckBox_dualCore.Checked
        Header.bSkipIdle = CheckBox_skipIdle.Checked
        Header.bDSPHLE = CheckBox_HLE.Checked
        Header.bProgressive = CheckBox_progressive.Checked
        Header.bFastDiscSpeed = CheckBox_fastDiscSpeed.Checked

        Header.bEFBAccessEnable = CheckBox_EFBAccess.Checked
        Header.bEFBCopyEnable = CheckBox_EFBCopy.Checked
        Header.bCopyEFBToTexture = CheckBox_EFBToText.Checked
        Header.bEFBCopyCacheEnable = CheckBox_EFBCopyCache.Checked
        Header.bEFBEmulateFormatChanges = CheckBox_EFBEmuFormat.Checked

        Header.bUseXFB = CheckBox_UseXFB.Checked
        Header.bUseRealXFB = CheckBox_RealXFB.Checked
        Header.bClearSave = CheckBox_ClearSave.Checked
        Header.bSyncGPU = CheckBox_SyncGPU.Checked


        If (ZE_Version = 3) Then

            Write8(FileContent, 154, Header.numGBAs)
            Write8(FileContent, 155, BooleanToByte(Header.bSyncGPU))
            Write8(FileContent, 156, BooleanToByte(Header.bNetPlay))

            Buffer.BlockCopy(Header.reserved, 0, FileContent, 157, 12)

        Else

            Write8(FileContent, 154, BooleanToByte(Header.bSyncGPU))
            Write8(FileContent, 155, BooleanToByte(Header.bNetPlay))

            Buffer.BlockCopy(Header.reserved, 0, FileContent, 156, 13)

        End If

        Buffer.BlockCopy(Header.filetype, 0, FileContent, 0, 4)
        Buffer.BlockCopy(Header.gameID, 0, FileContent, 4, 6)

        Write8(FileContent, 10, BooleanToByte(Header.bWii))
        Write8(FileContent, 11, Header.numControllers)
        Write8(FileContent, 12, BooleanToByte(Header.bFromSaveState))

        Write64(FileContent, 13, Header.frameCount)
        Write64(FileContent, 21, Header.inputCount)
        Write64(FileContent, 29, Header.lagCount)
        Write64(FileContent, 37, Header.uniqueID)
        Write32(FileContent, 45, Header.numRerecords)

        Buffer.BlockCopy(Header.author, 0, FileContent, 49, 32)
        Buffer.BlockCopy(Header.videoBackend, 0, FileContent, 81, 16)
        Buffer.BlockCopy(Header.audioEmulator, 0, FileContent, 97, 16)
        Buffer.BlockCopy(Header.md5, 0, FileContent, 113, 16)

        Write64(FileContent, 129, Header.recordingStartTime)
        Write8(FileContent, 137, BooleanToByte(Header.bSaveConfig))
        Write8(FileContent, 138, BooleanToByte(Header.bSkipIdle))
        Write8(FileContent, 139, BooleanToByte(Header.bDualCore))
        Write8(FileContent, 140, BooleanToByte(Header.bProgressive))
        Write8(FileContent, 141, BooleanToByte(Header.bDSPHLE))
        Write8(FileContent, 142, BooleanToByte(Header.bFastDiscSpeed))
        Write8(FileContent, 143, Header.CPUCore)

        Write8(FileContent, 144, BooleanToByte(Header.bEFBAccessEnable))
        Write8(FileContent, 145, BooleanToByte(Header.bEFBCopyEnable))
        Write8(FileContent, 146, Header.bCopyEFBToTexture)
        Write8(FileContent, 147, BooleanToByte(Header.bEFBCopyCacheEnable))
        Write8(FileContent, 148, BooleanToByte(Header.bEFBEmulateFormatChanges))
        Write8(FileContent, 149, BooleanToByte(Header.bUseXFB))
        Write8(FileContent, 150, BooleanToByte(Header.bUseRealXFB))
        Write8(FileContent, 151, Header.memcards)
        Write8(FileContent, 152, BooleanToByte(Header.bClearSave))
        Write8(FileContent, 153, Header.bongos)

        Buffer.BlockCopy(Header.discChange, 0, FileContent, 169, 40)
        Buffer.BlockCopy(Header.revision, 0, FileContent, 209, 20)

        Write32(FileContent, 229, Header.DSPiromHash)
        Write32(FileContent, 233, Header.DSPcoefHash)
        Write64(FileContent, 237, Header.tickCount)

        Buffer.BlockCopy(Header.reserved2, 0, FileContent, 245, 11)

    End Sub

    Public Sub ReadInputs(FileContent() As Byte)

        Dim BytesPerInputFrame As Integer = GetBytesPerInputFrame()

        For i As UInt64 = 0 To FileContent.Length - 256 - BytesPerInputFrame Step BytesPerInputFrame

            Dim Input As New ControllerState_Version3

            Dim Buttons1 As Byte = Read8(FileContent, 256 + i)
            Dim Buttons2 As Byte = Read8(FileContent, 256 + i + 1)

            'Buttons 1
            If (Buttons1 > 127) Then

                Buttons1 = Buttons1 - 128
                Input.DPadDown = True

            End If

            If (Buttons1 > 63) Then

                Buttons1 = Buttons1 - 64
                Input.DPadUp = True

            End If

            If (Buttons1 > 31) Then

                Buttons1 = Buttons1 - 32
                Input.Z = True

            End If

            If (Buttons1 > 15) Then

                Buttons1 = Buttons1 - 16
                Input.Y = True

            End If

            If (Buttons1 > 7) Then

                Buttons1 = Buttons1 - 8
                Input.X = True

            End If

            If (Buttons1 > 3) Then

                Buttons1 = Buttons1 - 4
                Input.B = True

            End If

            If (Buttons1 > 1) Then

                Buttons1 = Buttons1 - 2
                Input.A = True

            End If

            If (Buttons1 > 0) Then

                Buttons1 = Buttons1 - 1
                Input.Start = True

            End If


            'Buttons 2
            If (ZE_Version > 1) Then

                If (Buttons2 > 63) Then

                    Buttons2 = Buttons2 - 64
                    Input.loading = True

                End If

            End If

            If (Buttons2 > 31) Then

                Buttons2 = Buttons2 - 32
                Input.reset = True

            End If

            If (Buttons2 > 15) Then

                Buttons2 = Buttons2 - 16
                Input.disc = True

            End If

            If (Buttons2 > 7) Then

                Buttons2 = Buttons2 - 8
                Input.R = True

            End If

            If (Buttons2 > 3) Then

                Buttons2 = Buttons2 - 4
                Input.L = True

            End If

            If (Buttons2 > 1) Then

                Buttons2 = Buttons2 - 2
                Input.DPadRight = True

            End If

            If (Buttons2 > 0) Then

                Buttons2 = Buttons2 - 1
                Input.DPadLeft = True

            End If

            Input.TriggerL = Read8(FileContent, 256 + i + 2)
            Input.TriggerR = Read8(FileContent, 256 + i + 3)

            Input.AnalogStickX = Read8(FileContent, 256 + i + 4)
            Input.AnalogStickY = Read8(FileContent, 256 + i + 5)

            Input.CStickX = Read8(FileContent, 256 + i + 6)
            Input.CStickY = Read8(FileContent, 256 + i + 7)

            If (ZE_Version > 2) Then

                Input.tunerEvent = Read8(FileContent, 256 + i + 8)
                Input.LinkX = ReadFloat(FileContent, 256 + i + 9)
                Input.LinkZ = ReadFloat(FileContent, 256 + i + 13)

            ElseIf (ZE_Version > 1) Then

                Input.LinkX = ReadFloat(FileContent, 256 + i + 8)
                Input.LinkZ = ReadFloat(FileContent, 256 + i + 12)

            End If

            Inputs.Add(Input)

        Next


    End Sub

    Public Sub FillInputForm()

        Dim Input As ControllerState_Version3 = Inputs.Item(CurrentFrame - 1)

        TextBox_StickX.Text = Input.AnalogStickX
        TextBox_StickY.Text = Input.AnalogStickY

        TextBox_CStickX.Text = Input.CStickX
        TextBox_CStickY.Text = Input.CStickY

        TextBox_TriggerLeft.Text = Input.TriggerL
        TextBox_TriggerRight.Text = Input.TriggerR

        TextBox_tunerEvent.Text = Input.tunerEvent
        TextBox_LinkX.Text = Input.LinkX
        TextBox_LinkZ.Text = Input.LinkZ

        CheckBox_A.Checked = Input.A
        CheckBox_B.Checked = Input.B
        CheckBox_X.Checked = Input.X
        CheckBox_Y.Checked = Input.Y
        CheckBox_Z.Checked = Input.Z
        CheckBox_Start.Checked = Input.Start
        CheckBox_L.Checked = Input.L
        CheckBox_R.Checked = Input.R

        Checkbox_DU.Checked = Input.DPadUp
        CheckBox_DD.Checked = Input.DPadDown
        CheckBox_DL.Checked = Input.DPadLeft
        CheckBox_DR.Checked = Input.DPadRight

        CheckBox_reset.Checked = Input.reset
        CheckBox_loading.Checked = Input.loading
        CheckBox_disc.Checked = Input.disc

    End Sub

    Public Sub WriteInputs(FileContent() As Byte)

        Dim BytesPerInputFrame As Integer = GetBytesPerInputFrame()
        Dim Position As UInt64 = 256

        For Each Input As ControllerState_Version3 In Inputs

            Dim Buttons1 As Byte = 0
            Dim Buttons2 As Byte = 0

            'Buttons 1
            If (Input.DPadDown = True) Then

                Buttons1 = Buttons1 + 128

            End If

            If (Input.DPadUp = True) Then

                Buttons1 = Buttons1 + 64

            End If

            If (Input.Z = True) Then

                Buttons1 = Buttons1 + 32

            End If

            If (Input.Y = True) Then

                Buttons1 = Buttons1 + 16

            End If

            If (Input.X = True) Then

                Buttons1 = Buttons1 + 8

            End If

            If (Input.B = True) Then

                Buttons1 = Buttons1 + 4

            End If

            If (Input.A = True) Then

                Buttons1 = Buttons1 + 2

            End If

            If (Input.Start = True) Then

                Buttons1 = Buttons1 + 1

            End If


            'Buttons 2
            If (ZE_Version > 1) Then

                If (Input.loading = True) Then

                    Buttons2 = Buttons2 + 64

                End If

            End If

            If (Input.reset = True) Then

                Buttons2 = Buttons2 + 32

            End If

            If (Input.disc = True) Then

                Buttons2 = Buttons2 + 16

            End If

            If (Input.R = True) Then

                Buttons2 = Buttons2 + 8

            End If

            If (Input.L = True) Then

                Buttons2 = Buttons2 + 4

            End If

            If (Input.DPadRight = True) Then

                Buttons2 = Buttons2 + 2

            End If

            If (Input.DPadLeft = True) Then

                Buttons2 = Buttons2 + 1

            End If

            Write8(FileContent, Position, Buttons1)
            Write8(FileContent, Position + 1, Buttons2)

            Write8(FileContent, Position + 2, Input.TriggerL)
            Write8(FileContent, Position + 3, Input.TriggerR)

            Write8(FileContent, Position + 4, Input.AnalogStickX)
            Write8(FileContent, Position + 5, Input.AnalogStickY)

            Write8(FileContent, Position + 6, Input.CStickX)
            Write8(FileContent, Position + 7, Input.CStickY)

            If (ZE_Version > 2) Then

                Write8(FileContent, Position + 8, Input.tunerEvent)
                WriteFloat(FileContent, Position + 9, Input.LinkX)
                WriteFloat(FileContent, Position + 13, Input.LinkZ)

            ElseIf (ZE_Version > 1) Then

                WriteFloat(FileContent, Position + 8, Input.LinkX)
                WriteFloat(FileContent, Position + 12, Input.LinkZ)

            End If

            Position = Position + BytesPerInputFrame

        Next

    End Sub

    Public Function GetBytesPerInputFrame()

        Dim BytesPerInputFrame As Integer

        If (ZE_Version = 1) Then

            BytesPerInputFrame = 8

        ElseIf (ZE_Version = 2) Then

            BytesPerInputFrame = 16

        ElseIf (ZE_Version = 3) Then

            BytesPerInputFrame = 17

        End If

        Return BytesPerInputFrame

    End Function


    Public Function BooleanToByte(bol As Boolean) As Byte

        If (bol = True) Then

            Return 1

        Else

            Return 0

        End If

    End Function

    Public Function ConvertHexStringToByteArray(input As String) As Byte()

        Dim splitStrings As String() = input.Split("-")
        Dim outputArray(splitStrings.Length - 1) As Byte

        For i As Integer = 0 To splitStrings.Length - 1

            outputArray(i) = Convert.ToByte(splitStrings(i), 16)

        Next

        Return outputArray

    End Function

    Private Sub TextBox_DTMPath_DragEnter(ByVal sender As Object, ByVal e As System.Windows.Forms.DragEventArgs) Handles TextBox_DTMPath.DragEnter

        If e.Data.GetDataPresent(DataFormats.FileDrop) Then

            e.Effect = DragDropEffects.All

        End If

    End Sub

    Private Sub TextBox_DTMPath_DragDrop(ByVal sender As Object, ByVal e As System.Windows.Forms.DragEventArgs) Handles TextBox_DTMPath.DragDrop

        If e.Data.GetDataPresent(DataFormats.FileDrop) Then

            Dim DroppedFiles() As String = e.Data.GetData(DataFormats.FileDrop)

            TextBox_DTMPath.Text = DroppedFiles(0)

        End If

    End Sub

    Private Sub TextBox_GoToFrame_TextChanged(sender As Object, e As EventArgs) Handles TextBox_GoToFrame.TextChanged

        If (Loaded = True And IsNumeric(TextBox_GoToFrame.Text)) Then

            If (TextBox_GoToFrame.Text > 0 And TextBox_GoToFrame.Text <= Header.inputCount) Then

                CurrentFrame = TextBox_GoToFrame.Text

                IgnoreChanges = True
                FillInputForm()
                IgnoreChanges = False

            Else

                IgnoreChanges = True

            End If

        Else

            IgnoreChanges = True

        End If

    End Sub

    Private Sub TextBox_StickX_TextChanged(sender As Object, e As EventArgs) Handles TextBox_StickX.TextChanged

        If (IgnoreChanges = False And IsNumeric(TextBox_StickX.Text)) Then

            If (TextBox_StickX.Text >= 0 And TextBox_StickX.Text <= 255) Then

                Inputs.Item(CurrentFrame - 1).AnalogStickX = TextBox_StickX.Text

            End If

        End If

    End Sub

    Private Sub TextBox_StickY_TextChanged(sender As Object, e As EventArgs) Handles TextBox_StickY.TextChanged

        If (IgnoreChanges = False And IsNumeric(TextBox_StickY.Text)) Then

            If (TextBox_StickY.Text >= 0 And TextBox_StickY.Text <= 255) Then

                Inputs.Item(CurrentFrame - 1).AnalogStickY = TextBox_StickY.Text

            End If

        End If

    End Sub

    Private Sub TextBox_CStickX_TextChanged(sender As Object, e As EventArgs) Handles TextBox_CStickX.TextChanged

        If (IgnoreChanges = False And IsNumeric(TextBox_CStickX.Text)) Then

            If (TextBox_CStickX.Text >= 0 And TextBox_CStickX.Text <= 255) Then

                Inputs.Item(CurrentFrame - 1).CStickX = TextBox_CStickX.Text

            End If

        End If

    End Sub

    Private Sub TextBox_CStickY_TextChanged(sender As Object, e As EventArgs) Handles TextBox_CStickY.TextChanged

        If (IgnoreChanges = False And IsNumeric(TextBox_CStickY.Text)) Then

            If (TextBox_CStickY.Text >= 0 And TextBox_CStickY.Text <= 255) Then

                Inputs.Item(CurrentFrame - 1).CStickY = TextBox_CStickY.Text

            End If

        End If

    End Sub

    Private Sub TextBox_TriggerLeft_TextChanged(sender As Object, e As EventArgs) Handles TextBox_TriggerLeft.TextChanged

        If (IgnoreChanges = False And IsNumeric(TextBox_TriggerLeft.Text)) Then

            If (TextBox_TriggerLeft.Text >= 0 And TextBox_TriggerLeft.Text <= 255) Then

                Inputs.Item(CurrentFrame - 1).TriggerL = TextBox_TriggerLeft.Text

            End If

        End If

    End Sub

    Private Sub TextBox_TriggerRight_TextChanged(sender As Object, e As EventArgs) Handles TextBox_TriggerRight.TextChanged

        If (IgnoreChanges = False And IsNumeric(TextBox_TriggerRight.Text)) Then

            If (TextBox_TriggerRight.Text >= 0 And TextBox_TriggerRight.Text <= 255) Then

                Inputs.Item(CurrentFrame - 1).TriggerR = TextBox_TriggerRight.Text

            End If

        End If

    End Sub

    Private Sub TextBox_tunerEvent_TextChanged(sender As Object, e As EventArgs) Handles TextBox_tunerEvent.TextChanged

        If (IgnoreChanges = False And IsNumeric(TextBox_tunerEvent.Text)) Then

            If (TextBox_tunerEvent.Text >= 0 And TextBox_TriggerRight.Text <= 99) Then

                Inputs.Item(CurrentFrame - 1).tunerEvent = TextBox_tunerEvent.Text

            End If

        End If

    End Sub

    Private Sub TextBox_LinkX_TextChanged(sender As Object, e As EventArgs) Handles TextBox_LinkX.TextChanged

        If (IgnoreChanges = False And IsNumeric(TextBox_LinkX.Text)) Then

            Inputs.Item(CurrentFrame - 1).LinkX = TextBox_LinkX.Text

        End If

    End Sub

    Private Sub TextBox_LinkZ_TextChanged(sender As Object, e As EventArgs) Handles TextBox_LinkZ.TextChanged

        If (IgnoreChanges = False And IsNumeric(TextBox_LinkZ.Text)) Then

            Inputs.Item(CurrentFrame - 1).LinkZ = TextBox_LinkZ.Text

        End If

    End Sub

    Private Sub CheckBox_A_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox_A.CheckedChanged

        If (IgnoreChanges = False) Then

            Inputs.Item(CurrentFrame - 1).A = CheckBox_A.Checked

        End If

    End Sub

    Private Sub CheckBox_B_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox_B.CheckedChanged

        If (IgnoreChanges = False) Then

            Inputs.Item(CurrentFrame - 1).B = CheckBox_B.Checked

        End If

    End Sub

    Private Sub CheckBox_X_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox_X.CheckedChanged

        If (IgnoreChanges = False) Then

            Inputs.Item(CurrentFrame - 1).X = CheckBox_X.Checked

        End If

    End Sub

    Private Sub CheckBox_Y_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox_Y.CheckedChanged

        If (IgnoreChanges = False) Then

            Inputs.Item(CurrentFrame - 1).Y = CheckBox_Y.Checked

        End If

    End Sub

    Private Sub CheckBox_Z_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox_Z.CheckedChanged

        If (IgnoreChanges = False) Then

            Inputs.Item(CurrentFrame - 1).Z = CheckBox_Z.Checked

        End If

    End Sub

    Private Sub CheckBox_Start_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox_Start.CheckedChanged

        If (IgnoreChanges = False) Then

            Inputs.Item(CurrentFrame - 1).Start = CheckBox_Start.Checked

        End If

    End Sub

    Private Sub CheckBox_L_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox_L.CheckedChanged

        If (IgnoreChanges = False) Then

            Inputs.Item(CurrentFrame - 1).L = CheckBox_L.Checked

        End If

    End Sub

    Private Sub CheckBox_R_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox_R.CheckedChanged

        If (IgnoreChanges = False) Then

            Inputs.Item(CurrentFrame - 1).R = CheckBox_R.Checked

        End If

    End Sub

    Private Sub Checkbox_DU_CheckedChanged(sender As Object, e As EventArgs) Handles Checkbox_DU.CheckedChanged

        If (IgnoreChanges = False) Then

            Inputs.Item(CurrentFrame - 1).DPadUp = Checkbox_DU.Checked

        End If

    End Sub

    Private Sub CheckBox_DD_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox_DD.CheckedChanged

        If (IgnoreChanges = False) Then

            Inputs.Item(CurrentFrame - 1).DPadDown = CheckBox_DD.Checked

        End If

    End Sub

    Private Sub CheckBox_DL_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox_DL.CheckedChanged

        If (IgnoreChanges = False) Then

            Inputs.Item(CurrentFrame - 1).DPadLeft = CheckBox_DL.Checked

        End If

    End Sub

    Private Sub CheckBox_DR_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox_DR.CheckedChanged

        If (IgnoreChanges = False) Then

            Inputs.Item(CurrentFrame - 1).DPadRight = CheckBox_DR.Checked

        End If

    End Sub

    Private Sub CheckBox_reset_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox_reset.CheckedChanged

        If (IgnoreChanges = False) Then

            Inputs.Item(CurrentFrame - 1).reset = CheckBox_reset.Checked

        End If

    End Sub

    Private Sub CheckBox_loading_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox_loading.CheckedChanged

        If (IgnoreChanges = False) Then

            Inputs.Item(CurrentFrame - 1).loading = CheckBox_loading.Checked

        End If

    End Sub

    Private Sub CheckBox_disc_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox_disc.CheckedChanged

        If (IgnoreChanges = False) Then

            Inputs.Item(CurrentFrame - 1).disc = CheckBox_disc.Checked

        End If

    End Sub

    Private Sub Button_AddFrame_Click(sender As Object, e As EventArgs) Handles Button_AddFrame.Click

        If (Loaded = True And IgnoreChanges = False) Then

            Dim Input As New ControllerState_Version3
            Dim Input2 As New ControllerState_Version3

            Inputs.Insert(CurrentFrame, Input)
            Inputs.Insert(CurrentFrame + 1, Input2)

            Header.frameCount += 1
            Header.inputCount += 2

            TextBox_framecount.Text = Header.frameCount
            TextBox_inputcount.Text = Header.inputCount

            TextBox_GoToFrame.Text = CurrentFrame + 1

        End If

    End Sub

    Private Sub Button_RemoveFrame_Click(sender As Object, e As EventArgs) Handles Button_RemoveFrame.Click

        If (Loaded = True And IgnoreChanges = False) Then

            Inputs.RemoveAt(CurrentFrame - 1)
            Inputs.RemoveAt(CurrentFrame - 1)

            Header.frameCount -= 1
            Header.inputCount -= 2

            TextBox_framecount.Text = Header.frameCount
            TextBox_inputcount.Text = Header.inputCount

            If (CurrentFrame > Header.inputCount) Then

                CurrentFrame = Header.inputCount

                TextBox_GoToFrame.Text = CurrentFrame

            Else

                IgnoreChanges = True
                FillInputForm()
                IgnoreChanges = False

            End If

        End If

    End Sub

    Private Sub Button_GoForward_Click(sender As Object, e As EventArgs) Handles Button_GoForward.Click

        If (Loaded = True And CurrentFrame < Header.inputCount) Then

            TextBox_GoToFrame.Text = CurrentFrame + 1

        End If

    End Sub

    Private Sub Button_GoBack_Click(sender As Object, e As EventArgs) Handles Button_GoBack.Click

        If (Loaded = True And CurrentFrame > 1) Then

            TextBox_GoToFrame.Text = CurrentFrame - 1

        End If

    End Sub

End Class

Public Class DTMHeader_Version3

    Public filetype(3) As Byte ' Unique Identifier (always "DTM"0x1A)

    Public gameID(5) As Byte ' The Game ID
    Public bWii As Boolean = False ' Wii game

    Public numControllers As Byte ' The number of connected controllers (1-4)

    Public bFromSaveState As Boolean = False ' false indicates that the recording started from bootup, true for savestate
    Public frameCount As UInt64 ' Number of frames in the recording
    Public inputCount As UInt64  ' Number of input frames in recording
    Public lagCount As UInt64  ' Number of lag frames in the recording
    Public uniqueID As UInt64 ' (not implemented) A Unique ID comprised of: md5(time + Game ID)
    Public numRerecords As UInt32 ' Number of rerecords/'cuts' of this TAS
    Public author(31) As Byte ' Author's name (encoded in UTF-8)

    Public videoBackend(15) As Byte ' UTF-8 representation of the video backend
    Public audioEmulator(15) As Byte ' UTF-8 representation of the audio emulator
    Public md5(15) As Byte ' MD5 of game iso

    Public recordingStartTime As UInt64 ' seconds since 1970 that recording started (used for RTC)

    Public bSaveConfig As Boolean = False ' Loads the settings below on startup if true
    Public bSkipIdle As Boolean = False
    Public bDualCore As Boolean = False
    Public bProgressive As Boolean = False
    Public bDSPHLE As Boolean = False
    Public bFastDiscSpeed As Boolean = False
    Public CPUCore As Byte ' 0 = interpreter, 1 = JIT, 2 = JITIL
    Public bEFBAccessEnable As Boolean = False
    Public bEFBCopyEnable As Boolean = False
    Public bCopyEFBToTexture As Boolean = False
    Public bEFBCopyCacheEnable As Boolean = False
    Public bEFBEmulateFormatChanges As Boolean = False
    Public bUseXFB As Boolean = False
    Public bUseRealXFB As Boolean = False
    Public memcards As Byte
    Public bClearSave As Boolean = False ' Create a new memory card when playing back a movie if true
    Public bongos As Byte
    Public numGBAs As Byte 'Dragonbane
    Public bSyncGPU As Boolean = False
    Public bNetPlay As Boolean = False
    Public reserved(12) As Byte ' Padding for any new config options
    Public discChange(39) As Byte ' Name of iso file to switch to, for two disc games.
    Public revision(19) As Byte ' Git hash
    Public DSPiromHash As UInt32
    Public DSPcoefHash As UInt32
    Public tickCount As UInt64 ' Number of ticks in the recording
    Public reserved2(10) As Byte ' Make heading 256 bytes, just because we can

End Class

Public Class ControllerState_Version3

    Public Start = False, A = False, B = False, X = False, Y = False, Z As Boolean = False ' Binary buttons, 6 bits
    Public DPadUp = False, DPadDown = False, DPadLeft = False, DPadRight As Boolean = False ' Binary D-Pad buttons, 4 bits
    Public L = False, R As Boolean = False ' Binary triggers, 2 bits

    Public disc As Boolean = False ' Checks for disc being changed
    Public reset As Boolean = False ' Console reset button
    Public loading As Boolean = False ' Dragonbane: Loading status flag, 1 bit
    Public reserved As Boolean = False ' Reserved bits used for padding, 1 bit

    Public TriggerL As Byte = 0 ' Triggers, 16 bits
    Public TriggerR As Byte = 0
    Public AnalogStickX As Byte = 128 ' Main Stick, 16 bits
    Public AnalogStickY As Byte = 128
    Public CStickX As Byte = 128 ' Sub-Stick, 16 bits
    Public CStickY As Byte = 128

    Public tunerEvent As Byte = 0 ' Dragonbane: Tuner Event, 8 bits

    Public LinkX As Single = 0F ' Dragonbane: Used to detect desyncs, 64 bits
    Public LinkZ As Single = 0F

End Class