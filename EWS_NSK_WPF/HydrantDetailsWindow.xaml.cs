using System;
using System.Windows;
using EWS_NSK_WPF.Models;
using EWS_NSK_WPF.ViewModels;

namespace EWS_NSK_WPF
{
    public partial class HydrantDetailsWindow : Window
    {
        public HydrantDetailsWindow(Ewss hydrant, int userRole)
        {
            // 1. СНАЧАЛА загружаем тему
            string themePath = ThemeManager.IsDarkTheme
                ? "Themes/DarkTheme.xaml"
                : "Themes/LightTheme.xaml";

            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Relative)
            });

            // 2. ПОТОМ инициализируем компоненты
            InitializeComponent();

            // 3. И только потом устанавливаем DataContext
            DataContext = new HydrantDetailsViewModel(hydrant, userRole);
        }
    }
}