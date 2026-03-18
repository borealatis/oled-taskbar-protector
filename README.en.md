# 🖤 OLED Dimmer

A lightweight Windows utility to prevent taskbar burn-in on OLED monitors.

It places a semi-transparent black overlay directly on top of the taskbar. When the program is closed, it leaves no trace behind.

![Platform](https://img.shields.io/badge/platform-Windows-blue)
![Language](https://img.shields.io/badge/language-C%23-purple)
![Framework](https://img.shields.io/badge/.NET-Framework%204.x-blueviolet)
![License](https://img.shields.io/badge/license-MIT-green)

---

## Features

- Adjust dimming level with a slider (0% to 96%)
- Toggle on/off with a single click from the system tray
- Automatically hide the overlay when the mouse hovers over the taskbar (optional)
- Launch automatically with Windows (optional)
- All settings are saved automatically and restored on next launch
- Does not touch system files — only uses its own user-level Registry space
- Leaves no trace when closed

---

## Installation

### Requirements

- Windows 10 / 11
- .NET Framework 4.x (already included with Windows)

### Build

1. Download `OledDimmer.cs` and `build.bat` into the same folder
2. **Right-click** `build.bat` → **Run as administrator**
3. `OledDimmer.exe` will be compiled and launched automatically

---

## Usage

Once running, a small icon appears in the system tray (bottom right).

| Action | Description |
|---|---|
| Double-click | Open settings panel |
| Right-click | Toggle dimming or exit |

### Settings Panel

- **Dimming Level** — Drag the slider to adjust intensity, preview updates in real time
- **Disable / Enable Dimming** — Temporarily turn the overlay off
- **Hide when mouse is on taskbar** — Overlay disappears when hovering, reappears when leaving
- **Launch at Windows startup** — Automatically start with Windows
- **Quit** — Closes the program and removes the overlay

---

## How It Works

The overlay window is attached directly to the taskbar using the `SetParent` Windows API, making it a child window of the taskbar itself. This means:

- No z-order conflicts — nothing can push it behind the taskbar
- Clicks pass through normally — the taskbar works as usual
- Minimal resource usage — ~0% CPU at idle, ~8 MB RAM

Settings are saved to `HKEY_CURRENT_USER\SOFTWARE\OledDimmer`. No system files or Windows settings are modified.

---

## License

MIT License — free to use, modify, and distribute.
