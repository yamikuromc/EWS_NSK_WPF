using System;
using System.Windows;
using EWS_NSK_WPF.Models;
using EWS_NSK_WPF.ViewModels;

namespace EWS_NSK_WPF
{
    public partial class HydrantDetailsWindow : Window
    {
        // Делаем ViewModel публичным, чтобы MapWindow мог на него подписаться
        public HydrantDetailsViewModel ViewModel { get; private set; }

        public HydrantDetailsWindow(Ewss hydrant, int userRole)
        {
            string themePath = ThemeManager.IsDarkTheme
                ? "Themes/DarkTheme.xaml"
                : "Themes/LightTheme.xaml";

            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Relative)
            });

            InitializeComponent();

            ViewModel = new HydrantDetailsViewModel(hydrant, userRole);
            DataContext = ViewModel;
        }
    }
}