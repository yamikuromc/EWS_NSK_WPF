using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace EWS_NSK_WPF
{
    // Конвертер для скрытия подсказки поиска
    public class EmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class ReferencesWindow : Window
    {
        private static readonly Dictionary<string, Dictionary<string, string>> ColumnTranslations = new Dictionary<string, Dictionary<string, string>>
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
        private int currentStaffId = 0;
        private string currentUserName = "Пользователь";
        private string currentUserPosition = "Сотрудник";

        public ReferencesWindow()
        {
            InitializeComponent();
            UserInfoLabel.Text = "Режим: Просмотр справочных данных";

            this.Loaded += (s, e) =>
            {
                ApplyCurrentTheme();
                if (string.IsNullOrEmpty(currentTableName))
                {
                    LoadTable("tbl_Fire3_Unit", "Подразделения ППС");
                }
            };
        }

        private void ApplyCurrentTheme()
        {
            if (ThemeManager.IsDarkTheme)
            {
                this.Background = new SolidColorBrush(Color.FromRgb(10, 10, 30));
                ReferenceDataGrid.Background = new SolidColorBrush(Color.FromRgb(20, 20, 50));
                ReferenceDataGrid.Foreground = new SolidColorBrush(Colors.White);
                ReferenceDataGrid.RowBackground = new SolidColorBrush(Color.FromRgb(25, 25, 55));
                ReferenceDataGrid.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(35, 35, 70));
                ReferenceDataGrid.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 100));

                var headerStyle = new Style(typeof(DataGridColumnHeader));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0, 60, 120))));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, new SolidColorBrush(Colors.Orange)));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(60, 60, 100))));
                ReferenceDataGrid.ColumnHeaderStyle = headerStyle;

                var rowStyle = new Style(typeof(DataGridRow));
                rowStyle.Setters.Add(new Setter(DataGridRow.ForegroundProperty, new SolidColorBrush(Colors.White)));
                ReferenceDataGrid.RowStyle = rowStyle;
            }
            else
            {
                this.Background = new SolidColorBrush(Color.FromRgb(0, 40, 100));
                ReferenceDataGrid.Background = new SolidColorBrush(Colors.White);
                ReferenceDataGrid.Foreground = new SolidColorBrush(Colors.Black);
                ReferenceDataGrid.RowBackground = new SolidColorBrush(Colors.White);
                ReferenceDataGrid.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(240, 248, 255));
                ReferenceDataGrid.BorderBrush = new SolidColorBrush(Colors.Transparent);

                var headerStyle = new Style(typeof(DataGridColumnHeader));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0, 60, 120))));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, new SolidColorBrush(Colors.Orange)));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
                ReferenceDataGrid.ColumnHeaderStyle = headerStyle;

                var rowStyle = new Style(typeof(DataGridRow));
                rowStyle.Setters.Add(new Setter(DataGridRow.ForegroundProperty, new SolidColorBrush(Colors.Black)));
                ReferenceDataGrid.RowStyle = rowStyle;
            }
        }

        public void LoadTable(string tableName, string titleName)
        {
            currentTableName = tableName;
            this.Title = $"Справочник: {titleName}";
            LoadData(tableName);
        }

        public void SetUserRole(int role)
        {
            userRole = role;
            AdminButtons.Visibility = role >= 2 ? Visibility.Visible : Visibility.Collapsed;
        }

        public void SetStaffId(int staffId) { currentStaffId = staffId; }
        public void SetUserInfo(string name, string position) { currentUserName = name; currentUserPosition = position; }

        private void ReferenceItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton && !string.IsNullOrEmpty(clickedButton.Tag?.ToString()))
            {
                string tableName = clickedButton.Tag.ToString();
                string titleName = ReferenceTitles.ContainsKey(tableName) ? ReferenceTitles[tableName] : tableName;
                LoadTable(tableName, titleName);
                e.Handled = true;
            }
        }

        private async void LoadData(string tableName)
        {
            try
            {
                ReferenceDataGrid.IsEnabled = false;
                ReferenceDataGrid.Opacity = 0.5;
                SearchTextBox.Clear(); // Сброс поиска при смене таблицы

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
                    ReferenceDataGrid.ItemsSource = currentDataTable.DefaultView;
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
                ReferenceDataGrid.IsEnabled = true;
                ReferenceDataGrid.Opacity = 1.0;
            }
        }

        // 🔍 ПОИСК ПО ТАБЛИЦЕ
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (currentDataTable == null) return;

            string searchText = SearchTextBox.Text.Trim();
            DataView view = currentDataTable.DefaultView;

            if (string.IsNullOrEmpty(searchText))
            {
                view.RowFilter = string.Empty;
            }
            else
            {
                // Экранируем одинарные кавычки для SQL-подобного фильтра
                string safeText = searchText.Replace("'", "''");
                List<string> conditions = new List<string>();

                foreach (DataColumn col in currentDataTable.Columns)
                {
                    if (col.DataType == typeof(string))
                    {
                        conditions.Add($"[{col.ColumnName}] LIKE '%{safeText}%'");
                    }
                }

                view.RowFilter = conditions.Count > 0 ? string.Join(" OR ", conditions) : string.Empty;
            }
            ReferenceDataGrid.ItemsSource = view;
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            ReferenceDataGrid.Focus();
        }
        // 🖱️ ДВОЙНОЙ КЛИК ПО СТРОКЕ
        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 🛑 ИСПРАВЛЕНИЕ: Если включен режим редактирования, выходим из метода.
            // Это позволяет стандартному поведению DataGrid сработать (начать редактирование ячейки, как в Excel).
            if (isEditMode) return;

            if (ReferenceDataGrid.SelectedItem is DataRowView rowView && currentTableName == "tbl_Fire3_Unit")
            {
                // Используем русские названия колонок, так как они уже были переименованы при загрузке
                if (rowView.Row.Table.Columns.Contains("Координата X") && rowView.Row.Table.Columns.Contains("Координата Y"))
                {
                    if (double.TryParse(rowView["Координата X"]?.ToString(), out double lat) &&
                        double.TryParse(rowView["Координата Y"]?.ToString(), out double lng) && lat != 0 && lng != 0)
                    {
                        OpenMapWithFocus(lat, lng);
                    }
                }
            }
        }

        // 🗺️ УМНАЯ КНОПКА "ОТКРЫТЬ КАРТУ"
        private void OpenMapButton_Click(object sender, RoutedEventArgs e)
        {
            if (ReferenceDataGrid.SelectedItem is DataRowView rowView && currentTableName == "tbl_Fire3_Unit")
            {
                if (rowView.Row.Table.Columns.Contains("Координата X") && rowView.Row.Table.Columns.Contains("Координата Y"))
                {
                    if (double.TryParse(rowView["Координата X"]?.ToString(), out double lat) &&
                        double.TryParse(rowView["Координата Y"]?.ToString(), out double lng) && lat != 0 && lng != 0)
                    {
                        OpenMapWithFocus(lat, lng);
                        return;
                    }
                }
            }

            // Если строка не выбрана или это не подразделения, открываем карту как обычно
            MapWindow mapWindow = new MapWindow(currentUserName, currentUserPosition, "г. Новосибирск", WindowMode.View, userRole);
            mapWindow.Owner = this;
            mapWindow.Show();
        }

        private void OpenMapWithFocus(double lat, double lng)
        {
            MapWindow mapWindow = new MapWindow(currentUserName, currentUserPosition, "г. Новосибирск", WindowMode.View, userRole);
            mapWindow.Owner = this;
            mapWindow.Show();

            // Вызываем после Show(), чтобы окно уже было инициализировано
            mapWindow.CenterOnCoordinates(lat, lng);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isEditMode)
            {
                isEditMode = true;
                ReferenceDataGrid.IsReadOnly = false;
                ReferenceDataGrid.CanUserAddRows = false;
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Сохранение изменений будет реализовано.\nStaff_ID: {currentStaffId}, Роль: {userRole}", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}