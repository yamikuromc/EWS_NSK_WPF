using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EWS_NSK_WPF
{
    public partial class MainWindow : Window
    {
        // Переменные для хранения данных текущего пользователя
        private bool isAuthorized = false;
        private string currentUserFullName = "Гость";
        private string currentUserPosition = "Гость";
        private bool isDarkTheme = false;

        public MainWindow()
        {
            InitializeComponent();
            UpdateGreeting();
        }

        // === ПРИВЕТСТВИЕ ПО ВРЕМЕНИ СУТОК ===
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

        // === ПРОВЕРКА АВТОРИЗАЦИИ (ОХРАННИК) ===
        private bool EnsureAuthorized()
        {
            if (!isAuthorized)
            {
                AuthWindow authWindow = new AuthWindow();

                if (authWindow.ShowDialog() == true)
                {
                    isAuthorized = true;
                    currentUserFullName = authWindow.AuthorizedFullName;
                    currentUserPosition = authWindow.AuthorizedPosition;

                    UserStatusCombo.Items.Clear();
                    UserStatusCombo.Items.Add(currentUserPosition);
                    UserStatusCombo.SelectedIndex = 0;

                    UpdateGreeting();
                }
            }
            return isAuthorized;
        }

        // === ПЕРЕКЛЮЧЕНИЕ ТЕМЫ ===
        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            isDarkTheme = !isDarkTheme;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            if (isDarkTheme)
            {
                DarkOverlay.Opacity = 0.75;
                SidebarBorder.Background = new SolidColorBrush(Color.FromArgb(230, 10, 10, 15));
                HeaderBorder.Background = new SolidColorBrush(Color.FromArgb(210, 10, 10, 15));
                FooterBorder.Background = new SolidColorBrush(Color.FromArgb(230, 5, 5, 10));
                TilesContainer.Opacity = 0.6;
                ThemeIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/sun_icon.png"));
                ThemeToggleButton.ToolTip = "Включить светлую тему";
            }
            else
            {
                DarkOverlay.Opacity = 0;
                SidebarBorder.Background = new SolidColorBrush(Color.FromArgb(200, 0, 40, 100));
                HeaderBorder.Background = new SolidColorBrush(Color.FromArgb(180, 0, 20, 60));
                FooterBorder.Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0));
                TilesContainer.Opacity = 1.0;
                ThemeIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/moon_icon.png"));
                ThemeToggleButton.ToolTip = "Включить тёмную тему";
            }
        }

        // === КЛИК ПО ПЛИТКАМ ===
        private void Tile_Click(object sender, MouseButtonEventArgs e)
        {
            Border clickedTile = sender as Border;
            if (clickedTile == null) return;

            string tag = clickedTile.Tag?.ToString();

            if (tag == "КАРТА ПГ")
            {
                if (EnsureAuthorized())
                {
                    MapWindow mapWindow = new MapWindow(currentUserFullName, currentUserPosition, "г. Новосибирск", WindowMode.View);
                    mapWindow.Owner = this;
                    mapWindow.Show();
                }
            }
            else if (tag == "ЖУРНАЛЫ")
            {
                if (EnsureAuthorized())
                {
                    JournalsWindow journalsWindow = new JournalsWindow(currentUserFullName, currentUserPosition, "г. Новосибирск", WindowMode.View);
                    journalsWindow.Owner = this;
                    journalsWindow.Show();
                }
            }
            else if (tag == "ОТЧЁТЫ")
            {
                if (EnsureAuthorized())
                {
                    ReportsWindow reportsWindow = new ReportsWindow(currentUserFullName, currentUserPosition, "г. Новосибирск", WindowMode.View);
                    reportsWindow.Owner = this;
                    reportsWindow.Show();
                }
            }
            else if (tag == "ПЛАНШЕТ ИНППВ")
            {
                if (EnsureAuthorized())
                {
                    TabletWindow tabletWindow = new TabletWindow(currentUserFullName, currentUserPosition, "г. Новосибирск", WindowMode.View);
                    tabletWindow.Owner = this;
                    tabletWindow.Show();
                }
            }
            else if (tag == "СПРАВОЧНИКИ")
            {
                if (EnsureAuthorized())
                {
                    ReferencesWindow refWindow = new ReferencesWindow();
                    refWindow.Owner = this;
                    refWindow.Show();
                }
            }
            else if (tag == "ОБМЕН ДАННЫМИ")
            {
                if (EnsureAuthorized())
                {
                    ExchangeWindow exchangeWindow = new ExchangeWindow(currentUserFullName, currentUserPosition, "г. Новосибирск", WindowMode.View);
                    exchangeWindow.Owner = this;
                    exchangeWindow.Show();
                }
            }
        }


        // === КЛИК ПО ПУНКТАМ БОКОВОГО МЕНЮ ===
        private void SidebarItem_Click(object sender, MouseButtonEventArgs e)
        {
            TextBlock clickedItem = sender as TextBlock;
            if (clickedItem == null) return;

            string tag = clickedItem.Tag?.ToString();
            string text = clickedItem.Text;

            // Если кликнули на любой пункт в разделе СПРАВОЧНИКИ
            if (tag == "СПРАВОЧНИКИ")
            {
                if (!EnsureAuthorized()) return;

                ReferencesWindow refWindow = new ReferencesWindow();
                refWindow.Owner = this;

                // Логика: какой пункт меню -> какую таблицу открыть
                // Имена таблиц должны точно совпадать с теми, что в базе SQLite!

                if (text == "Подразделения ППС")
                {
                    refWindow.LoadTable("tbl_Fire3_Unit", "Подразделения ППС");
                }
                else if (text == "Адресные объекты")
                {
                    refWindow.LoadTable("tbl_RS4_AddressObject", "Адресные объекты");
                }
                else if (text == "Характеристики")
                {
                    // Пока не знаем точное имя таблицы, используем tbl_EWS_Type как заглушку
                    refWindow.LoadTable("tbl_EWS_Type", "Характеристики");
                }
                else if (text == "Параметры проверок")
                {
                    refWindow.LoadTable("tbl_CheckEWS_StatusEWS", "Параметры проверок");
                }
                else if (text == "Балансодержатели")
                {
                    refWindow.LoadTable("tbl_EWS_Owner", "Балансодержатели");
                }
                else
                {
                    // Если кликнули на что-то другое в разделе Справочники
                    refWindow.LoadTable("tbl_EWS_Type", "Справочник");
                }

                refWindow.Show();
            }
            // Здесь потом добавим логику для других разделов (КАРТА ПГ, ЖУРНАЛЫ и т.д.)
        }

        // === ОБРАБОТЧИКИ ДЛЯ COMBOBOX (пока заглушки) ===
        private void UserStatusCombo_DropDownOpened(object sender, EventArgs e)
        {
            // Пока ничего не делаем, позже добавим логику смены пользователя
        }

        private void TerritoryCombo_DropDownOpened(object sender, EventArgs e)
        {
            // Пока ничего не делаем, позже добавим логику смены территории
        }

        // === ВЫХОД ===
        private void Exit_Click(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}