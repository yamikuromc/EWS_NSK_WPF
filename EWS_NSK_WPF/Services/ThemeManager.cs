using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EWS_NSK_WPF.Views;      


namespace EWS_NSK_WPF.Services
{
    public static class ThemeManager
    {
        private static bool _isDarkTheme = false;
        public static event EventHandler ThemeChanged;
        private static string settingsFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "theme_settings.txt");

        // Свойство для получения/установки текущей темы
        public static bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                _isDarkTheme = value;
                SaveThemeSetting();
                ThemeChanged?.Invoke(null, EventArgs.Empty);   
            }
        }

        // Загружаем сохранённую тему при запуске
        static ThemeManager()
        {
            LoadThemeSetting();
        }

        // Применение темы к главному окну (MainWindow)
        public static void ApplyThemeToMainWindow(Window window)
        {
            if (window is MainWindow mainWindow)
            {
                if (_isDarkTheme)
                {
                    // Тёмная тема для MainWindow
                    mainWindow.DarkOverlay.Opacity = 0.75;
                    mainWindow.SidebarBorder.Background = new SolidColorBrush(Color.FromArgb(230, 10, 10, 15));
                    mainWindow.HeaderBorder.Background = new SolidColorBrush(Color.FromArgb(210, 10, 10, 15));
                    mainWindow.FooterBorder.Background = new SolidColorBrush(Color.FromArgb(230, 5, 5, 10));

                    mainWindow.ThemeIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/sun_icon_hd.png"));
                    mainWindow.ThemeLabel.Text = "Светлая тема";
                    mainWindow.ThemeLabel.Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0));

                    mainWindow.ChangeTileBackgroundOpacity(0.6);
                    mainWindow.ChangeTileTextColor(Colors.White);
                }
                else
                {
                    // Светлая тема для MainWindow 
                    mainWindow.Background = new SolidColorBrush(Color.FromRgb(245, 245, 250));
                    mainWindow.DarkOverlay.Opacity = 0;
                    mainWindow.SidebarBorder.Background = new SolidColorBrush(Color.FromArgb(200, 0, 40, 100));
                    mainWindow.HeaderBorder.Background = new SolidColorBrush(Color.FromArgb(180, 0, 20, 60));
                    mainWindow.FooterBorder.Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0));

                    mainWindow.ThemeIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/moon_icon_hd.png"));
                    mainWindow.ThemeLabel.Text = "Тёмная тема";
                    mainWindow.ThemeLabel.Foreground = new SolidColorBrush(Colors.White);

                    mainWindow.ChangeTileBackgroundOpacity(1.0);
                }
            }
        }

        // Применение темы к окну справочников
        public static void ApplyThemeToReferencesWindow(Window window)
        {
            if (window is ReferencesWindow refWindow)
            {
                if (_isDarkTheme)
                {
                    // Тёмный фон окна
                    refWindow.Background = new SolidColorBrush(Color.FromRgb(10, 10, 30));
                    ApplyDarkThemeToDataGrid(refWindow);
                }
                else
                {
                    // Светло-голубой фон окна
                    refWindow.Background = new SolidColorBrush(Colors.White);
                    ApplyLightThemeToDataGrid(refWindow);
                }
            }
        }

        // Тёмная тема для DataGrid
        private static void ApplyDarkThemeToDataGrid(ReferencesWindow refWindow)
        {
            DataGrid dataGrid = FindVisualChild<DataGrid>(refWindow);
            if (dataGrid == null) return;

            dataGrid.Background = new SolidColorBrush(Color.FromRgb(20, 20, 50));
            dataGrid.Foreground = new SolidColorBrush(Colors.White);
            dataGrid.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 100));
            
            var headerStyle = new Style(typeof(DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0, 60, 120))));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, new SolidColorBrush(Colors.Orange)));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(60, 60, 100))));
            dataGrid.ColumnHeaderStyle = headerStyle;

            var rowStyle = new Style(typeof(DataGridRow));
            rowStyle.Setters.Add(new Setter(DataGridRow.ForegroundProperty, new SolidColorBrush(Colors.White)));
            dataGrid.RowStyle = rowStyle;
        }

        // Светлая тема для DataGrid
        private static void ApplyLightThemeToDataGrid(ReferencesWindow refWindow)
        {
            DataGrid dataGrid = FindVisualChild<DataGrid>(refWindow);
            if (dataGrid == null) return;

            dataGrid.Background = new SolidColorBrush(Colors.White); // Сама таблица белая для контраста
            dataGrid.Foreground = new SolidColorBrush(Colors.Black);
            dataGrid.BorderBrush = new SolidColorBrush(Colors.Transparent);
            dataGrid.RowBackground = new SolidColorBrush(Colors.White);
            dataGrid.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(220, 235, 250)); // Светло-голубая полоска

            var headerStyle = new Style(typeof(DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0, 60, 120))));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, new SolidColorBrush(Colors.Orange)));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
            dataGrid.ColumnHeaderStyle = headerStyle;

            var rowStyle = new Style(typeof(DataGridRow));
            rowStyle.Setters.Add(new Setter(DataGridRow.ForegroundProperty, new SolidColorBrush(Colors.Black)));
            dataGrid.RowStyle = rowStyle;
        }

        // Вспомогательный метод: поиск элемента в визуальном дереве
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T found)
                    return found;

                T result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        // Применение темы к любому окну (базовая стилизация)
        public static void ApplyThemeToWindow(Window window)
        {
            if (_isDarkTheme)
            {
                window.Background = new SolidColorBrush(Color.FromRgb(20, 20, 40));
                SetAllTextColors(window, Colors.White);
            }
            else
            {
                window.Background = new SolidColorBrush(Color.FromRgb(245, 245, 250)); // Вернули светло-серый
                SetAllTextColors(window, Color.FromRgb(0, 40, 100)); // Тёмно-синий текст
            }
        }

        // Вспомогательный метод: красит все TextBlock в окне
        private static void SetAllTextColors(Window window, Color color)
        {
            void FindAndSetColor(DependencyObject parent)
            {
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                    if (child is TextBlock tb && tb.Foreground != Brushes.Orange)
                    {
                        tb.Foreground = new SolidColorBrush(color);
                    }

                    FindAndSetColor(child);
                }
            }

            FindAndSetColor(window.Content as DependencyObject);
        }

        // Сохранение настройки темы в файл
        private static void SaveThemeSetting()
        {
            try
            {
                File.WriteAllText(settingsFilePath, _isDarkTheme.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения темы: {ex.Message}");
            }
        }

        // Загрузка настройки темы из файла
        private static void LoadThemeSetting()
        {
            try
            {
                if (File.Exists(settingsFilePath))
                {
                    string content = File.ReadAllText(settingsFilePath).Trim();
                    _isDarkTheme = content.ToLower() == "true";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки темы: {ex.Message}");
                _isDarkTheme = false; // По умолчанию светлая тема
            }
        }

        // Применение темы к окну авторизации
        public static void ApplyThemeToAuthWindow(Window window)
        {
            if (window is AuthWindow authWindow)
            {
                if (_isDarkTheme)
                {
                    authWindow.Background = new SolidColorBrush(Color.FromRgb(30, 30, 50));

                    if (authWindow.FindName("LoginLabel") is TextBlock loginLabel)
                        loginLabel.Foreground = new SolidColorBrush(Colors.White);

                    if (authWindow.FindName("PasswordLabel") is TextBlock passwordLabel)
                        passwordLabel.Foreground = new SolidColorBrush(Colors.White);

                    if (authWindow.FindName("DateLabel") is TextBlock dateLabel)
                        dateLabel.Foreground = new SolidColorBrush(Colors.White);
                }
                else
                {
                    authWindow.Background = new SolidColorBrush(Color.FromRgb(245, 245, 250)); // Светло-серый
                    SetTextColorsInWindow(authWindow, Color.FromRgb(0, 40, 100));
                }
            }
        }

        // Вспомогательный метод: меняет цвет всех TextBlock в окне
        private static void SetTextColorsInWindow(Window window, Color color)
        {
            void FindAndSetColor(DependencyObject parent)
            {
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                    if (child is TextBlock tb && tb.Foreground != Brushes.Orange)
                    {
                        tb.Foreground = new SolidColorBrush(color);
                    }

                    FindAndSetColor(child);
                }
            }

            FindAndSetColor(window.Content as DependencyObject);
        }

        // Вспомогательный метод: меняет цвета текста в MapWindow
        private static void SetTextColorsInMapWindow(MapWindow window, Color textColor, Color headerColor)
        {
            void FindAndSetColor(DependencyObject parent)
            {
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                    if (child is TextBlock tb)
                    {
                        if (tb.FontWeight == FontWeights.Bold && tb.FontSize > 16)
                        {
                            tb.Foreground = new SolidColorBrush(headerColor);
                        }
                        else
                        {
                            tb.Foreground = new SolidColorBrush(textColor);
                        }
                    }

                    FindAndSetColor(child);
                }
            }

            FindAndSetColor(window.Content as DependencyObject);
        }

        // Применение темы специально для окна Отчетов (по аналогии со Справочниками)
        public static void ApplyThemeToReportsWindow(Window window)
        {
            bool isDark = IsDarkTheme;

            // Фон окна
            window.Background = isDark
                ? new SolidColorBrush(Color.FromRgb(10, 10, 30))
                : new SolidColorBrush(Color.FromRgb(245, 245, 245)); // #F5F5F5 как справочники

            // Левая панель типов отчётов
            if (window.FindName("LeftPanelBorder") is Border leftPanel)
            {
                leftPanel.Background = isDark
                    ? new SolidColorBrush(Color.FromRgb(20, 20, 50))
                    : new SolidColorBrush(Colors.White);
                leftPanel.BorderBrush = isDark
                    ? new SolidColorBrush(Color.FromRgb(90, 90, 122))
                    : new SolidColorBrush(Color.FromRgb(208, 208, 208));

                // Покраска содержимого левой панели (заголовок + кнопки)
                if (leftPanel.Child is StackPanel sp)
                {
                    var textColor = isDark ? Colors.White : Color.FromRgb(51, 51, 51);
                    foreach (var child in sp.Children)
                    {
                        if (child is TextBlock tb)
                            tb.Foreground = new SolidColorBrush(Colors.Orange);
                        else if (child is Button btn)
                            btn.Foreground = new SolidColorBrush(textColor);
                    }
                }
            }

            // Подложка вокруг листа А4
            if (window.FindName("SheetBackdropBorder") is Border backdrop)
            {
                backdrop.Background = isDark
                    ? new SolidColorBrush(Color.FromRgb(42, 42, 62))
                    : new SolidColorBrush(Color.FromRgb(232, 232, 232));
                backdrop.BorderBrush = isDark
                    ? new SolidColorBrush(Color.FromRgb(90, 90, 122))
                    : new SolidColorBrush(Color.FromRgb(208, 208, 208));
            }

            // Заголовок и подписи
            if (window.FindName("TitleTextBlock") is TextBlock title)
                title.Foreground = new SolidColorBrush(Colors.Orange);
            if (window.FindName("UserInfoLabel") is TextBlock user)
                user.Foreground = isDark
                    ? new SolidColorBrush(Colors.White)
                    : new SolidColorBrush(Color.FromRgb(51, 51, 51));
            if (window.FindName("PermissionsLabel") is TextBlock perm)
                perm.Foreground = new SolidColorBrush(Colors.Orange);
        }
    }
}