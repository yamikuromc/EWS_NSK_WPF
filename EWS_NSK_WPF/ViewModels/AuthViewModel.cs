using EWS_NSK_WPF.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EWS_NSK_WPF.ViewModels
{
    public class AuthViewModel : ViewModelBase
    {
        private string _login;
        private string _errorMessage;
        private bool _isSuccess;
        private bool _hasPassword;
        private DateTime? _selectedDate = DateTime.Today; // Дата по умолчанию

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

        private void ExecuteCancel(object parameter)
        {
            IsSuccess = false;
        }
    }
}