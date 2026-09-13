using System.Windows;

namespace EWS_NSK_WPF
{
    public partial class JournalsWindow : Window
    {
        public JournalsWindow(string userName, string position, string accessLevel, WindowMode requestedMode)
        {
            InitializeComponent();
            UserInfoLabel.Text = $"Пользователь: {userName} ({position})";

            // Умная логика прав доступа
            WindowMode actualMode = requestedMode;

            if (requestedMode == WindowMode.Edit && accessLevel == "Operator")
            {
                actualMode = WindowMode.View;
                MessageBox.Show("У вашей роли нет прав на редактирование.\nОкно открыто в режиме просмотра.",
                                "Ограничение прав", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (actualMode == WindowMode.Edit)
            {
                PermissionsLabel.Text = "Режим: Просмотр и редактирование записей";
                BtnAdd.Visibility = Visibility.Visible;
                BtnEdit.Visibility = Visibility.Visible;
            }
            else
            {
                PermissionsLabel.Text = "Режим: Только просмотр журналов";
                BtnAdd.Visibility = Visibility.Collapsed;
                BtnEdit.Visibility = Visibility.Collapsed;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}