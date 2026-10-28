using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using EWS_NSK_WPF.Helpers;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;

namespace EWS_NSK_WPF
{
    public partial class MapWindow : Window
    {
        public MapWindow(string userName, string position, string accessLevel, WindowMode requestedMode)
        {
            InitializeComponent();

            // === НАСТРОЙКА КАРТЫ ===

            // 1. Устанавливаем правильный User-Agent (ОБЯЗАТЕЛЬНО!)
            GMapProvider.UserAgent = "EWS_NSK_WPF/1.0 (Fire Hydrant System; mailto:yaroslavtseva@example.com)";

            // 2. Пробуем загрузить MBTiles файл
            string mbTilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NewLoadMap.mbtiles");

            if (File.Exists(mbTilesPath))
            {
                try
                {
                    MBTilesProvider.Instance.LoadMBTilesFile(mbTilesPath);
                    // Используем MBTiles как основной провайдер
                    MainMap.MapProvider = MBTilesProvider.Instance;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки MBTiles: {ex.Message}");
                    // Если MBTiles не загрузился — используем стандартный OSM
                    MainMap.MapProvider = GMapProviders.OpenStreetMap;
                }
            }
            else
            {
                // Если файла нет — используем стандартный OSM
                MainMap.MapProvider = GMapProviders.OpenStreetMap;
            }

            // 3. РЕЖИМ: ServerAndCache — сначала ищем локально, потом качаем
            // Это ключевой момент! Даже с MBTiles это помогает
            MainMap.Manager.Mode = AccessMode.ServerAndCache;

            // 4. Настройки карты
            MainMap.Position = new PointLatLng(55.0084, 82.9357); // Центр Новосибирска
            MainMap.MinZoom = 10;  // Можно отдалить для обзора города
            MainMap.MaxZoom = 18;  // Максимальное приближение
            MainMap.Zoom = 14;     // Стартовый зум
            MainMap.DragButton = System.Windows.Input.MouseButton.Left;
            MainMap.LevelsKeepInMemory = 5;

            UserInfoLabel.Text = $"Пользователь: {userName} ({position})";

            this.Loaded += (s, e) => ApplyTheme();

            // 5. Правильное освобождение ресурсов
            this.Closed += (s, e) =>
            {
                try
                {
                    MainMap.MapProvider = null;
                    MainMap.Dispose();
                    MBTilesProvider.Instance?.Close();
                }
                catch { }
            };
        }

        private void ApplyTheme()
        {
            if (ThemeManager.IsDarkTheme)
            {
                this.Background = new SolidColorBrush(Color.FromRgb(20, 20, 40));
                MapTitleText.Foreground = new SolidColorBrush(Colors.Orange);
                UserInfoLabel.Foreground = new SolidColorBrush(Colors.White);
                PermissionsLabel.Foreground = new SolidColorBrush(Colors.LightGray);
            }
            else
            {
                this.Background = new SolidColorBrush(Color.FromRgb(245, 245, 250));
                MapTitleText.Foreground = new SolidColorBrush(Colors.Orange);
                UserInfoLabel.Foreground = new SolidColorBrush(Color.FromRgb(0, 40, 100));
                PermissionsLabel.Foreground = new SolidColorBrush(Color.FromRgb(60, 60, 90));
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}