Imports System.Globalization
Imports System.Windows.Forms

Public Class Form1
    Private trayIcon As NotifyIcon
    Private contextMenu1 As ContextMenuStrip
    Private currentHebrewYear As Integer
    Private currentHebrewMonth As Integer ' 1-based, Tishrei=1
    Private dayToolTips As ToolTip
    Protected Overrides Sub WndProc(ByRef m As Message)
        Const WM_NCLBUTTONDOWN As Integer = &HA1
        Const HTCAPTION As Integer = 2

        If m.Msg = WM_NCLBUTTONDOWN AndAlso m.WParam.ToInt32() = HTCAPTION Then
            ' Block form dragging
            Return
        End If

        MyBase.WndProc(m)
    End Sub

    Private Sub ShowCalendar(sender As Object, e As EventArgs)
        ' Position the form above the mouse (can later align to tray icon)
        Dim mousePos = Cursor.Position
        Me.Location = New Point(mousePos.X - Me.Width \ 2, mousePos.Y - Me.Height - 10)
        Me.Show()
        Me.BringToFront()
    End Sub

    Private Sub ExitApp(sender As Object, e As EventArgs)
        trayIcon.Visible = False
        Application.Exit()
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If e.CloseReason = CloseReason.UserClosing Then
            e.Cancel = True
            Me.Hide()
        Else
            MyBase.OnFormClosing(e)
        End If
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        dayToolTips = New ToolTip() With {
        .AutoPopDelay = 5000,
        .InitialDelay = 500,
        .ReshowDelay = 200,
        .ShowAlways = True
        }

        Me.ShowInTaskbar = False
        Me.FormBorderStyle = FormBorderStyle.FixedToolWindow ' shows X, no minimize/maximize
        Me.ControlBox = True
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.Manual
        Me.Visible = False

        ' Set up tray icon
        trayIcon = New NotifyIcon()
        trayIcon.Icon = SystemIcons.Information
        trayIcon.Visible = True

        ' Context menu
        contextMenu1 = New ContextMenuStrip()
        contextMenu1.Items.Add("הצג לוח", Nothing, AddressOf ShowCalendar)
        contextMenu1.Items.Add("יציאה", Nothing, AddressOf ExitApp)
        trayIcon.ContextMenuStrip = contextMenu1

        ' Set Hebrew tooltip
        Dim hc As New HebrewCalendar()
        Dim today = Date.Today

        Dim hYear = hc.GetYear(today)
        Dim hMonth = hc.GetMonth(today)
        Dim hDay = hc.GetDayOfMonth(today)

        Dim hebrewDate = $"{IntToHebrewDay(hDay)} {GetHebrewMonthName(hMonth, hYear)} {IntToHebrewYear(hYear)}"
        trayIcon.Text = $"היום: {hebrewDate}".Substring(0, Math.Min(63, $"היום: {hebrewDate}".Length))

        ' Init calendar state
        currentHebrewYear = hYear
        currentHebrewMonth = hMonth

        DrawCalendar(0)
        DrawCalendar(0)
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        DrawCalendar(1)
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        DrawCalendar(-1)
    End Sub

    Private Sub DrawCalendar(Optional monthOffset As Integer = 0)
        Dim hc As New HebrewCalendar()

        ' Adjust month/year with rollover
        currentHebrewMonth += monthOffset
        Do While True
            Dim isLeap = hc.IsLeapYear(currentHebrewYear)
            Dim maxMonth = If(isLeap, 13, 12)

            If currentHebrewMonth < 1 Then
                currentHebrewYear -= 1
                currentHebrewMonth += If(hc.IsLeapYear(currentHebrewYear), 13, 12)
            ElseIf currentHebrewMonth > maxMonth Then
                currentHebrewMonth -= maxMonth
                currentHebrewYear += 1
            Else
                Exit Do
            End If
        Loop

        ClearCalendarGrid()

        Dim daysInMonth = hc.GetDaysInMonth(currentHebrewYear, currentHebrewMonth)
        Dim firstDay = hc.ToDateTime(currentHebrewYear, currentHebrewMonth, 1, 0, 0, 0, 0)
        Dim startDayOfWeek = CInt(firstDay.DayOfWeek)

        ' Label1 = title
        Dim monthName = GetHebrewMonthName(currentHebrewMonth, currentHebrewYear)
        Dim yearText = IntToHebrewYear(currentHebrewYear)
        Label1.Text = $"{monthName}, {yearText}"

        ' Fill grid
        For day = 1 To daysInMonth
            Dim gDate = hc.ToDateTime(currentHebrewYear, currentHebrewMonth, day, 0, 0, 0, 0)
            Dim dow = CInt(gDate.DayOfWeek)
            Dim row = (day + startDayOfWeek - 1) \ 7

            Dim lbl As New Label With {
            .Text = IntToHebrewDay(day),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Dock = DockStyle.Fill,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Margin = New Padding(1)
}

            dayToolTips.SetToolTip(lbl, gDate.ToString("dd/MM/yyyy"))

            ' Highlight today's date
            Dim today As Date = Date.Today
            If gDate.Date = today.Date Then
                lbl.BackColor = Color.LightGoldenrodYellow
                lbl.BorderStyle = BorderStyle.FixedSingle
            End If

            TableLayoutPanel1.Controls.Add(lbl, dow, row)
        Next
    End Sub

    Private Sub ClearCalendarGrid()
        For i = TableLayoutPanel1.Controls.Count - 1 To 0 Step -1
            Dim ctrl = TableLayoutPanel1.Controls(i)
            If TypeOf ctrl Is Label Then
                TableLayoutPanel1.Controls.RemoveAt(i)
                ctrl.Dispose()
            End If
        Next
    End Sub

    Private Function GetHebrewMonthName(dotNetMonth As Integer, year As Integer) As String
        Dim isLeap = New HebrewCalendar().IsLeapYear(year)
        Dim monthsRegular = {
            "תשרי", "חשון", "כסלו", "טבת", "שבט", "אדר",
            "ניסן", "אייר", "סיון", "תמוז", "אב", "אלול"
        }
        Dim monthsLeap = {
            "תשרי", "חשון", "כסלו", "טבת", "שבט", "אדר א",
            "אדר ב", "ניסן", "אייר", "סיון", "תמוז", "אב", "אלול"
        }

        If isLeap Then
            Return monthsLeap(dotNetMonth - 1)
        Else
            Return monthsRegular(dotNetMonth - 1)
        End If
    End Function

    Private Function IntToHebrewDay(n As Integer) As String
        Dim ones() = {"א", "ב", "ג", "ד", "ה", "ו", "ז", "ח", "ט"}
        Dim tens() = {"י", "כ", "ל", "מ", "נ", "ס", "ע", "פ", "צ"}

        If n <= 10 Then
            If n = 10 Then Return "י"
            Return $"{ones(n - 1)}"
        ElseIf n = 15 Then
            Return "טו"
        ElseIf n = 16 Then
            Return "טז"
        ElseIf n < 20 Then
            Return $"י{ones(n - 11)}"
        ElseIf n Mod 10 = 0 Then
            Return $"{tens(n \ 10 - 1)}"
        Else
            Return $"{tens(n \ 10 - 1)}{ones(n Mod 10 - 1)}"
        End If
    End Function

    Private Function IntToHebrewYear(year As Integer) As String
        If year <= 0 Then Return ""

        Dim map As New Dictionary(Of Integer, String) From {
            {1, "א"}, {2, "ב"}, {3, "ג"}, {4, "ד"}, {5, "ה"},
            {6, "ו"}, {7, "ז"}, {8, "ח"}, {9, "ט"},
            {10, "י"}, {20, "כ"}, {30, "ל"}, {40, "מ"}, {50, "נ"},
            {60, "ס"}, {70, "ע"}, {80, "פ"}, {90, "צ"},
            {100, "ק"}, {200, "ר"}, {300, "ש"}, {400, "ת"},
            {500, "תק"}, {600, "תר"}, {700, "תש"}, {800, "תת"}, {900, "תתק"}
        }

        ' Handle thousands prefix
        If year >= 1000 Then
            Dim thousands = year \ 1000
            Dim remainder = year Mod 1000
            Dim prefix As String = If(map.ContainsKey(thousands), map(thousands) & "׳", "")
            Return prefix & IntToHebrewYear(remainder)
        End If

        ' Special: טו / טז
        If year = 15 Then Return "טו"
        If year = 16 Then Return "טז"

        ' Find largest key less than or equal to year
        Dim key = map.Keys.Where(Function(k) k <= year).OrderByDescending(Function(k) k).FirstOrDefault()

        If key = 0 Then Return ""
        Return map(key) & IntToHebrewYear(year - key)
    End Function

    Private Sub Button3_Click_1(sender As Object, e As EventArgs) Handles Button3.Click
        Dim hc As New HebrewCalendar()
        Dim today = Date.Today
        currentHebrewYear = hc.GetYear(today)
        currentHebrewMonth = hc.GetMonth(today)
        DrawCalendar(0)
    End Sub

    Private Sub Label1_Click(sender As Object, e As EventArgs) Handles Label1.Click
        Dim picker As New FormMonthPicker(currentHebrewYear, currentHebrewMonth)
        AddHandler picker.MonthSelected, AddressOf SetSelectedMonth
        picker.Location = Cursor.Position
        picker.Show()
    End Sub

    Private Sub SetSelectedMonth(year As Integer, month As Integer)
        currentHebrewYear = year
        currentHebrewMonth = month
        DrawCalendar(0)
    End Sub
