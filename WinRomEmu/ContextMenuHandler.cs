using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using System.Windows.Documents;
using System.Windows;

namespace EmulatorManager
{
    public class ContextMenuHandler
    {
        private const string MenuRegistryKey = @"Software\Classes\*\shell\EmulatorManager";
        private const string DefaultMenuRegistryKey = @"Software\Classes\*\shell\EmulatorManagerDefault";
        private const string FolderMenuRegistryKey = @"Software\Classes\Directory\shell\EmulatorManager";
        private readonly EmulatorDatabase _emulatorDb;
        private string logPath = "";

        public ContextMenuHandler()
        {
            _emulatorDb = new EmulatorDatabase();

            logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EmulatorManager",
                "debug.log"
            );
        }

        private void SetMenuIcon(RegistryKey key, string exePath, int emulatorId)
        {
            if (!File.Exists(exePath)) return;

            try
            {
                // The format ",0" means use the first (main) icon from the executable
                key.SetValue("Icon", $"\"{exePath}\",0");
            }
            catch
            {
                // If we can't set the direct exe path as icon (e.g., due to permissions),
                // we fall back to not setting an icon
            }
        }
        public async Task RegisterContextMenuAsync()
        {
            UnregisterContextMenu();
            var exePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WinRomEmu.exe"));
            var exeDir = Path.GetDirectoryName(exePath);

            // Register "Run with Emulator..." menu
            using (var key = Registry.CurrentUser.CreateSubKey(MenuRegistryKey))
            {
                key.SetValue("MUIVerb", "Run with Emulator...");
                key.SetValue("SubCommands", "");
                key.SetValue("Position", "Bottom");
            }

            // Register "Run with Default Emulator" menu
            using (var key = Registry.CurrentUser.CreateSubKey(DefaultMenuRegistryKey))
            {
                key.SetValue("MUIVerb", "Run with Default Emulator");
                key.SetValue("Position", "Bottom");


                using var commandKey = key.CreateSubKey("command");
                // Use /c for command and /d for drive change, /min to minimize the window
                var command = $"cmd /c start /b /min \"\" /d \"{exeDir}\" \"{exePath}\" rundefault \"%V\"";
                commandKey.SetValue("", command);
            }

            using (var cmdKey = Registry.CurrentUser.CreateSubKey($"{MenuRegistryKey}\\shell"))
            {
                // Add all emulators as sub-commands
                var emulators = await _emulatorDb.LoadEmulatorsAsync();
                foreach (var emulator in emulators)
                {
                    using var subKey = cmdKey.CreateSubKey(emulator.Id.ToString());
                    subKey.SetValue("MUIVerb", emulator.Name);
                    SetMenuIcon(subKey, emulator.Path, emulator.Id);

                    using var commandKey = subKey.CreateSubKey("command");
                    // Use start command with /b flag to run without creating a window
                    var command = $"cmd /c start /b /min \"\" /d \"{exeDir}\" \"{exePath}\" run \"%V\" {emulator.Id}";
                    commandKey.SetValue("", command);
                }
            }

            // Register folder context menu
            using (var key = Registry.CurrentUser.CreateSubKey(FolderMenuRegistryKey))
            {
                key.SetValue("MUIVerb", "Set Default Emulator");
                key.SetValue("SubCommands", "");
                key.SetValue("Position", "Bottom");
            }

            using (var cmdKey = Registry.CurrentUser.CreateSubKey($"{FolderMenuRegistryKey}\\shell"))
            {
                var emulators = await _emulatorDb.LoadEmulatorsAsync();
                foreach (var emulator in emulators)
                {
                    using var subKey = cmdKey.CreateSubKey(emulator.Id.ToString());
                    subKey.SetValue("MUIVerb", emulator.Name);
                    SetMenuIcon(subKey, emulator.Path, emulator.Id);

                    using var commandKey = subKey.CreateSubKey("command");
                    var command = $"cmd /c start /b /min \"\" /d \"{exeDir}\" \"{exePath}\" setdefault \"%V\" {emulator.Id}";
                    commandKey.SetValue("", command);
                }

                // Add "Remove Default Emulator" option
                using var removeKey = cmdKey.CreateSubKey("remove");
                removeKey.SetValue("MUIVerb", "Remove Default Emulator");
                using var removeCommandKey = removeKey.CreateSubKey("command");
                var removeCommand = $"cmd /c start /b /min \"\" /d \"{exeDir}\" \"{exePath}\" removedefault \"%V\"";
                removeCommandKey.SetValue("", removeCommand);
            }
        }
        public void UnregisterContextMenu()
        {
            try
            {
                // Remove all possible old registry paths
                var baseKeys = new[]
                {
                    MenuRegistryKey,
                    DefaultMenuRegistryKey,
                    FolderMenuRegistryKey,
                    @"Software\Classes\*\shell\EmulatorManager",
                    @"Software\Classes\Directory\shell\EmulatorManager"
                };

                foreach (var key in baseKeys)
                {
                    try
                    {
                        Registry.CurrentUser.DeleteSubKeyTree(key, false);
                    }
                    catch (Exception) { /* Continue if key doesn't exist */ }
                }

                // Also clean up any individual emulator entries that might exist
                var emulators = _emulatorDb.LoadEmulatorsAsync().GetAwaiter().GetResult();
                foreach (var emulator in emulators)
                {
                    try
                    {
                        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\*\shell\EmulatorManager_{emulator.Id}", false);
                        Registry.CurrentUser.DeleteSubKeyTree($@"Directory\shell\EmulatorManager_Default_{emulator.Id}", false);
                    }
                    catch (Exception) { /* Continue if key doesn't exist */ }
                }
            }
            catch (Exception) { /* Ignore if keys don't exist */ }
        }

