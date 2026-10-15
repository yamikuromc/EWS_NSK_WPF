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
        private bool isDarkTheme = false;

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

                // В тёмной теме показываем солнце и текст "Светлая тема"
                ThemeIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/sun_icon_hd.png"));
                ThemeLabel.Text = "Светлая тема";
                ThemeLabel.Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // Оранжевый текст

                ChangeTileBackgroundOpacity(0.6);
                ChangeTileTextColor(Colors.White);
            }
            else
            {
                DarkOverlay.Opacity = 0;
                SidebarBorder.Background = new SolidColorBrush(Color.FromArgb(200, 0, 40, 100));
                HeaderBorder.Background = new SolidColorBrush(Color.FromArgb(180, 0, 20, 60));
                FooterBorder.Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0));

                // В светлой теме показываем луну и текст "Тёмная тема"
                ThemeIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/moon_icon_hd.png"));
                ThemeLabel.Text = "Тёмная тема";
                ThemeLabel.Foreground = new SolidColorBrush(Colors.White); // Белый текст

                ChangeTileBackgroundOpacity(1.0);
            }
        }

        // ✅ НОВЫЙ МЕТОД: Меняет прозрачность ТОЛЬКО фона (Rectangle)
        private void ChangeTileBackgroundOpacity(double opacity)
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
        private void ChangeTileTextColor(Color color)
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

                refWindow.Show();
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