# DateToday
Beautiful, configurable desktop widget that displays the current date

---
**TL;DR**

Do you simply [want a Windows executable](https://old.reddit.com/r/github/comments/1at9br4/i_am_new_to_github_and_i_have_lots_to_say/?share_id=rjJKZS1aIO04c9zK5J3vL)? Please see [Releases](https://github.com/JosiahDanger/DateToday/releases/), and download 'DateToday.Windows.x64.zip'.

You will need to install as a prerequisite the [x64 .NET Desktop Runtime 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

---

![Screenshot](https://github.com/user-attachments/assets/b65bb8d5-ca72-4070-8e96-0f724f11c899)

![Screenshot](https://github.com/user-attachments/assets/7c39f31c-9ac6-448a-a6e5-41cb816f10ca)

Do you take pride in your gorgeous, overly customised Windows desktop? Perhaps you use tools like [ExplorerPatcher](https://github.com/valinet/ExplorerPatcher), or [TranslucentTB](https://github.com/TranslucentTB/TranslucentTB)?

Put a cherry on top of your ['rice'](https://www.reddit.com/r/unixporn/comments/45l5if/what_is_the_etymology_of_the_word_rice/): a date display widget for the modern minimalist. Just run the executable; no bullshit.

&nbsp;

# Get Started
1. Run 'DateToday.exe'. The widget will be displayed. You may right-click the widget to open a context menu. Select 'Widget Settings' to customise its font and position on your desktop. The app's theme will adapt automatically to that of your Windows installation.
2. (Optional) Configure DateToday to run upon signing in to Windows:
  
    1. Make a shortcut to 'DateToday.exe'.
    2. Launch 'Run'.
    3. Enter `shell:startup`. This will open an instance of File Explorer, and navigate to a folder named 'Startup'.
    4. Move your shortcut into this folder.

# Planned Features
- Additional configuration options:
  - Custom date format string
  - Font size and weight
  - Option to override automatic black / white font colour with a custom colour
- Multi-monitor support
- Linux support
    
# Under the Hood
- This is a .NET 8.0 application built with Avalonia and ReactiveUI.
- The app follows the MVVM architechtural pattern.
- Persistence of settings is achieved through the ReactiveUI 'AutoSuspendHelper' class.
