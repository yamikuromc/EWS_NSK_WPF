using System;
using EWS_NSK_WPF.Core;

namespace EWS_NSK_WPF.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private string _greeting = "Добро пожаловать!";
        private string _userFullName = "Гость";
        private string _userPosition = "Гость";
        private int _userRole = 0;
        private int _staffId = 0;
        private bool _isAuthorized = false;

        public string Greeting
        {
            get => _greeting;
            set => SetField(ref _greeting, value);
        }

        public string UserFullName
        {
            get => _userFullName;
            set => SetField(ref _userFullName, value);
        }

        public string UserPosition
        {
            get => _userPosition;
            set => SetField(ref _userPosition, value);
        }

        public int UserRole
        {
            get => _userRole;
            set => SetField(ref _userRole, value);
        }

        public int StaffId
        {
            get => _staffId;
            set => SetField(ref _staffId, value);
        }

        public bool IsAuthorized
        {
            get => _isAuthorized;
            set => SetField(ref _isAuthorized, value);
        }

        public MainViewModel()
        {
            UpdateGreeting();
        }

        public void UpdateGreeting()
        {
            int hour = DateTime.Now.Hour;
            string timeOfDay = hour switch
            {
                >= 5 and < 12 => "Доброе утро",
                >= 12 and < 18 => "Добрый день",
                >= 18 and < 23 => "Добрый вечер",
                _ => "Доброй ночи"
            };

            Greeting = $"{timeOfDay}, {UserFullName}!";
        }

        public void SetUserDetails(string fullName, string position, int role, int staffId)
        {
            UserFullName = fullName;
            UserPosition = position;
            UserRole = role;
            StaffId = staffId;
            IsAuthorized = true;
            UpdateGreeting();
        }
    }
}