using System.Windows;
using System.Windows.Media;

namespace EWS_NSK_WPF
{
    public partial class MapWindow : Window
    {
        public MapWindow(string userName, string position, string accessLevel, WindowMode requestedMode)
        {
            InitializeComponent();

            // Заполняем информацию о пользователе
            UserInfoLabel.Text = $"Пользователь: {userName} ({position})";

            // === УМНАЯ ЛОГИКА ПРАВ ДОСТУПА ===
            WindowMode actualMode = requestedMode;

            // ... здесь остальной твой код с правами доступа ...

            // Применяем тему после загрузки окна
            this.Loaded += (s, e) => ApplyTheme();
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