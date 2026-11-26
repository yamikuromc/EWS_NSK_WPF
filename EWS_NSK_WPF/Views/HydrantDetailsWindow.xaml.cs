using System;
using System.Windows;
using EWS_NSK_WPF.Models;
using EWS_NSK_WPF.Services;
using EWS_NSK_WPF.ViewModels;
using EWS_NSK_WPF.Views;

namespace EWS_NSK_WPF.Views
{
    public partial class HydrantDetailsWindow : Window
    {
        // Делаем ViewModel публичным, чтобы MapWindow мог на него подписаться
        public HydrantDetailsViewModel ViewModel { get; private set; }

        public HydrantDetailsWindow(Ewss hydrant, int userRole, bool isMapEditMode = false)
        {
            string themePath = ThemeManager.IsDarkTheme
                ? "Themes/DarkTheme.xaml"
                : "Themes/LightTheme.xaml";

            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Relative)
            });

            InitializeComponent();

            ViewModel = new HydrantDetailsViewModel(hydrant, userRole, isMapEditMode);
            DataContext = ViewModel;
        }
    }
}