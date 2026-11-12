using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EWS_NSK_WPF.Core;
using EWS_NSK_WPF.Models;

namespace EWS_NSK_WPF.ViewModels
{
    public class StatusItem
    {
        public int Code { get; set; }
        public string Name { get; set; }
    }

    public class HydrantDetailsViewModel : ViewModelBase
    {
        private Ewss _hydrant;
        private bool _isReadOnly;
        private int _selectedStatusCode;

        public Ewss Hydrant
        {
            get => _hydrant;
            set => SetField(ref _hydrant, value);
        }

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => SetField(ref _isReadOnly, value);
        }

        public bool CanEdit => !IsReadOnly;
        public bool CanSave => !IsReadOnly;
        public bool CanDelete => !IsReadOnly;

        public List<StatusItem> AvailableStatuses { get; } = new List<StatusItem>
        {
            new StatusItem { Code = 1, Name = "Исправен" },
            new StatusItem { Code = 2, Name = "Неисправен" },
            new StatusItem { Code = 3, Name = "Требует уточнения" },
            new StatusItem { Code = 4, Name = "Отсутствует / Демонтирован" }
        };

        private bool _hasUnsavedChanges = false;

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set => SetField(ref _hasUnsavedChanges, value);
        }
        public int SelectedStatusCode
        {
            get => _selectedStatusCode;
            set
            {
                if (SetField(ref _selectedStatusCode, value))
                {
                    // Обновляем данные в памяти (для мгновенной перекраски на карте)
                    Hydrant.EwsStatusCod = value;
                    var status = AvailableStatuses.FirstOrDefault(s => s.Code == value);
                    Hydrant.StatusName = status?.Name ?? "Неизвестно";

                    // Уведомляем карту, чтобы она перекрасила маркер
                    OnStatusChanged?.Invoke(Hydrant);
                    HasUnsavedChanges = true;
                }
            }
        }

        public Action<Ewss> OnStatusChanged { get; set; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand CloseCommand { get; }

        public HydrantDetailsViewModel(Ewss hydrant, int userRole)
        {
            Hydrant = hydrant;
            IsReadOnly = userRole < 2;
            _selectedStatusCode = hydrant.EwsStatusCod > 0 ? hydrant.EwsStatusCod : 1;

            SaveCommand = new RelayCommand(SaveHydrant);
            CloseCommand = new RelayCommand(CloseWindow);
        }

        private void SaveHydrant(object parameter)
        {
            // ЗДЕСЬ ДОЛЖЕН БЫТЬ ВЫЗОВ БАЗЫ ДАННЫХ. 
            // Пока просто имитируем успех. Данные в БД изменятся ТОЛЬКО здесь.
            MessageBox.Show($"Данные гидранта ПГ-{Hydrant.DisplayNumber} сохранены в базу!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            // Сообщаем MapWindow, что этот гидрант нужно обновить в списке _allHydrants
            if (parameter is Window window && window.Owner is MapWindow mapWindow)
            {
                mapWindow.RefreshHydrantInList(Hydrant);
            }
            HasUnsavedChanges = false;

            CloseWindow(null);
        }

        private void CloseWindow(object parameter)
        {
            if (HasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "У вас есть несохраненные изменения!\n\nСохранить их перед закрытием?",
                    "Несохраненные изменения",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    SaveHydrant(parameter);
                    return;
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return; // Не закрываем окно
                }
                // Если No - закрываем без сохранения
            }

            if (parameter is Window window) window.Close();
        }


    }
}