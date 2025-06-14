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
![Widget Screenshot](https://github.com/user-attachments/assets/60beb62d-827d-4ed2-a175-65bbc6fe57d3)

![Widget Settings Screenshot](https://github.com/user-attachments/assets/4345e90b-9e29-4db3-8792-324816e04347)

## Get Started

1. Ensure that the [x64 .NET Desktop Runtime 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) is installed on your computer.
2. Run 'DateToday.exe'. The widget will be displayed. You may right-click the widget to open a context menu. Select 'Widget Settings' for configuration options.
3. To customise the format in which the date and/or time is displayed, you may enter a valid date/time format string. See [this Microsoft Learn page](https://learn.microsoft.com/dotnet/standard/base-types/custom-date-and-time-format-strings).

### (Optional) Configure DateToday To Run Upon Signing in to Windows

1. Make a shortcut to 'DateToday.exe'.
2. Launch 'Run'.
3. Enter `shell:startup`. This will open an instance of File Explorer inside a folder named 'Startup'.
4. Move your shortcut into this folder.

### FAQ

1. *How may I omit the displayed ordinal day suffix?* ('st', 'nd', 'rd', 'th')

   Navigate to the field labelled 'Append ordinal suffix to day of month at position', and erase its contents.

2. *What's the deal with the 'Anchored corner' buttons?*

   #### TL;DR
   Just click the button corresponding to the corner of your desktop in which the widget lives. Okay, Jerry?

   *Tell me more.*

   The widget is secretly a rectangular window. Each of the buttons under 'Anchored corner' corresponds to one of its four corners. When the widget automatically resizes, the selected corner will remain fixed in place.

   If, for example, the widget lives in the bottom-right corner of your desktop, you will probably want to select the bottom-right button. This will allow the widget to grow towards the centre of your monitor.

3. *I messed up the date / time format.*

   lmao

## Planned Features
- Multi-monitor support
- Linux support
    
## Under the Hood
- This is a .NET 8.0 application built with Avalonia and ReactiveUI.
- The app follows the MVVM architechtural pattern.
- Persistence of settings is achieved through the ReactiveUI 'AutoSuspendHelper' class.
- Widget text is refreshed every minute, on the minute.

\
Should you have any queries or suggestions, feel free to contact me by email: joedangergithub.coherence596@passmail.net
