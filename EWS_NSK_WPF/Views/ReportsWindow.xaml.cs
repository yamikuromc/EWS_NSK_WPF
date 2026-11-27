using EWS_NSK_WPF.Models;
using EWS_NSK_WPF.Services;
using EWS_NSK_WPF.Services.ReportTemplates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace EWS_NSK_WPF.Views
{
    public partial class ReportsWindow : Window
    {
        private List<Ewss> _allHydrants;
        private List<Ewss> _filteredHydrants;
        private string _currentReportType = "Defective";
        private string _currentUserName = "Пользователь";
        private bool _canEdit = false;
        private ReportFormControl _currentForm;

        public ReportsWindow(string userName, string position, string accessLevel, WindowMode requestedMode)
        {
            InitializeComponent();

            _currentUserName = userName;
            _canEdit = (accessLevel != "Operator");

            UserInfoLabel.Text = $"Пользователь: {userName} ({position})";
            PermissionsLabel.Text = _canEdit ? "Режим: Формирование и экспорт отчетов" : "Режим: Только просмотр";

            this.Loaded += (s, e) =>


            {
                ApplyCurrentTheme();
                LoadData();
            };
            this.PreviewMouseWheel += ReportsWindow_PreviewMouseWheel;

            // Кнопка "Новый шаблон" для главного администратора
            if (accessLevel.Equals("Admin", StringComparison.OrdinalIgnoreCase) || _canEdit)
                NewTemplateButton.Visibility = Visibility.Visible;
            // Подписка на переключение темы
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        // ═══ ТЕМА ═══
        private void ApplyCurrentTheme()
        {
            bool isDark = ThemeManager.IsDarkTheme;

            // Фон окна
            this.Background = isDark
                ? new SolidColorBrush(Color.FromRgb(10, 10, 30))
                : new SolidColorBrush(Color.FromRgb(245, 245, 245));

            // Левая панель
            LeftPanelBorder.Background = isDark
                ? new SolidColorBrush(Color.FromRgb(20, 20, 50))
                : new SolidColorBrush(Colors.White);
            LeftPanelBorder.BorderBrush = isDark
                ? new SolidColorBrush(Color.FromRgb(90, 90, 122))
                : new SolidColorBrush(Color.FromRgb(208, 208, 208));

            // Подложка вокруг листа А4
            SheetBackdropBorder.Background = isDark
                ? new SolidColorBrush(Color.FromRgb(42, 42, 62))
                : new SolidColorBrush(Color.FromRgb(232, 232, 232));
            SheetBackdropBorder.BorderBrush = isDark
                ? new SolidColorBrush(Color.FromRgb(90, 90, 122))
                : new SolidColorBrush(Color.FromRgb(208, 208, 208));

            // Заголовок
            TitleTextBlock.Foreground = new SolidColorBrush(Colors.Orange);

            // Инфо о пользователе и правах
            var textColor = isDark ? Colors.White : Color.FromRgb(51, 51, 51);
            UserInfoLabel.Foreground = new SolidColorBrush(textColor);
            PermissionsLabel.Foreground = new SolidColorBrush(Colors.Orange);

            // Кнопки в левой панели
            if (LeftPanelBorder.Child is StackPanel sp)
            {
                foreach (var child in sp.Children)
                {
                    if (child is TextBlock tb)
                        tb.Foreground = new SolidColorBrush(Colors.Orange);
                    else if (child is Button btn)
                        btn.Foreground = new SolidColorBrush(textColor);
                }
            }
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => ApplyCurrentTheme());
        }

        protected override void OnClosed(EventArgs e)
        {
            ThemeManager.ThemeChanged -= OnThemeChanged;
            base.OnClosed(e);
        }

        // ═══ ДАННЫЕ ═══
        private void LoadData()
        {
            _allHydrants = DatabaseHelper.GetAllHydrantsForReport();
            ApplyFilter();
        }

        private void ReportType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                _currentReportType = tag;
                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            if (_allHydrants == null) return;

            _filteredHydrants = _currentReportType switch
            {
                "Defective" => _allHydrants.Where(h => h.EwsStatusCod == 2 || h.EwsStatusCod == 3 || h.EwsStatusCod == 4).ToList(),
                "ByOwner" => _allHydrants.Where(h => !string.IsNullOrEmpty(h.OwnerName)).OrderBy(h => h.OwnerName).ToList(),
                _ => _allHydrants
            };

            string defaultPosition = "Начальник караула смены №2 37 ПСЧ 5 ПСО ФПС ГПС ГУ по Новосибирской области";

            _currentForm = new ReportFormControl(_filteredHydrants, _currentUserName, _currentReportType);
            ReportFormContainer.Content = _currentForm;

            // Применяем текущий размер шрифта
            if (FontSizeSlider != null)
                _currentForm.SetFontSize(FontSizeSlider.Value);
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_currentForm != null)
                _currentForm.SetFontSize(e.NewValue);
        }

        private void NewTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция создания нового шаблона будет реализована в следующей версии.\n\n" +
                            "Планируется: пустой лист А4 с возможностью добавления текста и выпадающих списков.",
                            "Новый шаблон", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void ReportsWindow_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && _currentForm != null)
            {
                double delta = e.Delta > 0 ? 1 : -1;
                double newSize = Math.Max(8, Math.Min(20, FontSizeSlider.Value + delta));
                FontSizeSlider.Value = newSize;
                e.Handled = true;
            }
}    }   }