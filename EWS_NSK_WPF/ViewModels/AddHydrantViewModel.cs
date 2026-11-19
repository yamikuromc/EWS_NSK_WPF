using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EWS_NSK_WPF.Core;
using EWS_NSK_WPF.Models;

namespace EWS_NSK_WPF.ViewModels
{
    public class AddHydrantViewModel : ViewModelBase
    {
        private Ewss _newHydrant;
        private int _selectedStatusCode = 1; // По умолчанию "Исправен"
        private string _validationError;

        public Ewss NewHydrant
        {
            get => _newHydrant;
            set => SetField(ref _newHydrant, value);
        }

        public int SelectedStatusCode
        {
            get => _selectedStatusCode;
            set => SetField(ref _selectedStatusCode, value);
        }

        public string ValidationError
        {
            get => _validationError;
            set => SetField(ref _validationError, value);
        }

        public bool CanSave => string.IsNullOrEmpty(ValidationError);

        public List<StatusItem> AvailableStatuses { get; } = new List<StatusItem>
        {
            new StatusItem { Code = 1, Name = "Исправен" },
            new StatusItem { Code = 2, Name = "Неисправен" },
            new StatusItem { Code = 3, Name = "Требует уточнения" },
            new StatusItem { Code = 4, Name = "Отсутствует / Демонтирован" }
        };

        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        // Конструктор принимает координаты точки клика по карте
        public AddHydrantViewModel(double latitude, double longitude, int userRole)
        {
            NewHydrant = new Ewss
            {
                LatitudeD = latitude,
                LongitudeD = longitude,
                EwsStatusCod = 1,
                StatusName = "Исправен",
                EwsNumber = 0,
                EwsDisplay = 0,
                EwsBindDisplay = 0,
                RecordCreated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                RecordUserCod = userRole,
                RecordStatus = "Новая запись"
            };

            SaveCommand = new RelayCommand(SaveHydrant);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void SaveHydrant(object parameter)
        {
            // Валидация: номер гидранта обязателен
            if (NewHydrant.EwsNumber <= 0)
            {
                ValidationError = "Укажите номер гидранта!";
                return;
            }

            // Применяем выбранный статус
            NewHydrant.EwsStatusCod = SelectedStatusCode;
            var status = AvailableStatuses.FirstOrDefault(s => s.Code == SelectedStatusCode);
            NewHydrant.StatusName = status?.Name ?? "Исправен";

            ValidationError = ""; // гасим ошибку валидации

            if (parameter is Window window)
            {
                window.DialogResult = true;
                window.Close();
            }
        }

        private void Cancel(object parameter)
        {
            if (parameter is Window window)
            {
                window.DialogResult = false;
                window.Close();
            }
        }
    }
}