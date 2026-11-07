using EWS_NSK_WPF.Core;
using EWS_NSK_WPF.Models; // <-- ДОБАВЛЕНО: для доступа к классу FireStation
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.Sqlite; // <-- ДОБАВЛЕНО: для работы с БД

namespace EWS_NSK_WPF.ViewModels
{
    public class AuthViewModel : ViewModelBase
    {
        private string _login;
        private string _errorMessage;
        private bool _isSuccess;
        private bool _hasPassword;
        private DateTime? _selectedDate = DateTime.Today;

        public string Login
        {
            get => _login;
            set => SetField(ref _login, value);
        }

        public bool HasPassword
        {
            get => _hasPassword;
            set => SetField(ref _hasPassword, value);
        }

        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set => SetField(ref _selectedDate, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetField(ref _errorMessage, value);
        }

        public bool IsSuccess
        {
            get => _isSuccess;
            private set => SetField(ref _isSuccess, value);
        }

        public string AuthorizedFullName { get; private set; } = "Гость";
        public string AuthorizedPosition { get; private set; } = "Гость";
        public int UserRole { get; private set; }
        public int StaffId { get; private set; }

        // === НОВОЕ: Данные пожарной части пользователя ===
        public FireStation UserStation { get; private set; }

        public RelayCommand LoginCommand { get; }
        public RelayCommand CancelCommand { get; }

        public AuthViewModel()
        {
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        private bool CanExecuteLogin(object parameter)
        {
            return !string.IsNullOrWhiteSpace(Login) && HasPassword;
        }

        private void ExecuteLogin(object parameter)
        {
            if (parameter is PasswordBox passwordBox)
            {
                string password = passwordBox.Password;

                if (string.IsNullOrEmpty(password))
                {
                    ErrorMessage = "Введите пароль.";
                    return;
                }

                string passwordHash = DatabaseHelper.HashPassword(password);
                var authResult = DatabaseHelper.AuthenticateUser(Login, passwordHash);

                if (authResult.Success)
                {
                    AuthorizedFullName = authResult.FullName;
                    AuthorizedPosition = authResult.Position;
                    UserRole = authResult.Role;
                    StaffId = authResult.StaffId;

                    // === НОВОЕ: Загружаем пожарную часть пользователя ===
                    UserStation = LoadUserStation();

                    IsSuccess = true;
                }
                else
                {
                    ErrorMessage = "Неверный логин, пароль или учетная запись неактивна.";
                    passwordBox.Clear();
                    HasPassword = false;
                    IsSuccess = false;
                }
            }
        }

        // === НОВЫЙ МЕТОД: Загрузка данных пожарной части из БД ===
        private FireStation LoadUserStation()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "INPPV.db");
            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT fs.StationId, fs.StationName, fs.CenterLat, fs.CenterLng, fs.RadiusMeters, fs.Address
                    FROM tbl_Auth_Users u
                    JOIN FireStations fs ON u.StationId = fs.StationId
                    WHERE u.Login = @login";

                cmd.Parameters.AddWithValue("@login", Login);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new FireStation
                    {
                        StationId = Convert.ToInt32(reader["StationId"]),
                        StationName = reader["StationName"]?.ToString() ?? "Неизвестная часть",
                        CenterLat = Convert.ToDouble(reader["CenterLat"]),
                        CenterLng = Convert.ToDouble(reader["CenterLng"]),
                        RadiusMeters = Convert.ToDouble(reader["RadiusMeters"]),
                        Address = reader["Address"]?.ToString() ?? ""
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠ Ошибка загрузки ПСЧ: {ex.Message}");
            }

            return null; // Если у пользователя нет части, вернем null
        }

        private void ExecuteCancel(object parameter)
        {
            IsSuccess = false;
        }
    }
}