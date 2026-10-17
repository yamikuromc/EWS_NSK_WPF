using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EWS_NSK_WPF
{
    public partial class MainWindow : Window
    {
        private bool isAuthorized = false;
        private string currentUserFullName = "Гость";
        private string currentUserPosition = "Гость";
        private int currentUserRole = 0;       //  Роль пользователя
        private int currentStaffId = 0;        //  ID сотрудника
        
        public MainWindow()
        {
            InitializeComponent();
            UpdateGreeting();
            ApplyTheme();
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
                    currentUserRole = authWindow.UserRole;   // ← ДОБАВЛЕНО
                    currentStaffId = authWindow.StaffId;     // ← ДОБАВЛЕНО

                    UserStatusCombo.Items.Clear();
                    UserStatusCombo.Items.Add(currentUserPosition);
                    UserStatusCombo.SelectedIndex = 0;

                    UpdateGreeting();
                }
            }
            return isAuthorized;
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.IsDarkTheme = !ThemeManager.IsDarkTheme; // Переключаем через менеджер
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            ThemeManager.ApplyThemeToMainWindow(this); // Делегируем менеджеру
        }
        // ✅ НОВЫЙ МЕТОД: Меняет прозрачность ТОЛЬКО фона (Rectangle)
        internal void ChangeTileBackgroundOpacity(double opacity)
        {
            var backgrounds = new List<System.Windows.Shapes.Rectangle>
    {
        TileMapBackground,
        TileJournalsBackground,
        TileReportsBackground,
        TileTabletBackground,
        TileReferencesBackground,
        TileExchangeBackground
    };

            foreach (var bg in backgrounds)
            {
                if (bg != null)
                {
                    bg.Opacity = opacity;
                }
            }
        }
        // ✅ СУЩЕСТВУЮЩИЙ МЕТОД: Меняет цвет текста на плитках
        internal void ChangeTileTextColor(Color color)
        {
            var tileTextBlocks = new List<TextBlock>
            {
                TileMapTitle, TileMapDesc,
                TileJournalsTitle, TileJournalsDesc,
                TileReportsTitle, TileReportsDesc,
                TileTabletTitle, TileTabletDesc,
                TileReferencesTitle, TileReferencesDesc,
                TileExchangeTitle, TileExchangeDesc
            };

            foreach (var tb in tileTextBlocks)
            {
                if (tb != null)
                {
                    tb.Foreground = new SolidColorBrush(color);
                }
            }
        }

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
                    refWindow.SetUserRole(currentUserRole); 
                    refWindow.SetStaffId(currentStaffId);  
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

        private void SidebarItem_Click(object sender, MouseButtonEventArgs e)
        {
            TextBlock clickedItem = sender as TextBlock;
            if (clickedItem == null) return;

            string tag = clickedItem.Tag?.ToString();
            string text = clickedItem.Text;

            if (tag == "СПРАВОЧНИКИ")
            {
                if (!EnsureAuthorized()) return;

                ReferencesWindow refWindow = new ReferencesWindow();
                refWindow.Owner = this;
                refWindow.SetUserRole(currentUserRole);
                refWindow.SetStaffId(currentStaffId);

                // 1. Сначала показываем окно (оно отрисует белый фон и индикатор загрузки)
                refWindow.Show();

                // 2. Потом начинаем загружать данные (асинхронно, не вешая интерфейс)
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
                    refWindow.LoadTable("tbl_EWS_Type", "Справочник");
                }
            }
            else if (tag == "КАРТА ПГ")
            {
                if (!EnsureAuthorized()) return;
                string mode = (text == "Редактирование") ? "Edit" : "View";
                MapWindow mapWindow = new MapWindow(currentUserFullName, currentUserPosition, "г. Новосибирск",
                    mode == "Edit" ? WindowMode.Edit : WindowMode.View);
                mapWindow.Owner = this;
                mapWindow.Show();
            }
            else if (tag == "ЖУРНАЛЫ")
            {
                if (!EnsureAuthorized()) return;
                JournalsWindow journalsWindow = new JournalsWindow(currentUserFullName, currentUserPosition, "г. Новосибирск", WindowMode.View);
                journalsWindow.Owner = this;
                journalsWindow.Show();
            }
            else if (tag == "ОТЧЁТЫ")
            {
                if (!EnsureAuthorized()) return;
                ReportsWindow reportsWindow = new ReportsWindow(currentUserFullName, currentUserPosition, "г. Новосибирск", WindowMode.View);
                reportsWindow.Owner = this;
                reportsWindow.Show();
            }
            else if (tag == "ПЛАНШЕТ ИНППВ")
            {
                if (!EnsureAuthorized()) return;
                TabletWindow tabletWindow = new TabletWindow(currentUserFullName, currentUserPosition, "г. Новосибирск", WindowMode.View);
                tabletWindow.Owner = this;
                tabletWindow.Show();
            }
            else if (tag == "ОБМЕН ДАННЫМИ")
            {
                if (!EnsureAuthorized()) return;
                ExchangeWindow exchangeWindow = new ExchangeWindow(currentUserFullName, currentUserPosition, "г. Новосибирск", WindowMode.View);
                exchangeWindow.Owner = this;
                exchangeWindow.Show();
            }
        }

        private void UserStatusCombo_DropDownOpened(object sender, EventArgs e)
        {
        }

        private void TerritoryCombo_DropDownOpened(object sender, EventArgs e)
        {
        }

        private void Exit_Click(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}