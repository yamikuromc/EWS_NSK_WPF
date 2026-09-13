using System.Windows;

namespace EWS_NSK_WPF
{
    public partial class ReportsWindow : Window
    {
        public ReportsWindow(string userName, string position, string accessLevel, WindowMode requestedMode)
        {
            InitializeComponent();
            UserInfoLabel.Text = $"Пользователь: {userName} ({position})";

            WindowMode actualMode = requestedMode;

            if (requestedMode == WindowMode.Edit && accessLevel == "Operator")
            {
                actualMode = WindowMode.View;
                MessageBox.Show("У вашей роли нет прав на формирование отчётов.\nОкно открыто в режиме просмотра.",
                                "Ограничение прав", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (actualMode == WindowMode.Edit)
            {
                PermissionsLabel.Text = "Режим: Формирование и экспорт отчётов";
                BtnGenerate.Visibility = Visibility.Visible;
                BtnExport.Visibility = Visibility.Visible;
            }
            else
            {
                PermissionsLabel.Text = "Режим: Только просмотр готовых отчётов";
                BtnGenerate.Visibility = Visibility.Collapsed;
                BtnExport.Visibility = Visibility.Collapsed;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}