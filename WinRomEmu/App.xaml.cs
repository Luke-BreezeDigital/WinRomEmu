using EmulatorManager;
using SQLitePCL;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;

namespace WinRomEmu
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ContextMenuHandler _contextMenuHandler;

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Initialize SQLite
            Batteries_V2.Init();

            _contextMenuHandler = new ContextMenuHandler();

            if (e.Args.Length > 0)
            {
                // Handle command-line arguments
                await _contextMenuHandler.HandleCommandLineAsync(e.Args);
                Shutdown();
                return;
            }

            // Register context menu
            await _contextMenuHandler.RegisterContextMenuAsync();
            base.OnStartup(e);
        }
    }

}
