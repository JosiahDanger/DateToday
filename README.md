<img width="128" height="128" alt="DateToday Logo" src="https://github.com/user-attachments/assets/19448ad7-3e0b-4579-b897-d04074c52365" />

This branch hosts DateToday 2.0, which is currently in development.

---

A [FOSS](https://en.wikipedia.org/wiki/Free_and_open-source_software) app for people who just want to see the current date and/or time directly on their desktop.

- Beautiful and lightweight
- No bullshit anti-features
- Quality software written the old-fashioned way

## Features

- Extensive date/time format configuration.
- Customise typeface family, size, weight; text alignment, colour, and shadow.
- Choose one of three gorgeous text rendering modes.
- Drag the widget to any monitor; it will never go off-screen.
- Anchor to your desktop any one of the widget's four corners. If the widget changes in size, the selected corner will remain fixed in place.
- Settings are saved and restored automatically, including where and on which monitor the widget is displayed.
- The app's theme adjusts automatically to that of your operating system.
- Supports high-DPI monitors.
- A [portable application](https://en.wikipedia.org/wiki/Portable_application). Just run the executable; no installation necessary.
- The executable is only 11 MB in size, and occupies about 65 MB of RAM while running.
- The groundwork has been laid for future translations into different languages.

## Privacy

- Absolutely no data-collection whatsoever. The app never even attempts to connect to the internet.
- This will never change in the future.

---

### Platform Support

I perform Windows builds, test them, and distribute them via GitHub. I want to do the same for Linux builds in the future. I anticipate that macOS and Linux builds will compile successfully, but I haven't tried yet.

### Under the Hood

- Avalonia 12
- .NET 11
- NativeAOT
- MVVM architecture
- XML source code documentation

### My Position on AI Use

This is not a [vibe-coded](https://en.wikipedia.org/wiki/Vibe_coding) app. The extent of my AI use is limited to code reviews and bug-fixes. AI was **not** used for source code generation.

### Special Thanks

DateToday 2.0 would not have been possible without the work of these two individuals.

- [PeaceShi](https://github.com/peaceshi)

  - Maintains statically-linked builds of ANGLE and Skia Sharp for Avalonia.
  - Provides an [example NativeAOT deployment configuration for Avalonia apps on the Windows x64 platform](https://github.com/peaceshi/Avalonia-NativeAOT-SingleFile/blob/main/MinimalAotSample/Directory.Build.props).

- [rosslight GmbH](https://github.com/rosslight)

  - Maintains the [Darp.CommunityToolkit.MvvmValidator](https://github.com/rosslight/Darp.CommunityToolkit.MvvmValidator) package.
  
