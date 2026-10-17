using System.Windows;

namespace EWS_NSK_WPF
{
    public partial class ExchangeWindow : Window
    {
        public ExchangeWindow(string userName, string position, string accessLevel, WindowMode requestedMode)
        {
            InitializeComponent();
            UserInfoLabel.Text = $"Пользователь: {userName} ({position})";
            ThemeManager.ApplyThemeToWindow(this);

            WindowMode actualMode = requestedMode;

            if (requestedMode == WindowMode.Edit && accessLevel == "Operator")
            {
                actualMode = WindowMode.View;
                MessageBox.Show("У вашей роли нет прав на импорт/экспорт данных.\nОкно открыто в режиме просмотра.",
                                "Ограничение прав", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (actualMode == WindowMode.Edit)
            {
                PermissionsLabel.Text = "Режим: Импорт и экспорт данных";
                BtnImport.Visibility = Visibility.Visible;
                BtnExport.Visibility = Visibility.Visible;
            }
            else
            {
                PermissionsLabel.Text = "Режим: Только просмотр (операции с данными недоступны)";
                BtnImport.Visibility = Visibility.Collapsed;
                BtnExport.Visibility = Visibility.Collapsed;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}