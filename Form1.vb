Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports Zmanim
Imports Zmanim.TimeZone
Imports Zmanim.Utilities

Public Class Form1
    ' Region: Private Member Variables
    ' These variables store the state and components of the form and calendar.
    Private trayIcon As NotifyIcon
    Private contextMenu1 As ContextMenuStrip
    Private currentHebrewYear As Integer
    Private currentHebrewMonth As Integer ' 1-based, Tishrei=1
    Private dayToolTips As ToolTip
    Private holidayMap As New Dictionary(Of Date, List(Of String))
    Private lastComputedYear As Integer = -1

    ' End Region

    ' Region: Form Event Handlers
    ' These methods handle standard Windows Form events.
    Protected Overrides Sub WndProc(ByRef m As Message)
        Const WM_NCLBUTTONDOWN As Integer = &HA1
        Const HTCAPTION As Integer = 2

        If m.Msg = WM_NCLBUTTONDOWN AndAlso m.WParam.ToInt32() = HTCAPTION Then
            ' Block form dragging
            Return
        End If

        MyBase.WndProc(m)
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        ' Initialize ToolTip for calendar days
        dayToolTips = New ToolTip() With {
            .AutoPopDelay = 5000,
            .InitialDelay = 500,
            .ReshowDelay = 200,
            .ShowAlways = True
        }

        ' Configure form appearance and behavior
        Me.ShowInTaskbar = False
        Me.FormBorderStyle = FormBorderStyle.FixedToolWindow ' shows X, no minimize/maximize
        Me.ControlBox = True
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.Manual
        Me.Visible = False

        ' Set up tray icon
        trayIcon = New NotifyIcon()
        trayIcon.Icon = SystemIcons.Information ' Consider using a custom icon if available
        trayIcon.Visible = True

        AddHandler trayIcon.MouseUp, AddressOf TrayIcon_MouseUp

        ' Set up context menu for tray icon
        contextMenu1 = New ContextMenuStrip()
        contextMenu1.Items.Add("הצג לוח", Nothing, AddressOf ShowCalendar)
        contextMenu1.Items.Add("יציאה", Nothing, AddressOf ExitApp)
        trayIcon.ContextMenuStrip = contextMenu1

        ' Initialize Hebrew calendar state and update tray icon tooltip
        Dim hc As New HebrewCalendar()
        Dim today = Date.Today

        Dim hYear = hc.GetYear(today)
        Dim hMonth = hc.GetMonth(today)
        Dim hDay = hc.GetDayOfMonth(today)

        Dim hebrewDate = $"{IntToHebrewDay(hDay)} {GetHebrewMonthName(hMonth, hYear)} {IntToHebrewYear(hYear)}"
        trayIcon.Text = $"היום: {hebrewDate}".Substring(0, Math.Min(63, $"היום: {hebrewDate}".Length)) ' Tray icon text limit

        ' Set initial calendar view to today
        currentHebrewYear = hYear
        currentHebrewMonth = hMonth

        ' Draw the calendar (called twice in original, one is sufficient for initial load)
        DrawCalendar(0)
    End Sub
    Private Sub TrayIcon_MouseUp(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            ShowCalendar(Nothing, Nothing)
        End If
    End Sub
    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        ' Intercept user closing to hide the form instead of exiting
        If e.CloseReason = CloseReason.UserClosing Then
            e.Cancel = True
            Me.Hide()
        Else
            MyBase.OnFormClosing(e)
        End If
    End Sub
    ' End Region

    ' Region: UI Interaction Handlers
    ' These methods respond to user interactions with the form controls.
    Private Sub ShowCalendar(sender As Object, e As EventArgs)
        ' Position the form above the mouse (can later align to tray icon)
        Dim mousePos = Cursor.Position
        Me.Location = New Point(mousePos.X - Me.Width \ 2, mousePos.Y - Me.Height - 10)
        Me.Show()
        Me.BringToFront()
    End Sub

    Private Sub ExitApp(sender As Object, e As EventArgs)
        ' Clean up tray icon and exit the application
        trayIcon.Visible = False
        Application.Exit()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        ' Handle click for next month button
        DrawCalendar(1)
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        ' Handle click for previous month button
        DrawCalendar(-1)
    End Sub

    Private Sub Button3_Click_1(sender As Object, e As EventArgs) Handles Button3.Click
        ' Handle click for "Today" button
        Dim hc As New HebrewCalendar()
        Dim today = Date.Today
        currentHebrewYear = hc.GetYear(today)
        currentHebrewMonth = hc.GetMonth(today)
        DrawCalendar(0)
    End Sub

    Private Sub Label1_Click(sender As Object, e As EventArgs) Handles Label1.Click
        ' Open month picker dialog when the month/year label is clicked
        Dim picker As New FormMonthPicker(currentHebrewYear, currentHebrewMonth)
        AddHandler picker.MonthSelected, AddressOf SetSelectedMonth
        picker.Location = Cursor.Position
        picker.Show()
    End Sub

    Private Sub SetSelectedMonth(year As Integer, month As Integer)
        ' Callback from month picker to set the selected month and redraw
        currentHebrewYear = year
        currentHebrewMonth = month
        DrawCalendar(0)
    End Sub
    ' End Region

    ' Region: Calendar Drawing and Logic
    ' These methods are responsible for rendering the Hebrew calendar and managing its state.
    Private Sub DrawCalendar(Optional monthOffset As Integer = 0)
        Dim hc As New HebrewCalendar()

        ' Recalculate holidays if the year has changed
        If currentHebrewYear <> lastComputedYear Then
            AddHebrewHolidaysForYear(currentHebrewYear)
            AddModernHoliday(hc, currentHebrewYear) ' Call this method here
            lastComputedYear = currentHebrewYear
        End If

        ' Adjust month/year with rollover logic
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

        ' Update calendar title (Label1)
        Dim monthName = GetHebrewMonthName(currentHebrewMonth, currentHebrewYear)
        Dim yearText = IntToHebrewYear(currentHebrewYear)
        Label1.Text = $"{monthName}, {yearText}"

        ' Fill calendar grid with days
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
            Dim names = GetHolidayName(gDate)

            Dim tooltipText As String = $"{gDate:dd/MM/yyyy}"
            If names.Count > 0 Then
                lbl.ForeColor = Color.Red
                tooltipText &= $" - {String.Join(" / ", names)}"
            End If

            If gDate.DayOfWeek = DayOfWeek.Saturday Then
                Try
                    Dim shabbatTimes = GetShabbatTimes(gDate)
                    Dim candle = shabbatTimes.Item1
                    Dim havdalah = shabbatTimes.Item2
                    tooltipText &= $"{Environment.NewLine}כניסת שבת: {candle}, צאת שבת: {havdalah}"
                Catch ex As Exception
                    tooltipText &= $"{Environment.NewLine}(שגיאה בחישוב זמני שבת)"
                End Try
            End If

            dayToolTips.SetToolTip(lbl, tooltipText)

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
        ' Remove all dynamically added labels from the TableLayoutPanel
        For i = TableLayoutPanel1.Controls.Count - 1 To 0 Step -1
            Dim ctrl = TableLayoutPanel1.Controls(i)
            If TypeOf ctrl Is Label Then
                TableLayoutPanel1.Controls.RemoveAt(i)
                ctrl.Dispose()
            End If
        Next
    End Sub
    ' End Region

    ' Region: Hebrew Date Conversion Utilities
    ' These functions convert integers to Hebrew date representations.
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

        ' Handle thousands prefix (e.g., ה׳תשפ״ה)
        If year >= 1000 Then
            Dim thousands = year \ 1000
            Dim remainder = year Mod 1000
            Dim prefix As String = If(map.ContainsKey(thousands), map(thousands) & "׳", "")
            Return prefix & IntToHebrewYear(remainder)
        End If

        ' Special cases: 15 (טו) and 16 (טז)
        If year = 15 Then Return "טו"
        If year = 16 Then Return "טז"

        ' Find largest key less than or equal to year
        Dim key = map.Keys.Where(Function(k) k <= year).OrderByDescending(Function(k) k).FirstOrDefault()

        If key = 0 Then Return ""
        Return map(key) & IntToHebrewYear(year - key)
    End Function

    Private Function GetHebrewMonthNumber(name As String, isLeap As Boolean) As Integer
        Dim mapNormal = New Dictionary(Of String, Integer) From {
            {"תשרי", 1}, {"חשון", 2}, {"כסלו", 3}, {"טבת", 4}, {"שבט", 5},
            {"אדר", 6}, {"ניסן", 7}, {"אייר", 8}, {"סיון", 9}, {"תמוז", 10},
            {"אב", 11}, {"אלול", 12}
        }

        Dim mapLeap = New Dictionary(Of String, Integer) From {
            {"תשרי", 1}, {"חשון", 2}, {"כסלו", 3}, {"טבת", 4}, {"שבט", 5},
            {"אדר א", 6}, {"אדר ב", 7}, {"ניסן", 8}, {"אייר", 9}, {"סיון", 10},
            {"תמוז", 11}, {"אב", 12}, {"אלול", 13}
        }

        Return If(isLeap, mapLeap(name), mapNormal(name))
    End Function
    ' End Region

    ' Region: Holiday Management
    ' These methods handle the addition and retrieval of Hebrew holidays.
    Private Sub AddHebrewHolidaysForYear(hYear As Integer)
        ' Clear previous year's holidays
        holidayMap.Clear()

        Dim hc As New HebrewCalendar()
        Dim isLeap = hc.IsLeapYear(hYear)

        ' Get Hebrew month numbers for the current year (handling leap year)
        Dim tishrei = GetHebrewMonthNumber("תשרי", isLeap)
        Dim cheshvan = GetHebrewMonthNumber("חשון", isLeap)
        Dim kislev = GetHebrewMonthNumber("כסלו", isLeap)
        Dim tevet = GetHebrewMonthNumber("טבת", isLeap)
        Dim shevat = GetHebrewMonthNumber("שבט", isLeap)
        Dim adar = GetHebrewMonthNumber("אדר", isLeap)
        Dim adar2 = If(isLeap, GetHebrewMonthNumber("אדר ב", isLeap), adar) ' If not leap, Adar is Adar A

        Dim nisan = GetHebrewMonthNumber("ניסן", isLeap)
        Dim iyar = GetHebrewMonthNumber("אייר", isLeap)
        Dim sivan = GetHebrewMonthNumber("סיון", isLeap)
        Dim tammuz = GetHebrewMonthNumber("תמוז", isLeap)
        Dim av = GetHebrewMonthNumber("אב", isLeap)
        Dim elul = GetHebrewMonthNumber("אלול", isLeap)

        ' Pesach and Chol HaMoed
        AddHoliday(hc.ToDateTime(hYear, nisan, 14, 0, 0, 0, 0), "ערב פסח")
        AddHoliday(hc.ToDateTime(hYear, nisan, 15, 0, 0, 0, 0), "פסח")
        For d = 16 To 20 : AddHoliday(hc.ToDateTime(hYear, nisan, d, 0, 0, 0, 0), $"{IntToHebrewDay(d - 15)} חול המועד פסח") : Next
        AddHoliday(hc.ToDateTime(hYear, nisan, 21, 0, 0, 0, 0), "שביעי של פסח")
        AddHoliday(hc.ToDateTime(hYear, nisan, 22, 0, 0, 0, 0), "איסרו חג פסח")

        ' Shavuot
        AddHoliday(hc.ToDateTime(hYear, sivan, 5, 0, 0, 0, 0), "ערב שבועות")
        AddHoliday(hc.ToDateTime(hYear, sivan, 6, 0, 0, 0, 0), "שבועות")
        AddHoliday(hc.ToDateTime(hYear, sivan, 7, 0, 0, 0, 0), "איסרו חג שבועות")

        ' Tu BiShvat
        AddHoliday(hc.ToDateTime(hYear, shevat, 15, 0, 0, 0, 0), "ט״ו בשבט")

        ' Lag BaOmer
        AddHoliday(hc.ToDateTime(hYear, iyar, 18, 0, 0, 0, 0), "ל״ג בעומר")

        ' Tzom Shivah Asar B'Tammuz
        AddHoliday(hc.ToDateTime(hYear, tammuz, 17, 0, 0, 0, 0), "שבעה עשר בתמוז")

        ' Tisha B'Av
        AddHoliday(hc.ToDateTime(hYear, av, 9, 0, 0, 0, 0), "תשעה באב")

        ' Rosh Chodesh (each month)
        Dim maxMonth = If(isLeap, 13, 12)
        For m = 1 To maxMonth
            Dim days = hc.GetDaysInMonth(hYear, m)
            If days >= 30 Then
                AddHoliday(hc.ToDateTime(hYear, m, 30, 0, 0, 0, 0), "ראש חודש")
            End If

            ' Add next month's 1st only if next month is valid
            If m < maxMonth Then
                AddHoliday(hc.ToDateTime(hYear, m + 1, 1, 0, 0, 0, 0), "ראש חודש")
            End If
        Next

        ' Sukkot, Chol HaMoed, Shmini Atzeret
        AddHoliday(hc.ToDateTime(hYear, tishrei, 14, 0, 0, 0, 0), "ערב סוכות")
        AddHoliday(hc.ToDateTime(hYear, tishrei, 15, 0, 0, 0, 0), "סוכות")
        For d = 16 To 20 : AddHoliday(hc.ToDateTime(hYear, tishrei, d, 0, 0, 0, 0), $"{IntToHebrewDay(d - 15)} חול המועד סוכות") : Next
        AddHoliday(hc.ToDateTime(hYear, tishrei, 21, 0, 0, 0, 0), "הושענא רבה")
        AddHoliday(hc.ToDateTime(hYear, tishrei, 22, 0, 0, 0, 0), "שמיני עצרת")
        AddHoliday(hc.ToDateTime(hYear, tishrei, 23, 0, 0, 0, 0), "איסרו חג סוכות")

        ' Tzom Gedaliah: 3 Tishrei, moved if Rosh Hashanah is Thu-Fri
        Dim gedalia = hc.ToDateTime(hYear, tishrei, 3, 0, 0, 0, 0)
        If gedalia.DayOfWeek = DayOfWeek.Saturday Then
            gedalia = gedalia.AddDays(1)
        End If
        AddHoliday(gedalia, "צום גדליה")

        ' Tzom Asarah B'Tevet (only moved if on Shabbat — and it never is for 10 Tevet)
        AddHoliday(hc.ToDateTime(hYear, tevet, 10, 0, 0, 0, 0), "עשרה בטבת")

        ' Taanit Esther: shifted if Friday/Saturday
        Dim esther = hc.ToDateTime(hYear, adar2, 13, 0, 0, 0, 0)
        If esther.DayOfWeek = DayOfWeek.Saturday Then
            esther = esther.AddDays(-2) ' Moved to Thursday
        ElseIf esther.DayOfWeek = DayOfWeek.Friday Then
            esther = esther.AddDays(-1) ' Moved to Thursday
        End If
        AddHoliday(esther, "תענית אסתר")

        ' Purim
        AddHoliday(hc.ToDateTime(hYear, adar2, 14, 0, 0, 0, 0), "פורים")
        Dim shushan = hc.ToDateTime(hYear, adar2, 15, 0, 0, 0, 0)
        If shushan.DayOfWeek = DayOfWeek.Saturday Then
            ' If Shushan Purim is on Shabbat, it's observed as Purim Meshulash,
            ' where Megillah reading is on Friday, Matanot La'Evyonim on Sunday.
            ' The specific "Shushan Purim" observance for Jerusalem shifts.
            shushan = shushan.AddDays(1)
            AddHoliday(shushan, "פורים משולש (ירושלים)") ' This refers to the observation of some aspects on Sunday
        Else
            AddHoliday(shushan, "שושן פורים")
        End If
    End Sub

    Private Sub AddModernHoliday(hc As HebrewCalendar, hYear As Integer)
        ' Yom HaShoah (Holocaust Remembrance Day)
        Dim nisan27 = hc.ToDateTime(hYear, GetHebrewMonthNumber("ניסן", hc.IsLeapYear(hYear)), 27, 0, 0, 0, 0)
        Select Case nisan27.DayOfWeek
            Case DayOfWeek.Friday
                AddHoliday(nisan27.AddDays(-1), "יום השואה") ' Moved to Thursday
            Case DayOfWeek.Sunday
                AddHoliday(nisan27.AddDays(1), "יום השואה") ' Moved to Monday
            Case Else
                AddHoliday(nisan27, "יום השואה")
        End Select

        ' Yom HaZikaron (Memorial Day) + Yom HaAtzmaut (Independence Day)
        Dim iyyar = GetHebrewMonthNumber("אייר", hc.IsLeapYear(hYear))
        Dim pesach15Nisan = hc.ToDateTime(hYear, GetHebrewMonthNumber("ניסן", hc.IsLeapYear(hYear)), 15, 0, 0, 0, 0) ' 15 Nisan
        Dim pdow = pesach15Nisan.DayOfWeek ' Day of week for 15th of Nisan

        Dim dayIyyar As Integer
        If pdow = DayOfWeek.Sunday Then ' If Pesach is Sunday, Yom HaAtzmaut is on Tuesday
            dayIyyar = 2
        ElseIf pdow = DayOfWeek.Saturday Then ' If Pesach is Saturday, Yom HaAtzmaut is on Tuesday
            dayIyyar = 3
        ElseIf hYear < 5764 AndAlso pdow = DayOfWeek.Tuesday Then ' Pre-5764 rule for Tuesday Pesach
            dayIyyar = 5
        ElseIf pdow = DayOfWeek.Tuesday Then ' Post-5764 rule for Tuesday Pesach (Yom HaAtzmaut on Thursday)
            dayIyyar = 5
        Else ' Default (Yom HaAtzmaut on 5th of Iyyar)
            dayIyyar = 4
        End If

        AddHoliday(hc.ToDateTime(hYear, iyyar, dayIyyar, 0, 0, 0, 0), "יום הזיכרון")
        AddHoliday(hc.ToDateTime(hYear, iyyar, dayIyyar + 1, 0, 0, 0, 0), "יום העצמאות")

        ' Yom Yerushalayim (Jerusalem Day)
        AddHoliday(hc.ToDateTime(hYear, iyyar, 28, 0, 0, 0, 0), "יום ירושלים")
    End Sub

    Private Function GetHolidayName(gDate As Date) As List(Of String)
        ' Retrieve holiday names for a given Gregorian date
        If holidayMap.ContainsKey(gDate.Date) Then
            Return holidayMap(gDate.Date)
        End If
        Return New List(Of String)
    End Function

    Private Sub AddHoliday(gDate As Date, name As String)
        ' Add a holiday to the holiday map
        Dim d = gDate.Date
        If Not holidayMap.ContainsKey(d) Then
            holidayMap(d) = New List(Of String)
        End If
        holidayMap(d).Add(name)
    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click

    End Sub
    ' End Region

    ' Region: Shabbat Times (Candle lighting and Havdalah via Zmanim.Net)
    Private Function GetShabbatTimes(gDate As Date) As Tuple(Of String, String)
        ' Step 1: Use the previous Friday if gDate is Shabbat
        Dim friday As Date = If(gDate.DayOfWeek = DayOfWeek.Saturday, gDate.AddDays(-1), gDate)

        ' Step 2: Wrap TimeZoneInfo into Zmanim's ITimeZone
        Dim windowsTz = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time")
        Dim zmanimTz As Zmanim.TimeZone.ITimeZone = New Zmanim.TimeZone.WindowsTimeZone(windowsTz)

        ' Step 3: Define GeoLocation
        Dim geo = New GeoLocation("Jerusalem", 31.7683, 35.2137, zmanimTz)

        ' Step 4: Get Zmanim
        Dim cal = New ZmanimCalendar(friday, geo)
        Dim candle As DateTime = cal.GetCandleLighting()
        Dim havdalah As DateTime = cal.GetTzais()

        ' Step 5: Return as formatted string tuple
        Return Tuple.Create(candle.ToString("HH:mm"), havdalah.ToString("HH:mm"))
    End Function
    ' End Region
End Class

' Region: FormMonthPicker Class
' This class represents the separate form for picking a Hebrew month and year.
Public Class FormMonthPicker
    Inherits Form

    Public Event MonthSelected(hebrewYear As Integer, hebrewMonth As Integer)

    Private yearCombo As ComboBox
    Private monthCombo As ComboBox
    Private confirmButton As Button
    Private hc As New HebrewCalendar()

    Public Sub New(currentYear As Integer, currentMonth As Integer)
        ' Configure form appearance
        Me.FormBorderStyle = FormBorderStyle.FixedToolWindow
        Me.StartPosition = FormStartPosition.Manual
        Me.ShowInTaskbar = False
        Me.Width = 200
        Me.Height = 150

        ' Initialize and position controls
        yearCombo = New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Left = 10, .Top = 10, .Width = 170}
        monthCombo = New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Left = 10, .Top = 40, .Width = 170}
        confirmButton = New Button With {.Text = "אישור", .Left = 10, .Top = 80, .Width = 170}

        ' Populate year combo box
        For y = 5700 To 5800 ' Range of years to display
            yearCombo.Items.Add(y)
        Next
        yearCombo.SelectedItem = currentYear

        ' Attach event handlers
        AddHandler yearCombo.SelectedIndexChanged, AddressOf UpdateMonthList
        AddHandler confirmButton.Click, AddressOf ConfirmSelection

        ' Add controls to the form
        Me.Controls.Add(yearCombo)
        Me.Controls.Add(monthCombo)
        Me.Controls.Add(confirmButton)

        ' Initial population of month list and selection
        UpdateMonthList(Nothing, Nothing)
        If currentMonth >= 1 AndAlso currentMonth <= monthCombo.Items.Count Then
            monthCombo.SelectedIndex = currentMonth - 1
        End If
    End Sub

    Private Sub UpdateMonthList(sender As Object, e As EventArgs)
        ' Update the month list based on the selected year (for leap years)
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
        ' Raise the MonthSelected event and close the form
        If yearCombo.SelectedItem Is Nothing OrElse monthCombo.SelectedIndex = -1 Then Return
        Dim y = CInt(yearCombo.SelectedItem)
        Dim m = monthCombo.SelectedIndex + 1
        RaiseEvent MonthSelected(y, m)
        Me.Close()
    End Sub
End Class
' End Region

