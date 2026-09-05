using System;
using System.Collections.Generic;
using System.Windows;

namespace EWS_NSK_WPF
{
    public partial class AuthWindow : Window
    {
        // === "База" пользователей (заглушка) ===
        private static readonly Dictionary<string, (string Password, string FullName, string Position)> Users =
            new Dictionary<string, (string, string, string)>(StringComparer.OrdinalIgnoreCase)
            {
                { "yamikuroMC", ("34521488", "Овчинникова Инесса Валерьевна", "Системный администратор") }
            };

        // Свойства, которые мы вернём в главное окно после успешного входа
        public string AuthorizedFullName { get; private set; }
        public string AuthorizedPosition { get; private set; }
        public string EnteredDate => DateBox.SelectedDate?.ToString("dd.MM.yyyy") ?? DateTime.Today.ToString("dd.MM.yyyy");

        public AuthWindow()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginBox.Text.Trim();
            string password = PasswordBox.Password; // В WPF у PasswordBox пароль берётся через свойство Password

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ErrorLabel.Text = "Введите логин и пароль.";
                return;
            }

            // Проверка логина и пароля
            if (Users.TryGetValue(login, out var user) && user.Password == password)
            {
                AuthorizedFullName = user.FullName;
                AuthorizedPosition = user.Position;

                // В WPF DialogResult имеет тип bool?
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                ErrorLabel.Text = "Неверный логин или пароль.";
                PasswordBox.Clear();
                LoginBox.Focus();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}