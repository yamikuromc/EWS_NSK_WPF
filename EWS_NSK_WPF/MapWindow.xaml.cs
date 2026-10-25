using System.Windows;
using EWS_NSK_WPF.Helpers;
using System.Windows.Media;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using GMap.NET.WindowsPresentation;
using System.IO;

namespace EWS_NSK_WPF
{
    public partial class MapWindow : Window
    {
        public MapWindow(string userName, string position, string accessLevel, WindowMode requestedMode)
        {
            InitializeComponent();

            // === НАСТРОЙКА КАРТЫ С MBTILES ===

            // Загружаем EWSs_map файл
            string mbTilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "EWSs_map.db");

            if (File.Exists(mbTilesPath))
            {
                MBTilesProvider.Instance.LoadMBTilesFile(mbTilesPath);
                MainMap.MapProvider = MBTilesProvider.Instance;
            }
            else
            {
                UserInfoLabel.Text = "Ошибка: файл карты не найден";
                return;
            }

            // Настройки карты
            MainMap.Manager.Mode = AccessMode.CacheOnly; // Только локальный кэш
            MainMap.Position = new PointLatLng(55.0084, 82.9357); // Центр Новосибирска
            MainMap.MinZoom = 10;
            MainMap.MaxZoom = 18;
            MainMap.Zoom = 12;
            MainMap.DragButton = System.Windows.Input.MouseButton.Left;

            UserInfoLabel.Text = $"Пользователь: {userName} ({position})";

            this.Loaded += (s, e) => ApplyTheme();

            this.Closed += (s, e) =>
            {
                MainMap.MapProvider = null;
                MainMap.Dispose();
            };
        }
        // Метод применения темы к этому окну
        private void ApplyTheme()
        {
            if (ThemeManager.IsDarkTheme)
            {
                // ТЁМНАЯ ТЕМА
                this.Background = new SolidColorBrush(Color.FromRgb(20, 20, 40));
                MapTitleText.Foreground = new SolidColorBrush(Colors.Orange);
                UserInfoLabel.Foreground = new SolidColorBrush(Colors.White);
                PermissionsLabel.Foreground = new SolidColorBrush(Colors.LightGray);
            }
            else
            {
                // СВЕТЛАЯ ТЕМА - светло-серый фон, тёмный текст
                this.Background = new SolidColorBrush(Color.FromRgb(245, 245, 250)); // Светло-серый
                MapTitleText.Foreground = new SolidColorBrush(Colors.Orange);
                UserInfoLabel.Foreground = new SolidColorBrush(Color.FromRgb(0, 40, 100)); // Тёмно-синий
                PermissionsLabel.Foreground = new SolidColorBrush(Color.FromRgb(60, 60, 90)); // Тёмно-серый
            }
        }
        

        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}