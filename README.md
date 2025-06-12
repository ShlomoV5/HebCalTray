### הוראות בעברית

1. להורדת התוכנה היכנסו לדף "[Releases](https://github.com/ShlomoV5/HebCalTray/releases)".
2. שימו את הקובץ על **שולחן העבודה** כדי שיהיה נגיש בקלות.
3. לחצו עליו פעמיים – האפליקציה תופיע ליד השעון (בשורת המשימות למטה). בהצבעה על הסמל יופיע התאריך העברי.
4. לחיצה ימנית על האייקון תפתח תפריט להצגת לוח השנה או יציאה.

---

# HebTray – Hebrew Calendar Tray App 🇮🇱🗓

![צילום מסך Screenshot](screenshot.png)

A lightweight Windows tray application that shows the **current Hebrew date**, and allows viewing a **monthly Hebrew calendar** by clicking the tray icon.

This project uses .NET WinForms and the built-in HebrewCalendar API, with zero internet requirement after installation.

---

## ✅ Features

- 🕎 Shows Hebrew date in system tray (tooltip shows full date)
- 📅 Pop-up Hebrew calendar with navigation
- 🖱️ Right-click menu: Show Calendar / Exit

---

## 📦 Download & Run

👉 [Download the latest version from the Releases tab](https://github.com/ShlomoV5/HebCalTray/releases)

### 🧠 Instructions (English)

1. Click the link above and download the latest `.exe` file from **Releases**.
2. Place the file on your **Desktop** for easy access.
3. **Double-click** the file – the app will start and appear as an icon near your clock (system tray).
4. **Right-click** the tray icon to show the calendar or exit.

📝 No installation required. Runs quietly in the background.

---


## 🛠 Developers

This project is written in **VB.NET (WinForms)**. To build locally:

```bash
git clone https://github.com/ShlomoV5/HebCalTray.git
```
Then open HebTray.sln in Visual Studio and build using the Release configuration.

GitHub Actions automatically builds and uploads .exe artifacts for every pull request and creates a downloadable release on merge to main.
