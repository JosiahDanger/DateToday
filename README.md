# DateToday
Beautiful, highly configurable desktop widget that displays the current date and/or time

---

If you are here simply to download a Windows executable, please see [Releases](https://github.com/JosiahDanger/DateToday/releases/). I will add Linux support when I can.

You will need to install as a prerequisite the [x64 .NET Desktop Runtime 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

See [Get Started](https://github.com/JosiahDanger/DateToday/tree/master?tab=readme-ov-file#get-started) for further instructions on how to use the widget.
  
---

DateToday is a desktop widget that displays the current date and/or time in a programmable format. Settings may be adjusted through an intuitive, minimalist GUI. The app's theme will adapt automatically to that of your operating system.

\
**All of my apps are proudly:**
- Open-source under the MIT Licence
- Free forever
- Developed without the use of generative AI

\
![Widget Screenshot](https://github.com/user-attachments/assets/43626d79-c1b2-4a98-8f4c-03e79050b469)

![Widget Settings Screenshot](https://github.com/user-attachments/assets/23b8b084-291a-4eb9-a0f2-1eb2b60b651e)

## Get Started

1. Ensure that the [x64 .NET Desktop Runtime 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) is installed on your computer.
2. Run 'DateToday.exe'. The widget will be displayed. You may right-click the widget to open a context menu. Select 'Widget Settings' for configuration options.
3. To customise the format in which the date and/or time is displayed, you may enter a valid date/time format string. See [this Microsoft Learn page](https://learn.microsoft.com/dotnet/standard/base-types/custom-date-and-time-format-strings).
4. If you would prefer for the widget to omit display of the ordinal day suffix ('st', 'nd', 'rd', 'th'), you may simply erase the contents of its position field.

### (Optional) Configure DateToday To Run Upon Signing in to Windows

1. Make a shortcut to 'DateToday.exe'.
2. Launch 'Run'.
3. Enter `shell:startup`. This will open an instance of File Explorer inside a folder named 'Startup'.
4. Move your shortcut into this folder.

## Planned Features
- User may choose one of four corners at which to anchor text. 
(Currently working on this as of June 2025; having some difficulty.)
- Multi-monitor support
- Linux support
    
## Under the Hood
- This is a .NET 8.0 application built with Avalonia and ReactiveUI.
- The app follows the MVVM architechtural pattern.
- Persistence of settings is achieved through the ReactiveUI 'AutoSuspendHelper' class.
- Widget text is refreshed every minute, on the minute.

\
I am keen to receive feedback on this application, particularly on its codebase. Please let me know if you have any suggestions.

I can be contacted by email at: joedangergithub.coherence596@passmail.net
