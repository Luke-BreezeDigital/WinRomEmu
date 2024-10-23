// EmulatorDatabase.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Linq;

namespace EmulatorManager
{
    public class EmulatorDatabase
    {
        private readonly string _dbPath;
        private const string DbFileName = "emulators.db";

        public EmulatorDatabase()
        {
            _dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EmulatorManager",
                DbFileName
            );

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath));
        }

        private SqliteConnection GetConnection()
        {
            return new SqliteConnection($"Data Source={_dbPath}");
        }

        public async Task InitializeDatabaseAsync()
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Emulators (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Path TEXT NOT NULL,
                    FileExtensions TEXT NOT NULL,
                    ExecutionArguments TEXT NOT NULL
                )";

            await command.ExecuteNonQueryAsync();

            command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS DefaultEmulators (
                    FolderPath TEXT PRIMARY KEY,
                    EmulatorId INTEGER NOT NULL,
                    FOREIGN KEY(EmulatorId) REFERENCES Emulators(Id)
                )";

            await command.ExecuteNonQueryAsync();
        }
        public async Task SetDefaultEmulatorAsync(string folderPath, int emulatorId)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO DefaultEmulators (FolderPath, EmulatorId)
                VALUES (@folderPath, @emulatorId)";

            command.Parameters.AddWithValue("@folderPath", folderPath);
            command.Parameters.AddWithValue("@emulatorId", emulatorId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<int?> GetDefaultEmulatorAsync(string folderPath)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                WITH RECURSIVE
                FolderPath(Path) AS (
                    SELECT @folderPath
                    UNION ALL
                    SELECT substr(Path, 1, instr(substr(Path, 1, length(Path)-1), '/'))
                    FROM FolderPath
                    WHERE Path != ''
                )
                SELECT EmulatorId
                FROM DefaultEmulators
                WHERE FolderPath IN (SELECT Path FROM FolderPath)
                ORDER BY length(FolderPath) DESC
                LIMIT 1";

            command.Parameters.AddWithValue("@folderPath", folderPath);

            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value ? null : Convert.ToInt32(result);
        }

        public async Task RemoveDefaultEmulatorAsync(string folderPath)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM DefaultEmulators WHERE FolderPath = @folderPath";
            command.Parameters.AddWithValue("@folderPath", folderPath);

            await command.ExecuteNonQueryAsync();
        }
        public async IAsyncEnumerable<string> GetDefaultEmulatorPathsAsync()
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT FolderPath FROM DefaultEmulators";
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var folderPath = reader.GetString(0);
                yield return folderPath;
            }
        }

        public async Task<List<EmulatorConfig>> LoadEmulatorsAsync()
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Emulators";

            var emulators = new List<EmulatorConfig>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                emulators.Add(new EmulatorConfig
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Path = reader.GetString(2),
                    FileExtensions = reader.GetString(3),
                    ExecutionArguments = reader.GetString(4)
                });
            }

            return emulators;
        }

        public async Task SaveEmulatorAsync(EmulatorConfig emulator)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            if (emulator.Id == 0)
            {
                // Insert new emulator
                command.CommandText = @"
                    INSERT INTO Emulators (Name, Path, FileExtensions, ExecutionArguments)
                    VALUES (@name, @path, @extensions, @args);
                    SELECT last_insert_rowid();";
            }
            else
            {
                // Update existing emulator
                command.CommandText = @"
                    UPDATE Emulators 
                    SET Name = @name, 
                        Path = @path, 
                        FileExtensions = @extensions, 
                        ExecutionArguments = @args
                    WHERE Id = @id";

                command.Parameters.AddWithValue("@id", emulator.Id);
            }

            command.Parameters.AddWithValue("@name", emulator.Name);
            command.Parameters.AddWithValue("@path", emulator.Path);
            command.Parameters.AddWithValue("@extensions", emulator.FileExtensions);
            command.Parameters.AddWithValue("@args", emulator.ExecutionArguments);

            if (emulator.Id == 0)
            {
                // Get the new ID for inserted emulator
                emulator.Id = Convert.ToInt32(await command.ExecuteScalarAsync());
            }
            else
            {
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task DeleteEmulatorAsync(int id)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Emulators WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            await command.ExecuteNonQueryAsync();
        }
    }
}