End Class

Public Class FormMonthPicker
    Inherits Form

    Public Event MonthSelected(hebrewYear As Integer, hebrewMonth As Integer)

    Private yearCombo As ComboBox
    Private monthCombo As ComboBox
    Private confirmButton As Button
    Private hc As New HebrewCalendar()

    Public Sub New(currentYear As Integer, currentMonth As Integer)
        Me.FormBorderStyle = FormBorderStyle.FixedToolWindow
        Me.StartPosition = FormStartPosition.Manual
        Me.ShowInTaskbar = False
        Me.Width = 200
        Me.Height = 150

        yearCombo = New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Left = 10, .Top = 10, .Width = 170}
        monthCombo = New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Left = 10, .Top = 40, .Width = 170}
        confirmButton = New Button With {.Text = "אישור", .Left = 10, .Top = 80, .Width = 170}

        For y = 5700 To 5800
            yearCombo.Items.Add(y)
        Next
        yearCombo.SelectedItem = currentYear

        AddHandler yearCombo.SelectedIndexChanged, AddressOf UpdateMonthList
        AddHandler confirmButton.Click, AddressOf ConfirmSelection

        Me.Controls.Add(yearCombo)
        Me.Controls.Add(monthCombo)
        Me.Controls.Add(confirmButton)

        UpdateMonthList(Nothing, Nothing)
        monthCombo.SelectedIndex = currentMonth - 1
    End Sub

    Private Sub UpdateMonthList(sender As Object, e As EventArgs)
        If yearCombo.SelectedItem Is Nothing Then Return
        Dim year As Integer = CInt(yearCombo.SelectedItem)
        Dim isLeap = hc.IsLeapYear(year)

        monthCombo.Items.Clear()
        Dim months = If(isLeap,
            {"תשרי", "חשון", "כסלו", "טבת", "שבט", "אדר א", "אדר ב", "ניסן", "אייר", "סיון", "תמוז", "אב", "אלול"},
            {"תשרי", "חשון", "כסלו", "טבת", "שבט", "אדר", "ניסן", "אייר", "סיון", "תמוז", "אב", "אלול"}
        )

        monthCombo.Items.AddRange(months)
    End Sub

    Private Sub ConfirmSelection(sender As Object, e As EventArgs)
        If yearCombo.SelectedItem Is Nothing OrElse monthCombo.SelectedIndex = -1 Then Return
        Dim y = CInt(yearCombo.SelectedItem)
        Dim m = monthCombo.SelectedIndex + 1
        RaiseEvent MonthSelected(y, m)
        Me.Close()
    End Sub
End Class
