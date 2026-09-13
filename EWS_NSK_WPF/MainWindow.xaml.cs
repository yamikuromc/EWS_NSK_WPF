using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

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
        // === ОБРАБОТЧИК КЛИКА ПО ПЛИТКЕ ===
        private void Tile_Click(object sender, MouseButtonEventArgs e)
        {
            if (!EnsureAuthorized()) return;

            if (sender is FrameworkElement element && element.Tag is string title)
            {
                string accessLevel = GetCurrentUserAccessLevel();

                switch (title)
                {
                    case "КАРТА ПГ":
                        new MapWindow(currentUserFullName, currentUserPosition, accessLevel, WindowMode.View) { Owner = this }.ShowDialog();
                        break;
                    case "ЖУРНАЛЫ":
                        new JournalsWindow(currentUserFullName, currentUserPosition, accessLevel, WindowMode.View) { Owner = this }.ShowDialog();
                        break;
                    case "ОТЧЁТЫ":
                        new ReportsWindow(currentUserFullName, currentUserPosition, accessLevel, WindowMode.View) { Owner = this }.ShowDialog();
                        break;
                    case "ПЛАНШЕТ ИНППВ":
                        new TabletWindow(currentUserFullName, currentUserPosition, accessLevel, WindowMode.View) { Owner = this }.ShowDialog();
                        break;
                    case "СПРАВОЧНИКИ":
                        new ReferencesWindow(currentUserFullName, currentUserPosition, accessLevel, WindowMode.View) { Owner = this }.ShowDialog();
                        break;
                    case "ОБМЕН ДАННЫМИ":
                        new ExchangeWindow(currentUserFullName, currentUserPosition, accessLevel, WindowMode.View) { Owner = this }.ShowDialog();
                        break;
                    default:
                        MessageBox.Show($"Раздел \"{title}\" пока не реализован.", "Раздел системы",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }
            }
        }

        // === ОБРАБОТЧИК КЛИКА ПО ПУНКТАМ БОКОВОГО МЕНЮ ===
        private void SidebarItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (!EnsureAuthorized()) return;

            if (sender is System.Windows.Controls.TextBlock textBlock && textBlock.Tag is string title)
            {
                string text = textBlock.Text;
                string accessLevel = GetCurrentUserAccessLevel();

                WindowMode mode;

                // Определяем режим ПО ТЕКСТУ подпункта (честно передаём запрос пользователя)
                if (text == "Просмотр")
                {
                    mode = WindowMode.View;
                }
                else if (text == "Редактирование" || text == "Формирование" || text == "Экспорт" ||
                         text == "Учёт ИНППВ" || text == "Проверки ПГ")
                {
                    // Эти пункты подразумевают редактирование — всегда передаём Edit
                    // Окно само проверит роль и покажет предупреждение если нужно
                    mode = WindowMode.Edit;
                }
                else
                {
                    // Функциональные подпункты (По территории, Импорт и т.д.)
                    // Для админов — Edit, для операторов — View (без предупреждения, это их естественный режим)
                    mode = (accessLevel == "Operator") ? WindowMode.View : WindowMode.Edit;
                }

                switch (title)
                {
                    case "КАРТА ПГ":
                        new MapWindow(currentUserFullName, currentUserPosition, accessLevel, mode) { Owner = this }.ShowDialog();
                        break;
                    case "ЖУРНАЛЫ":
                        new JournalsWindow(currentUserFullName, currentUserPosition, accessLevel, mode) { Owner = this }.ShowDialog();
                        break;
                    case "ОТЧЁТЫ":
                        new ReportsWindow(currentUserFullName, currentUserPosition, accessLevel, mode) { Owner = this }.ShowDialog();
                        break;
                    case "ПЛАНШЕТ ИНППВ":
                        new TabletWindow(currentUserFullName, currentUserPosition, accessLevel, mode) { Owner = this }.ShowDialog();
                        break;
                    case "СПРАВОЧНИКИ":
                        new ReferencesWindow(currentUserFullName, currentUserPosition, accessLevel, mode) { Owner = this }.ShowDialog();
                        break;
                    case "ОБМЕН ДАННЫМИ":
                        new ExchangeWindow(currentUserFullName, currentUserPosition, accessLevel, mode) { Owner = this }.ShowDialog();
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
                    UpdateGreeting(); 

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

        // Вспомогательный метод для получения уровня доступа текущего пользователя
        private string GetCurrentUserAccessLevel()
        {
            // В реальном проекте это будет делать Repository, но пока берем из нашего словаря
            // Мы ищем пользователя по его должности (Position), так как имя мы уже знаем
            // Для простоты теста, если это yamikuroMC или madara - вернем MainAdmin и т.д.

            if (currentUserPosition == "Главный Администратор") return "MainAdmin";
            if (currentUserPosition == "Администратор подразделения ГПС") return "UnitAdmin";
            return "Operator";
        }

        private void UpdateGreeting()
        {
            int hour = DateTime.Now.Hour;
            string timeOfDay;

            if (hour >= 5 && hour < 12)
                timeOfDay = "Доброе утро";
            else if (hour >= 12 && hour < 18)
                timeOfDay = "Добрый день";
            else if (hour >= 18 && hour < 23)
                timeOfDay = "Добрый вечер";
            else
                timeOfDay = "Доброй ночи";

            GreetingLabel.Text = $"{timeOfDay}, {currentUserFullName}!";
        }

        // === ПЕРЕКЛЮЧЕНИЕ ТЕМЫ ===
        private bool isDarkTheme = false;

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            isDarkTheme = !isDarkTheme;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            if (isDarkTheme)
            {
                // --- ТЁМНАЯ ТЕМА ---
                DarkOverlay.Opacity = 0.75;
                SidebarBorder.Background = new SolidColorBrush(Color.FromArgb(230, 10, 10, 15));
                HeaderBorder.Background = new SolidColorBrush(Color.FromArgb(210, 10, 10, 15));
                FooterBorder.Background = new SolidColorBrush(Color.FromArgb(230, 5, 5, 10));

                // Делаем плитки полупрозрачными (60% от исходной яркости)
                TilesContainer.Opacity = 0.6;

                // Меняем иконку на СОЛНЦЕ
                ThemeIcon.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Resources/sun_icon.png"));
                ThemeToggleButton.ToolTip = "Включить светлую тему";
            }
            else
            {
                // --- СВЕТЛАЯ ТЕМА ---
                DarkOverlay.Opacity = 0;
                SidebarBorder.Background = new SolidColorBrush(Color.FromArgb(200, 0, 40, 100));
                HeaderBorder.Background = new SolidColorBrush(Color.FromArgb(180, 0, 20, 60));
                FooterBorder.Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0));

                // Возвращаем плиткам полную яркость
                TilesContainer.Opacity = 1.0;

                // Меняем иконку на ЛУНУ
                ThemeIcon.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Resources/moon_icon.png"));
                ThemeToggleButton.ToolTip = "Включить тёмную тему";
            }
        }
    }
}