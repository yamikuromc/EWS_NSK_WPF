using System.Windows;

namespace EWS_NSK_WPF
{
    public partial class TabletWindow : Window
    {
        public TabletWindow(string userName, string position, string accessLevel, WindowMode requestedMode)
        {
            InitializeComponent();
            UserInfoLabel.Text = $"Пользователь: {userName} ({position})";

            WindowMode actualMode = requestedMode;

            if (requestedMode == WindowMode.Edit && accessLevel == "Operator")
            {
                actualMode = WindowMode.View;
                MessageBox.Show("У вашей роли нет прав на формирование планшетов.\nОкно открыто в режиме просмотра.",
                                "Ограничение прав", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (actualMode == WindowMode.Edit)
            {
                PermissionsLabel.Text = "Режим: Просмотр и формирование планшетов";
                BtnCreate.Visibility = Visibility.Visible;
            }
            else
            {
                PermissionsLabel.Text = "Режим: Только просмотр планшетов";
                BtnCreate.Visibility = Visibility.Collapsed;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}