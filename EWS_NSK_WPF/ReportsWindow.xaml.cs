using EWS_NSK_WPF.Models;
using EWS_NSK_WPF.Services;
using EWS_NSK_WPF.Services.ReportTemplates;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            PreviewDataGrid.PreviewMouseWheel += DataGrid_PreviewMouseWheel;

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
            PreviewDataGrid.AutoGeneratingColumn += PreviewDataGrid_AutoGeneratingColumn;
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
                FileName = $"Список_неисправных_ПГ_{DateTime.Now:yyyyMMdd}.docx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Параметры для шаблона
                    var parameters = new Dictionary<string, object>
                    {
                        ["territory"] = "р.п. Краснообск и п. Элитный",
                        ["season"] = "осенней",
                        ["year"] = DateTime.Now.Year,
                        ["authorPosition"] = "Начальник караула смены №2 37 ПСЧ 5 ПСО ФПС ГПС ГУ по Новосибирской области",
                        ["authorName"] = _currentUserName
                    };

                    // Генерируем через новую систему
                    ReportGenerator.GenerateReport(_currentReportType, _filteredHydrants, dialog.FileName, parameters);

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

        private void PreviewDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // Словарь переименований (как в справочниках)
            var columnNames = new Dictionary<string, string>
            {
                ["EwsNumber"] = "№ ПГ",
                ["AddressObjectName"] = "Населенный пункт",
                ["EwsHouseNumber"] = "Адрес",
                ["OwnerName"] = "Балансодержатель",
                ["StatusName"] = "Статус",
                ["PipeInfo"] = "Диаметр",
                ["EwsStateNote"] = "Примечание"
            };

            if (columnNames.ContainsKey(e.PropertyName))
            {
                e.Column.Header = columnNames[e.PropertyName];
            }
            else
            {
                // Скрываем все остальные колонки (технические поля)
                e.Cancel = true;
            }
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PreviewDataGrid != null)
            {
                PreviewDataGrid.FontSize = e.NewValue;
            }
        }

        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                double delta = e.Delta > 0 ? 1 : -1;
                double newSize = Math.Max(9, Math.Min(16, PreviewDataGrid.FontSize + delta));
                PreviewDataGrid.FontSize = newSize;
                if (FontSizeSlider != null) FontSizeSlider.Value = newSize;
                e.Handled = true;
            }
        }
    }
}