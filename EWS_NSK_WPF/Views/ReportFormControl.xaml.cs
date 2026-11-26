using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using EWS_NSK_WPF.Models;
using EWS_NSK_WPF.Services.ReportTemplates;

namespace EWS_NSK_WPF.Views
{
    public partial class ReportFormControl : UserControl
    {
        private readonly List<Ewss> _hydrants;
        private readonly string _authorName;
        private readonly string _authorPosition;

        public ReportFormControl(List<Ewss> hydrants, string authorName, string authorPosition)
        {
            InitializeComponent();
            _hydrants = hydrants;
            _authorName = authorName;
            _authorPosition = authorPosition;

            // Заполняем поля автора
            AuthorPositionText.Text = _authorPosition;
            AuthorNameText.Text = _authorName;
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Документ Word|*.docx",
                FileName = $"Список_неисправных_ПГ_{DateTime.Now:yyyyMMdd}.docx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Собираем параметры из выпадающих списков
                    var parameters = new Dictionary<string, object>
                    {
                        ["territory"] = (TerritoryCombo.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                        ["season"] = (SeasonCombo.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                        ["year"] = int.Parse((YearCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "2026"),
                        ["authorPosition"] = _authorPosition,
                        ["authorName"] = _authorName
                    };

                    // Вызываем генератор (он уже умеет делать списки, а не таблицы, благодаря прошлой правке)
                    ReportGenerator.GenerateReport("Defective", _hydrants, dialog.FileName, parameters);

                    MessageBox.Show($"Отчёт успешно сформирован и сохранён:\n{dialog.FileName}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка генерации: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}