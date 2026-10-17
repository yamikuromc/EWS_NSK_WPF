using System;
using System.Windows;

namespace EWS_NSK_WPF
{
    public partial class AuthWindow : Window
    {
        // Свойства, которые мы вернём в главное окно после успешного входа
        public string AuthorizedFullName { get; private set; } = "Гость";
        public string AuthorizedPosition { get; private set; } = "Гость";
        public int UserRole { get; private set; }
        public int StaffId { get; private set; }

        public AuthWindow()
        {
            InitializeComponent();
            ThemeManager.ApplyThemeToWindow(this);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ErrorLabel.Text = "Введите логин и пароль.";
                return;
            }

            // 1. Хешируем введенный пароль
            string passwordHash = DatabaseHelper.HashPassword(password);

            // 2. Проверяем данные в базе
            var authResult = DatabaseHelper.AuthenticateUser(login, passwordHash);

            if (authResult.Success)
            {
                // 3. Сохраняем данные пользователя
                AuthorizedFullName = authResult.FullName;
                AuthorizedPosition = authResult.Position;
                UserRole = authResult.Role;
                StaffId = authResult.StaffId; // ← ДОБАВЛЕНО: сохраняем ID сотрудника

                // Для отладки (можно потом убрать)
                System.Diagnostics.Debug.WriteLine($"Вход успешен: {AuthorizedFullName}, Роль: {UserRole}, Staff_ID: {StaffId}");

                this.DialogResult = true;
                this.Close();
            }
            else
            {
                ErrorLabel.Text = "Неверный логин, пароль или учетная запись неактивна.";
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