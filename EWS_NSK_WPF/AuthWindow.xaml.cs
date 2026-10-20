using System;
using System.Windows;
using System.Windows.Media;

namespace EWS_NSK_WPF
{
    public partial class AuthWindow : Window
    {
        public string AuthorizedFullName { get; private set; } = "Гость";
        public string AuthorizedPosition { get; private set; } = "Гость";
        public int UserRole { get; private set; }
        public int StaffId { get; private set; }

        public AuthWindow()
        {
            InitializeComponent();

            // Подписываемся на событие полной загрузки окна
            this.Loaded += AuthWindow_Loaded;
        }

        private void AuthWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyAuthTheme();
        }

        private void ApplyAuthTheme()
        {
            if (ThemeManager.IsDarkTheme)
            {
                // ТЁМНАЯ ТЕМА
                this.Background = new SolidColorBrush(Color.FromRgb(30, 30, 50));
                LoginLabel.Foreground = new SolidColorBrush(Colors.White);
                PasswordLabel.Foreground = new SolidColorBrush(Colors.White);
                DateLabel.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                // СВЕТЛАЯ ТЕМА
                this.Background = new SolidColorBrush(Color.FromRgb(245, 245, 250)); // Светло-серый фон

                // Делаем текст темно-синим, чтобы его было отлично видно на светлом фоне
                Color darkBlue = Color.FromRgb(0, 40, 100);
                LoginLabel.Foreground = new SolidColorBrush(darkBlue);
                PasswordLabel.Foreground = new SolidColorBrush(darkBlue);
                DateLabel.Foreground = new SolidColorBrush(darkBlue);
            }
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

            string passwordHash = DatabaseHelper.HashPassword(password);
            var authResult = DatabaseHelper.AuthenticateUser(login, passwordHash);

            if (authResult.Success)
            {
                AuthorizedFullName = authResult.FullName;
                AuthorizedPosition = authResult.Position;
                UserRole = authResult.Role;
                StaffId = authResult.StaffId;

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