using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace EWS_NSK_WPF
{
    public partial class ReferencesWindow : Window
    {
        // СЛОВАРЬ: имя таблицы → (имя колонки в БД → русское название)
        private static readonly Dictionary<string, Dictionary<string, string>> ColumnTranslations =
    new Dictionary<string, Dictionary<string, string>>
    {
        // Таблица типов ИНППВ (гидранты, водоемы, краны...)
        ["tbl_EWS_Type"] = new Dictionary<string, string>
        {
            ["Type_ID"] = "№",
            ["Type_NameShort"] = "Тип / Сокращение",
            ["Type_Note"] = "Описание",
            ["Type_Name"] = "Полное название"
        },

        // Таблица подразделений пожарной охраны
        ["tbl_Fire3_Unit"] = new Dictionary<string, string>
        {
            ["Unit_LFG_COD"] = "Код гарнизона",
            ["Unit_ID"] = "№",
            ["Unit_ShortName"] = "Сокращённое название",
            ["Unit_FullName"] = "Полное название",
            ["Unit_FullAddress"] = "Полный адрес",
            ["Unit_City_COD"] = "Код города",
            ["Unit_Locality_COD"] = "Код населённого пункта",
            ["Unit_District_COD"] = "Код района",
            ["Unit_AddressObject_COD"] = "Код адресного объекта",
            ["Unit_HouseNumber"] = "Номер дома",
            ["FireUnit_X"] = "Координата X",
            ["FireUnit_Y"] = "Координата Y",
            ["Unit_MunAddress"] = "Муниципальный адрес",
            ["Unit_AdmAddress"] = "Административный адрес"
        },

        // Таблица адресных объектов (улицы, переулки)
        ["tbl_RS4_AddressObject"] = new Dictionary<string, string>
        {
            ["AddressObject_District_COD"] = "Код района",
            ["AddressObject_ID"] = "ID объекта",
            ["AddressObject_Name"] = "Название",
            ["AddressObject_Adm"] = "Административный адрес",
            ["AddressObject_Mun"] = "Муниципальный адрес",
            ["AddressObject_X"] = "Координата X",
            ["AddressObject_Y"] = "Координата Y",
            ["AddressObject_Display"] = "Отображаемое название",
            ["AddressObject_Unit_COD"] = "Код подразделения",
            ["AddressObject_LevelZone1"] = "Уровень зоны 1",
            ["AddressObject_LevelZone2"] = "Уровень зоны 2"
        },

        // ✅ ИСПРАВЛЕНО: Таблица балансодержателей (ТОЧНЫЕ имена из скриншота)
        ["tbl_EWS_Owner"] = new Dictionary<string, string>
        {
            ["Owner_ID"] = "№",
            ["Owner_NameShort"] = "Краткое название",
            ["Owner_NameFull"] = "Полное название",
            ["Owner_Address"] = "Адрес",
            ["Owner_PostAddress"] = "Почтовый адрес",
            ["Owner_Unit_COD"] = "Код подразделения"
        },

        // ✅ ИСПРАВЛЕНО: Таблица статусов проверок (ТОЧНЫЕ имена из скриншота)
        ["tbl_CheckEWS_StatusEWS"] = new Dictionary<string, string>
        {
            ["StatusEWS_ID"] = "№",
            ["StatusEWS_Name"] = "Название статуса",
            ["StatusEWS_Note"] = "Описание"
        }
    };

        public ReferencesWindow()
        {
            InitializeComponent();
            UserInfoLabel.Text = "Режим: Просмотр справочных данных";
            PermissionsLabel.Text = "Данные загружаются из локальной базы SQLite";
        }

        // Метод загрузки таблицы с русскими заголовками
        public void LoadTable(string tableName, string titleName)
        {
            this.Title = $"Справочник: {titleName}";

            try
            {
                DataTable data = DatabaseHelper.GetTableData(tableName);

                if (data != null && data.Rows.Count > 0)
                {
                    // Очищаем существующие колонки
                    ReferenceDataGrid.Columns.Clear();

                    // Создаём колонки вручную на основе переименованных
                    foreach (DataColumn column in data.Columns)
                    {
                        string columnName = column.ColumnName;
                        string displayName = columnName;

                        // Если есть перевод, используем его
                        if (ColumnTranslations.ContainsKey(tableName))
                        {
                            var translations = ColumnTranslations[tableName];
                            if (translations.ContainsKey(columnName))
                            {
                                displayName = translations[columnName];
                            }
                        }

                        // Создаём колонку DataGrid
                        DataGridTextColumn dgColumn = new DataGridTextColumn
                        {
                            Header = displayName,
                            Binding = new System.Windows.Data.Binding(columnName),
                            IsReadOnly = true
                        };

                        ReferenceDataGrid.Columns.Add(dgColumn);
                    }

                    ReferenceDataGrid.ItemsSource = data.DefaultView;
                }
                else
                {
                    MessageBox.Show($"Таблица '{tableName}' пуста или не найдена.",
                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}