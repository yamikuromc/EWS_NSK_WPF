using System;
using System.Windows;
using System.Windows.Input;

namespace EWS_NSK_WPF
{
    public partial class MainWindow : Window
    {
        // === Состояние авторизации ===
        private bool isAuthorized = false;
        private string currentUserFullName = "Гость";
        private string currentUserPosition = "Гость";

        public MainWindow()
        {
            InitializeComponent();
        }

        // === ОБРАБОТЧИК КЛИКА ПО ПЛИТКЕ ===
        private void Tile_Click(object sender, MouseButtonEventArgs e)
        {
            if (!EnsureAuthorized()) return;

            if (sender is FrameworkElement element && element.Tag is string title)
            {
                switch (title)
                {
                    case "КАРТА ПГ":
                        MessageBox.Show("Открытие раздела: КАРТА ПГ", "Раздел системы", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    case "ЖУРНАЛЫ":
                        MessageBox.Show("Открытие раздела: ЖУРНАЛЫ", "Раздел системы", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    case "ОТЧЁТЫ":
                        MessageBox.Show("Открытие раздела: ОТЧЁТЫ", "Раздел системы", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    case "ПЛАНШЕТ ИНППВ":
                        MessageBox.Show("Открытие раздела: ПЛАНШЕТ ИНППВ", "Раздел системы", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    case "СПРАВОЧНИКИ":
                        MessageBox.Show("Открытие раздела: СПРАВОЧНИКИ", "Раздел системы", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    case "ОБМЕН ДАННЫМИ":
                        MessageBox.Show("Открытие раздела: ОБМЕН ДАННЫМИ", "Раздел системы", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    default:
                        MessageBox.Show($"Раздел \"{title}\" пока не реализован.", "Раздел системы", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }
            }
        }

        // === ОБРАБОТЧИК КЛИКА ПО КНОПКЕ "ВЫХОД" ===
        private void Exit_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        // === ОБРАБОТЧИК КЛИКА ПО COMBOBOX "ПОЛЬЗОВАТЕЛЬ" ===
        private void UserStatusCombo_DropDownOpened(object sender, EventArgs e)
        {
            if (!isAuthorized)
            {
                UserStatusCombo.IsDropDownOpen = false;

                AuthWindow authWindow = new AuthWindow();
                authWindow.Owner = this;

                if (authWindow.ShowDialog() == true)
                {
                    isAuthorized = true;
                    currentUserFullName = authWindow.AuthorizedFullName;
                    currentUserPosition = authWindow.AuthorizedPosition;

                    UserStatusCombo.Items.Clear();
                    UserStatusCombo.Items.Add(currentUserPosition);
                    UserStatusCombo.SelectedIndex = 0;

                    MessageBox.Show($"Добро пожаловать, {currentUserFullName}!\nДолжность: {currentUserPosition}",
                                   "Авторизация выполнена",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Information);
                }
            }
            else
            {
                UserStatusCombo.IsDropDownOpen = false;
            }
        }

        // === ОБРАБОТЧИК КЛИКА ПО COMBOBOX "ТЕРРИТОРИЯ" ===
        private void TerritoryCombo_DropDownOpened(object sender, EventArgs e)
        {
            if (!isAuthorized)
            {
                TerritoryCombo.IsDropDownOpen = false;
                EnsureAuthorized();
            }
        }

        // === ПРОВЕРКА АВТОРИЗАЦИИ (открывает окно авторизации) ===
        private bool EnsureAuthorized()
        {
            if (isAuthorized) return true;

            AuthWindow authWindow = new AuthWindow();
            authWindow.Owner = this;

            if (authWindow.ShowDialog() == true)
            {
                isAuthorized = true;
                currentUserFullName = authWindow.AuthorizedFullName;
                currentUserPosition = authWindow.AuthorizedPosition;

                UserStatusCombo.Items.Clear();
                UserStatusCombo.Items.Add(currentUserPosition);
                UserStatusCombo.SelectedIndex = 0;

                MessageBox.Show($"Добро пожаловать, {currentUserFullName}!",
                               "Авторизация выполнена",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information);

                return true;
            }

            return false;
        }
    }
}