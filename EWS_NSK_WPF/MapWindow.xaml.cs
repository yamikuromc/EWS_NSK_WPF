using System.Windows;

namespace EWS_NSK_WPF
{
    public partial class MapWindow : Window
    {
        public MapWindow(string userName, string position, string accessLevel, WindowMode requestedMode)
        {
            InitializeComponent();

            UserInfoLabel.Text = $"Пользователь: {userName} ({position})";

            // === УМНАЯ ЛОГИКА ПРАВ ДОСТУПА ===
            WindowMode actualMode = requestedMode;

            // Если запросили редактирование, но у пользователя роль Оператора — принудительно ставим просмотр
            if (requestedMode == WindowMode.Edit && accessLevel == "Operator")
            {
                actualMode = WindowMode.View;
                MessageBox.Show("У вашей роли нет прав на редактирование.\nОкно открыто в режиме просмотра.",
                                "Ограничение прав", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Настраиваем интерфейс в зависимости от ИТОГОВОГО режима
            if (actualMode == WindowMode.Edit)
            {
                PermissionsLabel.Text = "Режим: Редактирование";
                BtnAdd.Visibility = Visibility.Visible;
                BtnEdit.Visibility = Visibility.Visible;
            }
            else // View
            {
                PermissionsLabel.Text = "Режим: Только просмотр";
                BtnAdd.Visibility = Visibility.Collapsed;
                BtnEdit.Visibility = Visibility.Collapsed;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}