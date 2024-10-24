// Copyright (c) 2024 WinRomEmu
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace WinRomEmu.Models
{
    public class EmulatorConfig : INotifyPropertyChanged, IDataErrorInfo
    {
        private int _id;
        private string? _name;
        private string? _path;
        private string? _fileExtensions;
        private string? _executionArguments;

        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }
        public string? Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string? Path
        {
            get => _path;
            set
            {
                _path = value;
                OnPropertyChanged(nameof(Path));
            }
        }

        public string? FileExtensions
        {
            get => _fileExtensions;
            set
            {
                _fileExtensions = value;
                OnPropertyChanged(nameof(FileExtensions));
            }
        }

        public string? ExecutionArguments
        {
            get => _executionArguments;
            set
            {
                _executionArguments = value;
                OnPropertyChanged(nameof(ExecutionArguments));
            }
        }

        public string Error => null!;

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(Name):
                        if (string.IsNullOrWhiteSpace(Name))
                            return "Name is required";
                        if (!Regex.IsMatch(Name, @"[a-zA-Z0-9]"))
                            return "Name must contain at least one alphanumeric character";
                        break;

                    case nameof(Path):
                        if (string.IsNullOrWhiteSpace(Path))
                            return "Emulator path is required";
                        if (!System.IO.Path.HasExtension(Path) || !Path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            return "Path must point to an executable (.exe) file";
                        if (!File.Exists(Path))
                            return "Executable file does not exist";
                        break;

                    case nameof(FileExtensions):
                        if (string.IsNullOrWhiteSpace(FileExtensions))
                            return "At least one file extension is required";

                        var extensions = FileExtensions.Split(new[] { '\r', '\n', ';' },
                            StringSplitOptions.RemoveEmptyEntries)
                            .Select(ext => ext.Trim().TrimStart('.', '*'))
                            .Where(ext => !string.IsNullOrWhiteSpace(ext));

                        if (!extensions.Any())
                            return "At least one valid file extension is required";

                        if (extensions.Any(ext => !Regex.IsMatch(ext, @"^[a-zA-Z0-9]+$")))
                            return "Extensions must contain only alphanumeric characters";
                        break;

                    case nameof(ExecutionArguments):
                        if (string.IsNullOrWhiteSpace(ExecutionArguments))
                            return "Execution arguments are required";
                        if (!ExecutionArguments.Contains("{romPath}"))
                            return "Arguments must contain the {romPath} macro. If you are unsure, just enter {romPath}";
                        break;
                }
                return null!;
            }
        }

        public EmulatorConfig Clone()
        {
            return new EmulatorConfig
            {
                Id = Id,
                Name = Name,
                Path = Path,
                FileExtensions = FileExtensions,
                ExecutionArguments = ExecutionArguments
            };
        }

        public bool IsValid()
        {
            return string.IsNullOrEmpty(this[nameof(Name)])
                && string.IsNullOrEmpty(this[nameof(Path)])
                && string.IsNullOrEmpty(this[nameof(FileExtensions)])
                && string.IsNullOrEmpty(this[nameof(ExecutionArguments)]);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}