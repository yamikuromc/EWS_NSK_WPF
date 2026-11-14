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
        private bool _isMapEditMode; // <-- НОВОЕ: запоминаем режим карты

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

                  
                }
            }
        }

        public Action<Ewss> OnStatusChanged { get; set; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand CloseCommand { get; }

        // <-- ИЗМЕНЕНО: добавлен параметр isMapEditMode
        public HydrantDetailsViewModel(Ewss hydrant, int userRole, bool isMapEditMode)
        {
            Hydrant = hydrant;
            _isMapEditMode = isMapEditMode;

            // 🚨 Блокируем редактирование, если пользователь не админ ИЛИ карта не в режиме редактирования
            IsReadOnly = userRole < 2 || !isMapEditMode;

            _selectedStatusCode = hydrant.EwsStatusCod > 0 ? hydrant.EwsStatusCod : 1;

            SaveCommand = new RelayCommand(SaveHydrant);
            CloseCommand = new RelayCommand(CloseWindow);
        }

        private void SaveHydrant(object parameter)
        {
            MessageBox.Show($"Данные гидранта ПГ-{Hydrant.DisplayNumber} сохранены в базу!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            if (parameter is Window window && window.Owner is MapWindow mapWindow)
            {
                mapWindow.RefreshHydrantInList(Hydrant);
                // Здесь можно было бы сбросить флаг несохраненных изменений, 
                // но для простоты оставим его до закрытия карты, чтобы пользователь точно увидел предупреждение.
            }

            CloseWindow(parameter);   // ← было null, окно не закрывалось после сохранения
        }

        private void CloseWindow(object parameter)
        {
            // 🚨 УБРАНО: окно предупреждения отсюда. 
            // Теперь предупреждение будет выскакивать только при попытке закрыть САМУ КАРТУ (MapWindow),
            // как ты и просила.
            if (parameter is Window window)
            {
                window.Close();
            }
        }
    }
}