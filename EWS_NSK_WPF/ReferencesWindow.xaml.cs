using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace EWS_NSK_WPF
{
    public partial class ReferencesWindow : Window
    {
        // СЛОВАРЬ: имя таблицы → (имя колонки в БД → русское название)
        private static readonly Dictionary<string, Dictionary<string, string>> ColumnTranslations =
            new Dictionary<string, Dictionary<string, string>>
            {
                ["tbl_EWS_Type"] = new Dictionary<string, string>
                {
                    ["Type_ID"] = "№",
                    ["Type_NameShort"] = "Тип / Сокращение",
                    ["Type_Note"] = "Описание",
                    ["Type_Name"] = "Полное название"
                },

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

                ["tbl_EWS_Owner"] = new Dictionary<string, string>
                {
                    ["Owner_ID"] = "№",
                    ["Owner_NameShort"] = "Краткое название",
                    ["Owner_NameFull"] = "Полное название",
                    ["Owner_Address"] = "Адрес",
                    ["Owner_PostAddress"] = "Почтовый адрес",
                    ["Owner_Unit_COD"] = "Код подразделения"
                },

                ["tbl_CheckEWS_StatusEWS"] = new Dictionary<string, string>
                {
                    ["StatusEWS_ID"] = "№",
                    ["StatusEWS_Name"] = "Название статуса",
                    ["StatusEWS_Note"] = "Описание"
                }
            };

        // Словарь для отображаемых названий справочников
        private static readonly Dictionary<string, string> ReferenceTitles = new Dictionary<string, string>
        {
            ["tbl_Fire3_Unit"] = "Подразделения ППС",
            ["tbl_RS4_AddressObject"] = "Адресные объекты",
            ["tbl_EWS_Type"] = "Характеристики",
            ["tbl_CheckEWS_StatusEWS"] = "Параметры проверок",
            ["tbl_EWS_Owner"] = "Балансодержатели"
        };

        private string currentTableName = "";
        private bool isEditMode = false;
        private DataTable currentDataTable = null;
        private int userRole = 0;
        private int currentStaffId = 0; // ← ДОБАВЛЕНО: ID текущего пользователя

        public ReferencesWindow()
        {
            InitializeComponent();
            UserInfoLabel.Text = "Режим: Просмотр справочных данных";

            // Загружаем первый справочник ПОСЛЕ полной загрузки окна
            this.Loaded += (s, e) =>
            {
                // Сначала применяем тему
                ApplyCurrentTheme();

                // Потом загружаем данные
                if (string.IsNullOrEmpty(currentTableName))
                {
                    LoadTable("tbl_Fire3_Unit", "Подразделения ППС");
                }
            };
        }

        // Новый метод: применяем тему к DataGrid напрямую
        private void ApplyCurrentTheme()
        {
            if (ThemeManager.IsDarkTheme)
            {
                // Тёмная тема
                this.Background = new SolidColorBrush(Color.FromRgb(10, 10, 30));

                ReferenceDataGrid.Background = new SolidColorBrush(Color.FromRgb(20, 20, 50));
                ReferenceDataGrid.Foreground = new SolidColorBrush(Colors.White);
                ReferenceDataGrid.RowBackground = new SolidColorBrush(Color.FromRgb(25, 25, 55));
                ReferenceDataGrid.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(35, 35, 70));
                ReferenceDataGrid.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 100));

                // Заголовки колонок
                var headerStyle = new Style(typeof(DataGridColumnHeader));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty,
                    new SolidColorBrush(Color.FromRgb(0, 60, 120))));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty,
                    new SolidColorBrush(Colors.Orange)));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty,
                    new SolidColorBrush(Color.FromRgb(60, 60, 100))));
                ReferenceDataGrid.ColumnHeaderStyle = headerStyle;

                // Строки
                var rowStyle = new Style(typeof(DataGridRow));
                rowStyle.Setters.Add(new Setter(DataGridRow.ForegroundProperty,
                    new SolidColorBrush(Colors.White)));
                ReferenceDataGrid.RowStyle = rowStyle;

                // Текст заголовка и подзаголовка
                // (найди в XAML имена TextBlock'ов и примени сюда)
            }
            else
            {
                // Светлая тема (возвращаем как было)
                this.Background = new SolidColorBrush(Color.FromRgb(0, 40, 100));

                ReferenceDataGrid.Background = new SolidColorBrush(Colors.White);
                ReferenceDataGrid.Foreground = new SolidColorBrush(Colors.Black);
                ReferenceDataGrid.RowBackground = new SolidColorBrush(Colors.White);
                ReferenceDataGrid.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(240, 248, 255));
                ReferenceDataGrid.BorderBrush = new SolidColorBrush(Colors.Transparent);

                var headerStyle = new Style(typeof(DataGridColumnHeader));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty,
                    new SolidColorBrush(Color.FromRgb(0, 60, 120))));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty,
                    new SolidColorBrush(Colors.Orange)));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
                ReferenceDataGrid.ColumnHeaderStyle = headerStyle;

                var rowStyle = new Style(typeof(DataGridRow));
                rowStyle.Setters.Add(new Setter(DataGridRow.ForegroundProperty,
                    new SolidColorBrush(Colors.Black)));
                ReferenceDataGrid.RowStyle = rowStyle;
            }
        }

        // Метод загрузки таблицы (вызывается из MainWindow при клике в боковом меню)
        public void LoadTable(string tableName, string titleName)
        {
            currentTableName = tableName;
            this.Title = $"Справочник: {titleName}";
            LoadData(tableName);
        }

        // Метод для установки роли пользователя и управления доступом
        public void SetUserRole(int role)
        {
            userRole = role;

            System.Diagnostics.Debug.WriteLine($"[ReferencesWindow] Получена роль: {role}");

            if (role >= 2) // Администратор части (2) или Главный админ (3)
            {
                AdminButtons.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine("[ReferencesWindow] Кнопки админа ПОКАЗАНЫ");
            }
            else // Диспетчер (0) или Оператор (1) — только просмотр
            {
                AdminButtons.Visibility = Visibility.Collapsed;
                System.Diagnostics.Debug.WriteLine("[ReferencesWindow] Кнопки админа СКРЫТЫ");
            }
        }

        // ← ДОБАВЛЕНО: Метод для установки ID сотрудника
        public void SetStaffId(int staffId)
        {
            currentStaffId = staffId;
            System.Diagnostics.Debug.WriteLine($"[ReferencesWindow] Установлен Staff_ID: {staffId}");
        }

        // Обработчик клика по кнопке в левой панели
        private void ReferenceItem_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton == null) return;

            string tableName = clickedButton.Tag?.ToString();
            if (string.IsNullOrEmpty(tableName)) return;

            string titleName = ReferenceTitles.ContainsKey(tableName)
                ? ReferenceTitles[tableName]
                : tableName;

            LoadTable(tableName, titleName);
            e.Handled = true;
        }

        // Внутренний метод загрузки данных (асинхронный)
        private async void LoadData(string tableName)
        {
            try
            {
                ReferenceDataGrid.IsEnabled = false;
                ReferenceDataGrid.Opacity = 0.5;

                DataTable data = await Task.Run(() => DatabaseHelper.GetTableData(tableName));

                if (data != null && data.Rows.Count > 0)
                {
                    if (ColumnTranslations.ContainsKey(tableName))
                    {
                        var translations = ColumnTranslations[tableName];
                        foreach (DataColumn column in data.Columns)
                        {
                            if (translations.ContainsKey(column.ColumnName))
                            {
                                column.ColumnName = translations[column.ColumnName];
                            }
                        }
                    }

                    currentDataTable = data;
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
            finally
            {
                ReferenceDataGrid.IsEnabled = true;
                ReferenceDataGrid.Opacity = 1.0;
            }
        }

        // Кнопка "Режим редактирования"
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isEditMode)
            {
                isEditMode = true;
                ReferenceDataGrid.IsReadOnly = false;
                ReferenceDataGrid.CanUserAddRows = false; // Не разрешаем добавлять строки пока
                EditButton.Content = "Отменить редактирование";
                SaveButton.Visibility = Visibility.Visible;
                UserInfoLabel.Text = "Режим: Редактирование";
            }
            else
            {
                isEditMode = false;
                ReferenceDataGrid.IsReadOnly = true;
                ReferenceDataGrid.CanUserAddRows = false;
                EditButton.Content = "Режим редактирования";
                SaveButton.Visibility = Visibility.Collapsed;
                UserInfoLabel.Text = "Режим: Просмотр справочных данных";
            }
        }

        // Кнопка "Сохранить изменения" (заглушка)
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Сохранение будет реализовано.\nStaff_ID: {currentStaffId}, Роль: {userRole}",
                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}