using System;
using System.Collections.Generic;
using System.Windows;
using EWS_NSK_WPF.Models;
using EWS_NSK_WPF.Services;

namespace EWS_NSK_WPF
{
    public partial class ReportsWindow : Window
    {
        private List<Ewss> _allHydrants;
        private List<Ewss> _filteredHydrants;
        private string _currentReportType = "All";
        private string _currentUserName = "";
        private bool _canEdit = false;

        public ReportsWindow(string userName, string position, string accessLevel, WindowMode requestedMode)
        {
            InitializeComponent();

            _currentUserName = userName;
            _canEdit = (requestedMode == WindowMode.Edit && accessLevel != "Operator");

            UserInfoLabel.Text = $"Пользователь: {userName} ({position})";
            PermissionsLabel.Text = _canEdit ? "Режим: Формирование и экспорт отчетов" : "Режим: Только просмотр";

            if (!_canEdit)
            {
                BtnGenerateWord.Visibility = Visibility.Collapsed;
                BtnExportExcel.Visibility = Visibility.Collapsed;
            }

            this.Loaded += (s, e) =>
            {
                ThemeManager.ApplyThemeToReportsWindow(this);
                LoadData();
            };
        }

        private void LoadData()
        {
            _allHydrants = DatabaseHelper.GetAllHydrantsForReport();
            ApplyFilter();
        }

        private void ReportType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string tag)
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

            PreviewCountLabel.Text = $"Всего записей для отчета: {_filteredHydrants.Count}";
            PreviewDataGrid.ItemsSource = _filteredHydrants;
        }

        private void BtnGenerateWord_Click(object sender, RoutedEventArgs e)
        {
            if (_filteredHydrants.Count == 0)
            {
                MessageBox.Show("Нет данных для формирования отчета по выбранному фильтру.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Документ Word|*.docx",
                FileName = $"Отчет_ИНППВ_{_currentReportType}_{DateTime.Now:yyyyMMdd}.docx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var service = new WordReportService();
                    service.GenerateFormalReport(_filteredHydrants, dialog.FileName, _currentReportType, _currentUserName);

                    MessageBox.Show($"Отчет успешно сформирован:\n{dialog.FileName}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка генерации: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция экспорта в Excel находится в разработке.",
                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}