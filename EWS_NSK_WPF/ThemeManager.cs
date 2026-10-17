using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EWS_NSK_WPF
{
    public static class ThemeManager
    {
        private static bool _isDarkTheme = false;
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

        // Применение темы к окну справочников (ReferencesWindow)
        public static void ApplyThemeToReferencesWindow(Window window)
        {
            if (window is ReferencesWindow refWindow)
            {
                if (_isDarkTheme)
                {
                    refWindow.Background = new SolidColorBrush(Color.FromRgb(10, 10, 30));
                    // Здесь можно добавить стилизацию элементов ReferencesWindow
                }
                else
                {
                    refWindow.Background = new SolidColorBrush(Color.FromRgb(0, 40, 100));
                }
            }
        }

        // Применение темы к любому окну (базовая стилизация)
        public static void ApplyThemeToWindow(Window window)
        {
            if (_isDarkTheme)
            {
                window.Background = new SolidColorBrush(Color.FromRgb(20, 20, 40));
            }
            else
            {
                window.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            }
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
    }
}