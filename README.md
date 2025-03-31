# DateToday
Beautiful, configurable desktop widget that displays the current date

---

If you are here simply to download a Windows executable, please see [Releases](https://github.com/JosiahDanger/DateToday/releases/). I will add Linux support when I can.

You will need to install as a prerequisite the [x64 .NET Desktop Runtime 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

---

DateToday is a desktop widget that displays the current date in a configurable format, featuring gorgeous text rendering. Settings may be adjusted through an intuitive GUI. Recommended for users of [ExplorerPatcher](https://github.com/valinet/ExplorerPatcher), [TranslucentTB](https://github.com/TranslucentTB/TranslucentTB), and [Wallpaper Engine](https://www.wallpaperengine.io/). The app's theme will adapt automatically to that of your operating system. Open-source under the MIT Licence, and free forever.

&nbsp;

![Widget Screenshot](https://github.com/user-attachments/assets/43626d79-c1b2-4a98-8f4c-03e79050b469)

![Settings Screenshot](https://github.com/user-attachments/assets/a7ecff73-3ed1-46c9-80a2-c3a9e0f57d0b)

&nbsp;

## Get Started

1. Ensure that the [x64 .NET Desktop Runtime 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) is installed on your computer.
2. Run 'DateToday.exe'. The widget will be displayed. You may right-click the widget to open a context menu. Select 'Widget Settings' for configuration options.
3. To customise the format in which the date is displayed, you may enter a valid date format string. See [this Microsoft Learn page](https://learn.microsoft.com/dotnet/standard/base-types/custom-date-and-time-format-strings).

### (Optional) Configure DateToday To Run Upon Signing in to Windows

1. Make a shortcut to 'DateToday.exe'.
2. Launch 'Run'.
3. Enter `shell:startup`. This will open an instance of File Explorer inside a folder named 'Startup'.
4. Move your shortcut into this folder.

## Planned Features
- Additional configuration options:
  - Option to override automatic black / white font colour with a custom colour
  - Text shadow
- Multi-monitor support
- Linux support
    
## Under the Hood
- This is a .NET 8.0 application built with Avalonia and ReactiveUI.
- The app follows the MVVM architechtural pattern.
- Persistence of settings is achieved through the ReactiveUI 'AutoSuspendHelper' class.
- Widget text is refreshed every minute, on the minute.

## More Screenshots

![Screenshot 2025-03-22 030209](https://github.com/user-attachments/assets/ffcec9f1-1173-4d93-b04f-ba182a7b81a1)

![Screenshot 2025-03-22 031345](https://github.com/user-attachments/assets/ba30fe7b-6408-438c-b4e2-518ddedcc1ca)

I am keen to receive feedback on this application, particularly on its codebase. Please let me know if you have any suggestions.
