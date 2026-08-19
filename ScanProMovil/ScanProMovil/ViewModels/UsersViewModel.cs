using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Microsoft.Data.Sqlite;
using ScanProMovil.Data.Entities;
using System.Collections.ObjectModel;

namespace ScanProMovil.ViewModels
{
    public partial class UsersViewModel : ObservableObject
    {
        private readonly string dbPath = Path.Combine(FileSystem.AppDataDirectory, "scanpro.db");
        private readonly string connectionString;

        [ObservableProperty]
        private string? entryNameUser = string.Empty;
       
        public ObservableCollection<User> Users { get; } = [];

        public UsersViewModel()
        {
            connectionString = $"Data Source={dbPath}";
            CreateDatabase();
            GetUsersFull();
            EntryNameUser = string.Empty;
        }

        public void CreateDatabase()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"CREATE TABLE IF NOT EXISTS USERS 
								(userId INTEGER PRIMARY KEY AUTOINCREMENT,
								UserName TEXT NOT NULL);";
            command.ExecuteNonQuery();
        }

        [RelayCommand]
        private async void InsertUsers(string name)
        {
            if (string.IsNullOrEmpty(name)) 
            {
                await Shell.Current.DisplayAlertAsync("Advertencia", "el usuario no puede ser vacio...", "OK.");
                return;
            }

            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"INSERT INTO users (UserName) VALUES ($name)";
            command.Parameters.AddWithValue("$name", name);
            await command.ExecuteNonQueryAsync();

            User user = new User()
            {
                UserId = Users.Count + 1,
                UserName = name
            };
            Users.Add(user);
            EntryNameUser = string.Empty;

            
            
            var toast = Toast.Make("usuario creado correctamente.", 
                ToastDuration.Short, 18);
            


            await toast.Show();

        }

        public void UpdateUsers(int userid, string newName)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"UPDATE users SET UserName = $newName WHERE userId = $userId";
            command.Parameters.AddWithValue("$newName", newName);
            command.Parameters.AddWithValue("$userId", userid);
            command.ExecuteNonQuery();
            GetUsersFull();
        }

        private void DeleteUsers(int userid)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"DELETE FROM users WHERE userId = $userId";
            command.Parameters.AddWithValue("$userId", userid);
            command.ExecuteNonQuery();
        }

        [RelayCommand]
        public void GetUsersFull()
        {
            Users.Clear();
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT UserId, UserName FROM Users ORDER BY UserId";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                Users.Add(new User
                {
                    UserId = reader.GetInt32(0),
                    UserName = reader.GetString(1)
                });
            }
        }
    }
}