        public async Task HandleCommandLineAsync(string[] args)
        {
            if (args.Length < 2) return;
            var command = args[0].ToLower();
            var path = args[1];
            Console.WriteLine(args);
            switch (command)
            {
                case "run" when args.Length >= 3:
                    var emulatorId = int.Parse(args[2]);
                    await RunEmulatorAsync(path, emulatorId);
                    break;

                case "rundefault":
                    await RunWithDefaultEmulatorAsync(path);
                    break;

                case "setdefault" when args.Length >= 3:
                    var defaultEmulatorId = int.Parse(args[2]);
                    await _emulatorDb.SetDefaultEmulatorAsync(path, defaultEmulatorId);
                    break;

                case "removedefault":
                    await _emulatorDb.RemoveDefaultEmulatorAsync(path);
                    break;
            }
        }

        private async Task RunWithDefaultEmulatorAsync(string romPath)
        {
            var folderPath = Path.GetDirectoryName(romPath);
            var defaultEmulatorId = await _emulatorDb.GetDefaultEmulatorAsync(folderPath!);

            if (defaultEmulatorId.HasValue && defaultEmulatorId.Value != 0)
            {
                File.AppendAllLines(logPath, new[] { defaultEmulatorId.Value.ToString() });
                await RunEmulatorAsync(romPath, defaultEmulatorId.Value);
            } else
            {
                MessageBox.Show("No default emulator has been configured for this directory/set. Please go to the parent directory and right-click to assign a default emulator to this directory/set.", "No Default Configured", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private async Task RunEmulatorAsync(string romPath, int emulatorId)
        {
            var emulators = await _emulatorDb.LoadEmulatorsAsync();
            var emulator = emulators.FirstOrDefault(e => e.Id == emulatorId);

            if (emulator == null)
            {
                MessageBox.Show("No emulator found, it's possible no emulatorId was provided.", "Internal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return; 
            }

            // Verify file extension matches
            var extension = Path.GetExtension(romPath).TrimStart('.').ToLower();
            var supportedExtensions = emulator.FileExtensions
                .Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ext => ext.Trim().TrimStart('.', '*').ToLower());

            if (!supportedExtensions.Contains(extension))
            {
                MessageBox.Show($"This is an unsupported file format. If you believe this to be incorrect, please use the GUI to update applicable file extensions for this emulator ({emulator.Name}).", "Unsupported File Format", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            // Replace macros in arguments string
            var arguments = emulator.ExecutionArguments
                .Replace("{exePath}", $"\"{emulator.Path}\"")
                .Replace("{romPath}", $"\"{romPath}\"");

            // Start the process with the emulator as the executable
            var startInfo = new ProcessStartInfo
            {
                FileName = emulator.Path,
                Arguments = arguments,
                UseShellExecute = false
            };

            Process.Start(startInfo);
        }

    }
}