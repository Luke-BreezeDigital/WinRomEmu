// Copyright (c) 2024 WinRomEmu
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Linq;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;

using WinRomEmu.Database.Sqlite;
using WinRomEmu.Models;
using WinRomEmu.ContextMenu;

namespace WinRomEmu
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private EmulatorConfig? _selectedEmulator;
        private EmulatorConfig? _editingEmulator;
        private readonly EmulatorDatabase? _database;
        private bool _isEditing;
        private bool _hasUnsavedChanges;

        public ObservableCollection<EmulatorConfig> Emulators { get; }
        public EmulatorConfig? SelectedEmulator
        {
            get => _editingEmulator ?? _selectedEmulator;
            set
            {
                UnsubscribeFromPropertyChanged(_editingEmulator);
                _editingEmulator = value;
                SubscribeToPropertyChanged(_editingEmulator);
                OnPropertyChanged(nameof(SelectedEmulator));
            }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
                OnPropertyChanged(nameof(IsEditing));
            }
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                _hasUnsavedChanges = value;
                OnPropertyChanged(nameof(HasUnsavedChanges));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            _database = new EmulatorDatabase();
            Emulators = new ObservableCollection<EmulatorConfig>();
            DataContext = this;
            EmulatorListView.ItemsSource = Emulators;
            Loaded += MainWindow_Loaded;
        }

        public async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeAsync();
        }
        private void SubscribeToPropertyChanged(EmulatorConfig? emulator)
        {
            if (emulator != null)
            {
                emulator.PropertyChanged += SelectedEmulator_PropertyChanged;
            }
        }

        private void UnsubscribeFromPropertyChanged(EmulatorConfig? emulator)
        {
            if (emulator != null)
            {
                emulator.PropertyChanged -= SelectedEmulator_PropertyChanged;
            }
        }
        private async Task InitializeAsync()
        {
            try
            {
                await _database!.InitializeDatabaseAsync();
                var emulators = await _database.LoadEmulatorsAsync();

                foreach (var emulator in emulators)
                {
                    Emulators.Add(emulator);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading emulators: {ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }


        private void SelectedEmulator_PropertyChanged(object? sender, PropertyChangedEventArgs? e)
        {
            HasUnsavedChanges = true;
        }

        private bool CheckUnsavedChanges()
        {
            if (HasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. These changes will be lost if you continue.\n\nDo you want to continue?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                return result == MessageBoxResult.Yes;
            }
            return true;
        }
        private void AddEmulator_Click(object sender, RoutedEventArgs e)
        {
            // Check for unsaved changes before proceeding
            if (!CheckUnsavedChanges())
            {
                return;
            }

            _selectedEmulator = null;

            SelectedEmulator = new EmulatorConfig
            {
                ExecutionArguments = "{romPath}"
            };

            IsEditing = true;
            HasUnsavedChanges = false;
        }

        private void EmulatorListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // If we have unsaved changes and we're selecting a different item
            if (HasUnsavedChanges && e.AddedItems.Count > 0 &&
                e.AddedItems[0] as EmulatorConfig != _selectedEmulator)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. These changes will be lost if you continue.\n\nDo you want to continue?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.No)
                {
                    // Revert the selection
                    EmulatorListView.SelectedItem = _selectedEmulator;
                    return;
                }
            }

            if (e.AddedItems.Count > 0)
            {
                _selectedEmulator = e.AddedItems[0] as EmulatorConfig;
                SelectedEmulator = _selectedEmulator!.Clone();
                IsEditing = true;
                HasUnsavedChanges = false;
                OnPropertyChanged(nameof(SelectedEmulator));
            }
            else
            {
                _selectedEmulator = null;
                _editingEmulator = null;
                IsEditing = false;
                HasUnsavedChanges = false;
                OnPropertyChanged(nameof(SelectedEmulator));
            }
        }

        private void BrowseEmulator_Click(object sender, RoutedEventArgs e)
        {
            if (_editingEmulator == null)
                return;

            var dialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select Emulator Executable",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                _editingEmulator.Path = dialog.FileName;
            }
        }

        private async void SaveEmulator_Click(object? sender, RoutedEventArgs? e)
        {
            if (_editingEmulator == null)
                return;

            if (!_editingEmulator.IsValid())
            {
                MessageBox.Show(
                    "Please correct all validation errors before saving.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            try
            {
                // Clean up file extensions
                var cleanedExtensions = _editingEmulator!.FileExtensions!
                    .Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(ext => ext.Trim().TrimStart('.', '*'))
                    .Where(ext => !string.IsNullOrWhiteSpace(ext));

                _editingEmulator.FileExtensions = string.Join(";", cleanedExtensions);

                // Save to database
                await _database!.SaveEmulatorAsync(_editingEmulator);

                // If this is a new emulator
                if (_selectedEmulator == null || !Emulators.Contains(_selectedEmulator))
                {
                    Emulators.Add(_editingEmulator);
                    _selectedEmulator = _editingEmulator;
                }
                else
                {
                    // Update the original emulator with the edited values
                    _selectedEmulator!.Name = _editingEmulator.Name;
                    _selectedEmulator!.Path = _editingEmulator.Path;
                    _selectedEmulator!.FileExtensions = _editingEmulator.FileExtensions;
                    _selectedEmulator!.ExecutionArguments = _editingEmulator.ExecutionArguments;
                }

                // Update the ListView
                EmulatorListView.Items.Refresh();
                EmulatorListView.SelectedItem = _selectedEmulator;
                HasUnsavedChanges = false;

                // Update the system context menus
                ContextMenuHandler ctx = new ContextMenuHandler();
                await ctx.RegisterContextMenuAsync();

                MessageBox.Show(
                    "Emulator configuration saved successfully.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error saving emulator: {ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        // Update the RemoveEmulator_Click method
        private async void RemoveEmulator_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmulator == null)
                return;

            var result = MessageBox.Show(
                $"Are you sure you want to remove the emulator '{_selectedEmulator.Name}'?",
                "Confirm Removal",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _database!.DeleteEmulatorAsync(_selectedEmulator.Id);
                    Emulators.Remove(_selectedEmulator);
                    _selectedEmulator = null;
                    _editingEmulator = null;
                    IsEditing = false;
                    HasUnsavedChanges = false;
                    OnPropertyChanged(nameof(SelectedEmulator));

                    // Update the system context menus
                    ContextMenuHandler ctx = new ContextMenuHandler();
                    await ctx.RegisterContextMenuAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error removing emulator: {ex.Message}",
                        "Database Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (HasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save before closing?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question
                );

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        SaveEmulator_Click(null, null);
                        break;
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }

            base.OnClosing(e);
        }

        private void DisabledOverlay_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var result = MessageBox.Show(
                "Would you like to setup a new emulator?",
                "New Emulator",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            switch (result)
            {
                case MessageBoxResult.Yes:
                    AddEmulator_Click(sender, e);
                    break;
            }

        }

        private void RunEmulator_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedEmulator != null && !string.IsNullOrEmpty(SelectedEmulator.Path) && File.Exists(SelectedEmulator.Path))
                Process.Start(SelectedEmulator.Path);
            else
                MessageBox.Show("Please ensure the emulator path is valid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}