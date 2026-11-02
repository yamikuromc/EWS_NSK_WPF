using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EWS_NSK_WPF.ViewModels;

namespace EWS_NSK_WPF
{
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
            ApplyTheme();

            if (DataContext is AuthViewModel vm)
            {
                vm.PropertyChanged += Vm_PropertyChanged;
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AuthViewModel vm)
            {
                vm.HasPassword = !string.IsNullOrEmpty(((PasswordBox)sender).Password);
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                PasswordBox.Focus();
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                LoginButton.Focus();

                if (DataContext is AuthViewModel vm && vm.LoginCommand.CanExecute(PasswordBox))
                {
                    vm.LoginCommand.Execute(PasswordBox);
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ApplyTheme()
        {
            string themePath = ThemeManager.IsDarkTheme
                ? "Themes/DarkTheme.xaml"
                : "Themes/LightTheme.xaml";

            var themeDict = new ResourceDictionary { Source = new Uri(themePath, UriKind.Relative) };
            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(themeDict);
        }

        private void Vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AuthViewModel.IsSuccess))
            {
                if (DataContext is AuthViewModel vm)
                {
                    this.DialogResult = vm.IsSuccess;
                    this.Close();
                }
            }
        }

        public string AuthorizedFullName => ((AuthViewModel)DataContext).AuthorizedFullName;
        public string AuthorizedPosition => ((AuthViewModel)DataContext).AuthorizedPosition;
        public int UserRole => ((AuthViewModel)DataContext).UserRole;
        public int StaffId => ((AuthViewModel)DataContext).StaffId;
    }
}