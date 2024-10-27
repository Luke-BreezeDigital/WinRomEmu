using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Threading.Tasks;
using System.Linq;
using WinRomEmu.Models;
using WinRomEmu.Database.Sqlite;

namespace WinRomEmu
{
    public partial class DefaultEmulatorManagerWindow : Window
    {
        private readonly EmulatorDatabase _database;
        private ObservableCollection<DefaultEmulatorEntry> _defaultEmulators;
        private ObservableCollection<EmulatorConfig> _emulators;
        private DefaultEmulatorEntry _currentlyEditing;

        public DefaultEmulatorManagerWindow(EmulatorDatabase database)
        {
            InitializeComponent();
            _database = database;
            _defaultEmulators = new ObservableCollection<DefaultEmulatorEntry>();
            _emulators = new ObservableCollection<EmulatorConfig>();
            DefaultEmulatorsListView.ItemsSource = _defaultEmulators;
            EmulatorComboBox.ItemsSource = _emulators;

            Loaded += DefaultEmulatorManagerWindow_Loaded;
        }

        private async void DefaultEmulatorManagerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            // Load emulators for the ComboBox
            var emulators = await _database.LoadEmulatorsAsync();
            _emulators.Clear();
            foreach (var emulator in emulators)
            {
                _emulators.Add(emulator);
            }

            // Load default emulators
            _defaultEmulators.Clear();
            await foreach (var folderPath in _database.GetDefaultEmulatorPathsAsync())
            {
                var emulatorId = await _database.GetDefaultEmulatorAsync(folderPath);
                if (emulatorId.HasValue)
                {
                    var emulator = emulators.FirstOrDefault(e => e.Id == emulatorId.Value);
                    if (emulator != null)
                    {
                        _defaultEmulators.Add(new DefaultEmulatorEntry
                        {
                            FolderPath = folderPath,
                            EmulatorId = emulatorId.Value,
                            EmulatorName = emulator.Name
                        });
                    }
                }
            }
        }

        private void DefaultEmulatorsListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selectedItem = DefaultEmulatorsListView.SelectedItem as DefaultEmulatorEntry;
            if (selectedItem != null)
            {
                // Enter edit mode
                _currentlyEditing = selectedItem;
                FolderPathTextBox.Text = selectedItem.FolderPath;
                EmulatorComboBox.SelectedItem = _emulators.FirstOrDefault(em => em.Id == selectedItem.EmulatorId);

                // Show edit mode buttons
                AddButton.Visibility = Visibility.Collapsed;
                SaveButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Visible;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentlyEditing != null)
            {
                var folderPath = FolderPathTextBox.Text?.Trim();
                var selectedEmulator = EmulatorComboBox.SelectedItem as EmulatorConfig;

                if (ValidateInput(folderPath, selectedEmulator))
                {
                    // If folder path changed, remove old and add new
                    if (_currentlyEditing.FolderPath != folderPath)
                    {
                        await _database.RemoveDefaultEmulatorAsync(_currentlyEditing.FolderPath);
                    }

                    await _database.SetDefaultEmulatorAsync(folderPath, selectedEmulator.Id);
                    await LoadData();
                    ClearForm();
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            _currentlyEditing = null;
            FolderPathTextBox.Text = string.Empty;
            EmulatorComboBox.SelectedIndex = -1;
            DefaultEmulatorsListView.SelectedItem = null;

            // Show add mode buttons
            AddButton.Visibility = Visibility.Visible;
            SaveButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Collapsed;
        }

        private async void AddDefault_Click(object sender, RoutedEventArgs e)
        {
            var folderPath = FolderPathTextBox.Text?.Trim();
            var selectedEmulator = EmulatorComboBox.SelectedItem as EmulatorConfig;

            if (ValidateInput(folderPath, selectedEmulator))
            {
                await _database.SetDefaultEmulatorAsync(folderPath, selectedEmulator.Id);
                await LoadData();
                ClearForm();
            }
        }

        private bool ValidateInput(string folderPath, EmulatorConfig selectedEmulator)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                System.Windows.MessageBox.Show("Please enter a folder path.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (selectedEmulator == null)
            {
                System.Windows.MessageBox.Show("Please select an emulator.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }
        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select Default Emulator Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                FolderPathTextBox.Text = dialog.FolderName;
            }
        }
        private async void RemoveDefault_Click(object sender, RoutedEventArgs e)
        {
            var defaultEntry = ((System.Windows.Controls.Button)sender).DataContext as DefaultEmulatorEntry;
            if (defaultEntry != null)
            {
                var result = System.Windows.MessageBox.Show(
                    "Are you sure you want to remove this default emulator?",
                    "Confirm Removal",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await _database.RemoveDefaultEmulatorAsync(defaultEntry.FolderPath);
                    await LoadData();
                    ClearForm();
                }
            }
        }
    }

    public class DefaultEmulatorEntry
    {
        public string FolderPath { get; set; }
        public int EmulatorId { get; set; }
        public string EmulatorName { get; set; }
    }
}