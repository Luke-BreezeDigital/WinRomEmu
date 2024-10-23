# WinRomEmu

WinRomEmu is a Windows application that simplifies the management and launching of your emulators. It provides a centralized interface to configure multiple emulators and adds convenient context menu integration for launching ROMs directly from Windows Explorer.

## Features

- 🎮 Manage multiple emulators in one place
- 🖱️ Windows Explorer context menu integration
- 📁 Directory-specific default emulator settings
- 🚀 Quick launch ROMs with right-click
- 🎯 Smart file extension filtering
- 🛠️ Customizable launch arguments
- 🌗 Dark mode interface
- ⚠️ Comprehensive error handling and validation
- 💾 Persistent configurations using SQLite

## Installation

1. Download the latest release from the releases page
2. Extract the ZIP file to your desired location
3. Run `WinRomEmu.exe`
4. The application will automatically set up necessary context menu integrations

## Configuration Storage

WinRomEmu stores its configurations in:
- `%AppData%\WinRomEmu\emulators.db` - SQLite database for emulator configurations
- `%AppData%\WinRomEmu\debug.log` - Debug log file

## Usage

### Adding an Emulator

1. Click "Add New Emulator"
2. Fill in the following details:
   - Name: A descriptive name for the emulator (must contain alphanumeric characters)
   - Emulator Path: Path to the emulator executable (use Browse button)
   - File Extensions: Supported ROM file extensions (e.g., nes, smc, gba)
     - Can be separated by semicolons or newlines
     - Extensions can be entered with or without dots/asterisks
     - Must contain only alphanumeric characters
   - Arguments: Command line arguments for launching ROMs (must include {romPath})

### Argument Macros

The following macros are available for the Arguments field:

- `{romPath}` - Full path to the ROM file (automatically wrapped in quotes)
- `{exePath}` - Full path to the emulator executable (automatically wrapped in quotes)

### Context Menu Options

After configuring emulators, you'll have access to these context menu options:

1. **Run with Emulator...** - Shows submenu of all configured emulators
2. **Run with Default Emulator** - Launches using the folder's default emulator
3. **Set Default Emulator** (on folders) - Configure default emulator for a directory
4. **Remove Default Emulator** (on folders) - Remove default emulator setting

### Validation Rules

The application enforces the following validation rules:

- **Name**: Must contain at least one alphanumeric character
- **Path**: Must be a valid, existing .exe file
- **File Extensions**: Must be alphanumeric, can be separated by newlines or semicolons
- **Arguments**: Must contain the {romPath} macro

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

## Error Messages

The application provides clear error messages for common situations:

- **Validation Errors**: Displays specific requirements for each field
- **Unsupported File Format**: Appears when trying to open a ROM with an incompatible emulator
- **No Default Emulator**: Appears when trying to use "Run with Default Emulator" on a folder without a default set
- **Database Errors**: Detailed messages for any database-related issues
- **Missing Emulator**: Appears if the selected emulator configuration cannot be found

## Technical Details

- Built with .NET 8.0
- Uses SQLite for configuration storage
- Registry modifications made in HKEY_CURRENT_USER
- Dark mode UI using WPF
- Implements INotifyPropertyChanged for real-time UI updates
- Comprehensive data validation
- Async/await pattern for database operations

## System Requirements

- Windows 10 or later
- .NET 8.0 Runtime or later
- Administrator privileges (for context menu integration)

## Troubleshooting

### Context Menu Not Appearing
- Ensure you have administrator privileges
- Check the Windows Registry under HKCU\Software\Classes
- Restart Windows Explorer

### Emulator Won't Launch
- Verify the emulator path is correct and accessible
- Check that the ROM file extension matches the configured extensions
- Ensure launch arguments are correctly formatted
- Check the debug.log file in %AppData%\WinRomEmu

### File Extension Errors
- Make sure extensions are properly separated (use semicolons or newlines)
- Remove any leading dots or asterisks from extensions
- Use only alphanumeric characters

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.