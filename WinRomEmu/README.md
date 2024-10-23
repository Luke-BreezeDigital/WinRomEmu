# Emulator Manager

Emulator Manager is a Windows application that simplifies the management and launching of emulators. It provides a centralized interface to configure multiple emulators and adds convenient context menu integration for launching ROMs directly from Windows Explorer.

## Features

- 🎮 Manage multiple emulators in one place
- 🖱️ Windows Explorer context menu integration
- 📁 Directory-specific default emulator settings
- 🚀 Quick launch ROMs with right-click
- 🎯 Smart file extension filtering
- 🛠️ Customizable launch arguments
- 🌗 Dark mode interface
- ⚠️ Comprehensive error handling

## Installation

1. Download the latest release from the releases page
2. Extract the ZIP file to your desired location
3. Run `WinRomEmu.exe`
4. The application will automatically set up the necessary context menu integrations

## Usage

### Adding an Emulator

1. Click "Add New Emulator"
2. Fill in the following details:
   - Name: A descriptive name for the emulator
   - Emulator Path: Path to the emulator executable (use Browse button)
   - File Extensions: Supported ROM file extensions (e.g., nes, smc, gba)
     - Can be separated by semicolons or newlines
     - Extensions can be entered with or without dots/asterisks (both `.nes` and `nes` work)
   - Arguments: Command line arguments for launching ROMs

### Argument Macros

The following macros are available for the Arguments field:

- `{romPath}` - Full path to the ROM file (automatically wrapped in quotes)
- `{exePath}` - Full path to the emulator executable (automatically wrapped in quotes)

### Command Line Interface

The application supports the following command-line arguments:

```
WinRomEmu.exe <command> <path> [emulatorId]
```

Commands:
- `run <romPath> <emulatorId>` - Launch specific ROM with the specified emulator
- `rundefault <romPath>` - Launch ROM with the directory's default emulator
- `setdefault <directoryPath> <emulatorId>` - Set default emulator for a directory
- `removedefault <directoryPath>` - Remove default emulator setting for a directory

### Context Menu Integration

After configuring emulators, you'll have access to these context menu options:

1. **Run with Emulator...** - Shows submenu of all configured emulators
2. **Run with Default Emulator** - Launches using the folder's default emulator
3. **Set Default Emulator** (on folders) - Configure default emulator for a directory
4. **Remove Default Emulator** (on folders) - Remove default emulator setting

## Error Messages

The application provides clear error messages for common situations:

- **Unsupported File Format**: Appears when trying to open a ROM with an emulator that doesn't support its extension
- **No Default Emulator**: Appears when trying to use "Run with Default Emulator" on a folder without a default set
- **Missing Emulator**: Appears if the selected emulator configuration cannot be found

## Example Configurations

### RetroArch
```
Name: RetroArch SNES
Path: C:\RetroArch\retroarch.exe
Extensions: smc;sfc
Arguments: -L "cores\snes9x_libretro.dll" {romPath}
```

### PCSX2
```
Name: PCSX2
Path: C:\PCSX2\pcsx2.exe
Extensions: iso;bin
Arguments: {romPath}
```

### Dolphin
```
Name: Dolphin
Path: C:\Program Files\Dolphin\Dolphin.exe
Extensions: gcm;iso;wbfs;ciso;gcz;wia;rvz;dol;elf
Arguments: --batch --exec={romPath}
```

## System Requirements

- Windows 10 or later
- .NET 6.0 Runtime or later
- Administrator privileges (for context menu integration)

## Technical Notes

- Uses SQLite for configuration storage
- Registry modifications for context menu integration are made in HKEY_CURRENT_USER
- Command-line operations run minimized to avoid console window pop-up
- File extension matching is case-insensitive
- All emulator processes are launched with UseShellExecute=false for better process handling

## Troubleshooting

### Context Menu Not Appearing
- Ensure you have administrator privileges
- Try removing and re-adding the emulator configuration
- Restart Windows Explorer (taskkill /f /im explorer.exe)

### Emulator Won't Launch
- Verify the emulator path is correct and accessible
- Check that the ROM file extension matches the configured extensions
- Ensure launch arguments are correctly formatted
- Check the debug.log file in %AppData%\EmulatorManager for details

### File Extension Errors
- Make sure extensions are properly separated (use semicolons or newlines)
- Remove any leading dots or asterisks from extensions
- Extensions are case-insensitive, so both `.ISO` and `.iso` will work

## License

This project is licensed under the MIT License - see the LICENSE file for details.