using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using EWS_NSK_WPF.Core;

namespace EWS_NSK_WPF.ViewModels
{
    public class ReferencesViewModel : ViewModelBase
    {
        private static readonly Dictionary<string, Dictionary<string, string>> ColumnTranslations = new Dictionary<string, Dictionary<string, string>>
        {
            ["tbl_EWS_Type"] = new Dictionary<string, string> { ["Type_ID"] = "№", ["Type_NameShort"] = "Тип / Сокращение", ["Type_Note"] = "Описание", ["Type_Name"] = "Полное название" },
            ["tbl_Fire3_Unit"] = new Dictionary<string, string> { ["Unit_LFG_COD"] = "Код гарнизона", ["Unit_ID"] = "№", ["Unit_ShortName"] = "Сокращённое название", ["Unit_FullName"] = "Полное название", ["Unit_FullAddress"] = "Полный адрес", ["Unit_City_COD"] = "Код города", ["Unit_Locality_COD"] = "Код населённого пункта", ["Unit_District_COD"] = "Код района", ["Unit_AddressObject_COD"] = "Код адресного объекта", ["Unit_HouseNumber"] = "Номер дома", ["FireUnit_X"] = "Координата X", ["FireUnit_Y"] = "Координата Y", ["Unit_MunAddress"] = "Муниципальный адрес", ["Unit_AdmAddress"] = "Административный адрес" },
            ["tbl_RS4_AddressObject"] = new Dictionary<string, string> { ["AddressObject_District_COD"] = "Код района", ["AddressObject_ID"] = "ID объекта", ["AddressObject_Name"] = "Название", ["AddressObject_Adm"] = "Административный адрес", ["AddressObject_Mun"] = "Муниципальный адрес", ["AddressObject_X"] = "Координата X", ["AddressObject_Y"] = "Координата Y", ["AddressObject_Display"] = "Отображаемое название", ["AddressObject_Unit_COD"] = "Код подразделения", ["AddressObject_LevelZone1"] = "Уровень зоны 1", ["AddressObject_LevelZone2"] = "Уровень зоны 2" },
            ["tbl_EWS_Owner"] = new Dictionary<string, string> { ["Owner_ID"] = "№", ["Owner_NameShort"] = "Краткое название", ["Owner_NameFull"] = "Полное название", ["Owner_Address"] = "Адрес", ["Owner_PostAddress"] = "Почтовый адрес", ["Owner_Unit_COD"] = "Код подразделения" },
            ["tbl_CheckEWS_StatusEWS"] = new Dictionary<string, string> { ["StatusEWS_ID"] = "№", ["StatusEWS_Name"] = "Название статуса", ["StatusEWS_Note"] = "Описание" }
        };

        private static readonly Dictionary<string, string> ReferenceTitles = new Dictionary<string, string>
        {
            ["tbl_Fire3_Unit"] = "Подразделения ППС",
            ["tbl_RS4_AddressObject"] = "Адресные объекты",
            ["tbl_EWS_Type"] = "Характеристики",
            ["tbl_CheckEWS_StatusEWS"] = "Параметры проверок",
            ["tbl_EWS_Owner"] = "Балансодержатели"
        };

        private bool _isLoading;
        private bool _isEditMode;
        private bool _isAdmin;
        private string _userInfoText = "Режим: Просмотр справочных данных";
        private string _editButtonText = "Режим редактирования";
        private Visibility _saveButtonVisible = Visibility.Collapsed;
        private DataView _gridData;
        private string _windowTitle = "Справочники";

        public bool IsLoading { get => _isLoading; set => SetField(ref _isLoading, value); }
        public bool IsEditMode { get => _isEditMode; set { if (SetField(ref _isEditMode, value)) UpdateEditModeUI(); } }
        public bool IsAdmin { get => _isAdmin; set => SetField(ref _isAdmin, value); }
        public string UserInfoText { get => _userInfoText; private set => SetField(ref _userInfoText, value); }
        public string EditButtonText { get => _editButtonText; private set => SetField(ref _editButtonText, value); }
        public Visibility SaveButtonVisible { get => _saveButtonVisible; private set => SetField(ref _saveButtonVisible, value); }
        public DataView GridData { get => _gridData; private set => SetField(ref _gridData, value); }
        public string WindowTitle { get => _windowTitle; private set => SetField(ref _windowTitle, value); }

        public string CurrentUserName { get; set; } = "Пользователь";
        public string CurrentUserPosition { get; set; } = "Сотрудник";
        public int UserRole { get; set; }
        public int CurrentStaffId { get; set; }

        public ICommand LoadTableCommand { get; }
        public ICommand ToggleEditModeCommand { get; }
        public ICommand SaveCommand { get; }

        public ReferencesViewModel()
        {
            LoadTableCommand = new RelayCommand(LoadTable);
            ToggleEditModeCommand = new RelayCommand(ToggleEditMode);
            SaveCommand = new RelayCommand(Save);
        }

        private void UpdateEditModeUI()
        {
            if (_isEditMode)
            {
                EditButtonText = "Отменить редактирование";
                SaveButtonVisible = Visibility.Visible;
                UserInfoText = "Режим: Редактирование";
            }
            else
            {
                EditButtonText = "Режим редактирования";
                SaveButtonVisible = Visibility.Collapsed;
                UserInfoText = "Режим: Просмотр справочных данных";
            }
        }

        private async void LoadTable(object parameter)
        {
            string tableName = parameter as string;
            if (string.IsNullOrEmpty(tableName)) return;

            IsLoading = true;
            WindowTitle = $"Справочник: {ReferenceTitles.GetValueOrDefault(tableName, tableName)}";

            try
            {
                DataTable data = await Task.Run(() => DatabaseHelper.GetTableData(tableName));
                if (data != null && data.Rows.Count > 0)
                {
                    if (ColumnTranslations.ContainsKey(tableName))
                    {
                        var translations = ColumnTranslations[tableName];
                        foreach (DataColumn column in data.Columns)
                        {
                            if (translations.ContainsKey(column.ColumnName))
                                column.ColumnName = translations[column.ColumnName];
                        }
                    }
                    GridData = data.DefaultView;
                }
                else
                {
                    MessageBox.Show($"Таблица '{tableName}' пуста или не найдена.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ToggleEditMode(object parameter)
        {
            IsEditMode = !IsEditMode;
        }

        private void Save(object parameter)
        {
            MessageBox.Show($"Сохранение будет реализовано.\nStaff_ID: {CurrentStaffId}, Роль: {UserRole}", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}