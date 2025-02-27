# DateToday
Beautiful, configurable desktop widget that displays the current date

---

If you are here simply to download a Windows executable, please see [Releases](https://github.com/JosiahDanger/DateToday/releases/).

You will need to install as a prerequisite the [x64 .NET Desktop Runtime 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

---

DataToday is a desktop widget that displays the current date in a configurable format, featuring gorgeous text rendering. Settings may be adjusted through an intuitive GUI. Recommended for users of [ExplorerPatcher](https://github.com/valinet/ExplorerPatcher), [TranslucentTB](https://github.com/TranslucentTB/TranslucentTB), and [Wallpaper Engine](https://www.wallpaperengine.io/). Open-source under the MIT Licence, and free forever.

&nbsp;

![Widget Screenshot](https://github.com/user-attachments/assets/525f2c08-3557-40b5-a44a-c110decd5f09)

![Settings Screenshot](https://github.com/user-attachments/assets/5e984148-0cd7-4915-9420-658080a0bd57)

&nbsp;

# Get Started

1. Ensure that the [x64 .NET Desktop Runtime 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) is installed on your computer.
2. Run 'DateToday.exe'. The widget will be displayed. You may right-click the widget to open a context menu. Select 'Widget Settings' to customise its font and position on your desktop. The app's theme will adapt automatically to that of your Windows installation.
3. (Optional) Configure DateToday to run upon signing in to Windows:
  
    1. Make a shortcut to 'DateToday.exe'.
    2. Launch 'Run'.
    3. Enter `shell:startup`. This will open an instance of File Explorer inside a folder named 'Startup'.
    4. Move your shortcut into this folder.

# Planned Features
- Additional configuration options:
  - Option to override automatic black / white font colour with a custom colour
  - Text shadow
- Multi-monitor support
- Linux support
    
# Under the Hood
- This is a .NET 8.0 application built with Avalonia and ReactiveUI.
- The app follows the MVVM architechtural pattern.
- Persistence of settings is achieved through the ReactiveUI 'AutoSuspendHelper' class.
- Widget text is refreshed every minute, on the minute.

I am keen to receive feedback on this application, particularly on its codebase. Please let me know if you have any suggestions.
