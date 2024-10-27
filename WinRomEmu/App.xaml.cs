// Copyright (c) 2024 WinRomEmu
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using SQLitePCL;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Threading.Tasks;
using WinRomEmu.ContextMenu;
using WinRomEmu.Database.Sqlite;

namespace WinRomEmu
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ContextMenuHandler? _contextMenuHandler;
        private MainWindow? _mainWindow;

        protected override async void OnStartup(StartupEventArgs e)
        {
            var _database = new EmulatorDatabase();
            await _database!.InitializeDatabaseAsync();
            try
            {
                // Initialize SQLite
                Batteries_V2.Init();
                _contextMenuHandler = new ContextMenuHandler();

                // Handle command-line arguments if present
                if (e.Args.Length > 0)
                {
                    await HandleCommandLineMode(e.Args);
                    return;
                }

                await StartGuiMode();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred during startup: {ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }

            base.OnStartup(e);
        }

        private async Task HandleCommandLineMode(string[] args)
        {
            try
            {
                await _contextMenuHandler!.HandleCommandLineAsync(args);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error processing command: {ex.Message}",
                    "Command Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                Shutdown();
            }
        }

        private async Task StartGuiMode()
        {
            // Register context menu before showing the window
            await _contextMenuHandler!.RegisterContextMenuAsync();

            // Create and show the main window on the UI thread
            Dispatcher.Invoke(() =>
            {
                _mainWindow = new MainWindow();
                MainWindow = _mainWindow; // Set the application's main window
                _mainWindow.Show();
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Cleanup resources if needed
            _contextMenuHandler = null;
            _mainWindow = null;
            base.OnExit(e);
        }
    }
}