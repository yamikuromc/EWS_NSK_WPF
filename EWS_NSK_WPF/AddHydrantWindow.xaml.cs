using System;
using System.Windows;
using EWS_NSK_WPF.Helpers;
using EWS_NSK_WPF.ViewModels;

namespace EWS_NSK_WPF
{
    public partial class AddHydrantWindow : Window
    {
        public AddHydrantWindow(double lat, double lng, int userRole)
        {
            // Подгружаем актуальную тему ДО InitializeComponent (как в окне свойств)
            string themePath = ThemeManager.IsDarkTheme
                ? "Themes/DarkTheme.xaml"
                : "Themes/LightTheme.xaml";

            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Relative)
            });

            InitializeComponent();
            DataContext = new AddHydrantViewModel(lat, lng, userRole);
        }
    }
}