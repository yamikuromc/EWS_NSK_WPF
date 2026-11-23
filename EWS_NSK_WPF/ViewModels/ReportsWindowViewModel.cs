using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EWS_NSK_WPF.Core;
using EWS_NSK_WPF.Models;
using EWS_NSK_WPF.Services;
using Microsoft.Win32;

namespace EWS_NSK_WPF.ViewModels
{
    public class ReportsWindowViewModel : ViewModelBase
    {
        private List<Ewss> _allHydrants = new List<Ewss>();
        private int? _selectedStatusCod;
        private string _outputPath = "";
        private bool _isGenerating;
        private readonly bool _canEdit;

        public List<Ewss> AllHydrants
        {
            get => _allHydrants;
            set => SetField(ref _allHydrants, value);
        }

        public int? SelectedStatusCod
        {
            get => _selectedStatusCod;
            set => SetField(ref _selectedStatusCod, value);
        }

        public string OutputPath
        {
            get => _outputPath;
            set
            {
                SetField(ref _outputPath, value);
                OnPropertyChanged(nameof(CanGenerate));
            }
        }

        public bool IsGenerating
        {
            get => _isGenerating;
            set
            {
                SetField(ref _isGenerating, value);
                OnPropertyChanged(nameof(CanGenerate));
            }
        }

        public bool CanGenerate => _canEdit && !string.IsNullOrEmpty(OutputPath) && !IsGenerating;

        public RelayCommand GenerateCommand { get; }
        public RelayCommand BrowsePathCommand { get; }

        public List<StatusItem> AvailableStatuses { get; } = new List<StatusItem>
        {
            new StatusItem { Code = 0, Name = "— все статусы —" },
            new StatusItem { Code = 1, Name = "Исправен" },
            new StatusItem { Code = 2, Name = "Неисправен" },
            new StatusItem { Code = 3, Name = "Требует уточнения" },
            new StatusItem { Code = 4, Name = "Отсутствует / Демонтирован" }
        };

        public ReportsWindowViewModel(List<Ewss> hydrants, bool canEdit)
        {
            _canEdit = canEdit;
            AllHydrants = hydrants;
            SelectedStatusCod = 0;

            GenerateCommand = new RelayCommand(GenerateReport);
            BrowsePathCommand = new RelayCommand(BrowsePath);
        }

        private void BrowsePath(object parameter)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Документ Word|*.docx",
                FileName = $"Отчет_ИНППВ_{DateTime.Now:yyyyMMdd_HHmmss}.docx"
            };

            if (dialog.ShowDialog() == true)
            {
                OutputPath = dialog.FileName;
            }
        }

        private void GenerateReport(object parameter)
        {
            try
            {
                IsGenerating = true;

                var filteredHydrants = AllHydrants.AsEnumerable().ToList();
                if (SelectedStatusCod.HasValue && SelectedStatusCod.Value > 0)
                {
                    filteredHydrants = filteredHydrants.Where(h => h.EwsStatusCod == SelectedStatusCod.Value).ToList();
                }

                if (filteredHydrants.Count == 0)
                {
                    MessageBox.Show("Нет данных для отчёта по выбранным параметрам.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var service = new WordReportService();
                service.GenerateFormalReport(filteredHydrants, OutputPath, "All", "Пользователь");

                MessageBox.Show($"Отчёт успешно сформирован:\n{OutputPath}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации отчёта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsGenerating = false;
            }
        }
    }
}