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

    public class OwnerItem
    {
        public int Code { get; set; }
        public string Name { get; set; }
    }

    public class HydrantDetailsViewModel : ViewModelBase
    {
        private Ewss _hydrant;
        private bool _isReadOnly;
        private int _selectedStatusCode;
        private int _selectedOwnerCode;
        private List<OwnerItem> _availableOwners = new List<OwnerItem>();

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

        public List<OwnerItem> AvailableOwners
        {
            get => _availableOwners;
            set => SetField(ref _availableOwners, value);
        }

        public int SelectedStatusCode
        {
            get => _selectedStatusCode;
            set
            {
                if (SetField(ref _selectedStatusCode, value))
                {
                    Hydrant.EwsStatusCod = value;
                    var status = AvailableStatuses.FirstOrDefault(s => s.Code == value);
                    Hydrant.StatusName = status?.Name ?? "Неизвестно";
                    OnHydrantModified?.Invoke(Hydrant);
                }
            }
        }

        public int SelectedOwnerCode
        {
            get => _selectedOwnerCode;
            set
            {
                if (SetField(ref _selectedOwnerCode, value))
                {
                    Hydrant.EwsOwnerCod = value;
                    var owner = AvailableOwners.FirstOrDefault(o => o.Code == value);
                    Hydrant.OwnerName = owner?.Name ?? "";
                    OnHydrantModified?.Invoke(Hydrant);
                }
            }
        }

        public Action<Ewss> OnStatusChanged { get; set; }
        public Action<Ewss> OnHydrantModified { get; set; }
        public Action<Ewss> OnDeleteRequested { get; set; }

        public RelayCommand SaveCommand { get; }
        public RelayCommand CloseCommand { get; }
        public RelayCommand DeleteCommand { get; }

        public HydrantDetailsViewModel(Ewss hydrant, int userRole, bool isMapEditMode)
        {
            Hydrant = hydrant;
            _isReadOnly = userRole < 2 || !isMapEditMode;

            _selectedStatusCode = hydrant.EwsStatusCod > 0 ? hydrant.EwsStatusCod : 1;

            LoadOwners();
            _selectedOwnerCode = hydrant.EwsOwnerCod;

            SaveCommand = new RelayCommand(SaveHydrant);
            CloseCommand = new RelayCommand(CloseWindow);
            DeleteCommand = new RelayCommand(DeleteHydrant);
        }

        private void LoadOwners()
        {
            try
            {
                string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "INPPV.db");
                var owners = new List<OwnerItem>();

                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT * FROM tbl_EWS_Owner";
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        int id = 0;
                        int.TryParse(r[0]?.ToString(), out id);
                        string name = r[1]?.ToString() ?? "";
                        if (id > 0 && !string.IsNullOrEmpty(name))
                            owners.Add(new OwnerItem { Code = id, Name = name });
                    }
                }

                owners.Insert(0, new OwnerItem { Code = 0, Name = "— не указан —" });
                AvailableOwners = owners;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠ Ошибка загрузки справочника владельцев: {ex.Message}");
                AvailableOwners = new List<OwnerItem> { new OwnerItem { Code = 0, Name = "— не указан —" } };
            }
        }

        private void SaveHydrant(object parameter)
        {
            MessageBox.Show($"Данные гидранта ПГ-{Hydrant.DisplayNumber} подготовлены к сохранению.\nНажмите «💾 Сохранить изменения» на карте.",
                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            CloseWindow(parameter);
        }

        private void DeleteHydrant(object parameter)
        {
            var result = MessageBox.Show(
                $"Удалить гидрант ПГ-{Hydrant.DisplayNumber}?\n\nИзменение будет записано в базу после нажатия «💾 Сохранить изменения» на карте.",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                OnDeleteRequested?.Invoke(Hydrant);
                if (parameter is Window window) window.Close();
            }
        }

        private void CloseWindow(object parameter)
        {
            if (parameter is Window window) window.Close();
        }
    }
}