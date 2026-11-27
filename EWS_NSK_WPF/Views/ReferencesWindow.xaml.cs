using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using EWS_NSK_WPF.Models;
using EWS_NSK_WPF.Services;

namespace EWS_NSK_WPF.Views
{
    public partial class ReferencesWindow : Window
    {
        private static readonly Dictionary<string, Dictionary<string, string>> ColumnTranslations = new Dictionary<string, Dictionary<string, string>>
        {
            ["tbl_EWS"] = new Dictionary<string, string>
            {
                ["Ответств.ПСЧ"] = "Ответств. ПСЧ",
                ["EWS_Map_ID"] = "ID карты",
                ["Тип ИНППВ"] = "Тип ИНППВ",
                ["Номер ПГ"] = "Номер ПГ",
                ["EWS_Name"] = "Наименование",
                ["Статус ИНППВ"] = "Статус",
                ["EWS_TroubleEWS_COD"] = "Код неисправности",
                ["Тип сети"] = "Тип сети",
                ["Диаметр сети"] = "Диаметр сети",
                ["Диаметр ПК"] = "Диаметр ПК",
                ["Кол-во ПА"] = "Кол-во ПА",
                ["Значение осн.п."] = "Значение осн.п.",
                ["Ведомственная принадлежность"] = "Балансодержатель",
                ["EWS_GeoCoord_X"] = "Координата X",
                ["EWS_GeoCoord_Y"] = "Координата Y",
                ["Местоположение ПГ"] = "Местоположение",
                ["Местоположение ПГ (примечание)"] = "Примечание",
                ["Адресный объект - COD"] = "Код адреса",
                ["Номер дома"] = "Номер дома",
                ["Расположение указателя"] = "Расположение указателя",
                ["влево"] = "Влево",
                ["вправо"] = "Вправо",
                ["прямо"] = "Прямо",
                ["EWS_Priviazka_GeoX"] = "Привязка X",
                ["EWS_Priviazka_GeoY"] = "Привязка Y",
                ["Record_Created"] = "Дата создания",
                ["Record_User_COD"] = "Код пользователя",
                ["Статус записи"] = "Статус записи",
                ["№Журнала ППВ (тома)"] = "№ Журнала ППВ",
                ["EWS_Display"] = "Отображение",
                ["EWS_BindDisplay"] = "Привязка отображения"
            },
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
            ["tbl_EWS"] = "Пожарные гидранты",
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

        // ═══ ФИЛЬТРЫ ═══
        private readonly Dictionary<string, HashSet<string>> _filterState = new Dictionary<string, HashSet<string>>();
        private readonly Dictionary<string, List<string>> _columnValues = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, (string cond, string val)> _numericFilters = new Dictionary<string, (string, string)>();
        private bool _hasUnsavedChanges = false;

        // Ссылки на элементы активного попапа (держим в полях — без поиска по дереву)
        private Popup _activePopup;
        private string _activeColumn;
        private bool _activeIsNumeric;
        private StackPanel _valuesPanel;
        private CheckBox _selectAllCheck;
        private TextBox _searchBox;
        private ComboBox _conditionCombo;
        private TextBox _valueBox;

        public ReferencesWindow()
        {
            InitializeComponent();
            ReferenceDataGrid.PreviewMouseWheel += DataGrid_PreviewMouseWheel;
            this.Closing += ReferencesWindow_Closing;
            UserInfoLabel.Text = "Режим: Просмотр справочных данных";

            this.Loaded += (s, e) =>
            {
                ApplyCurrentTheme();
                if (string.IsNullOrEmpty(currentTableName))
                    LoadTable("tbl_Fire3_Unit", "Подразделения ППС");
            };
        }

        private void ApplyCurrentTheme()
        {
            bool isDark = ThemeManager.IsDarkTheme;
            var lineBrush = isDark
                ? new SolidColorBrush(Color.FromRgb(90, 90, 122))
                : new SolidColorBrush(Color.FromRgb(208, 208, 208));

            if (isDark)
            {
                // ═══ ТЁМНАЯ ТЕМА (вид не меняем, просто фиксируем в коде) ═══
                this.Background = new SolidColorBrush(Color.FromRgb(10, 10, 30));
                ReferenceDataGrid.Background = new SolidColorBrush(Color.FromRgb(20, 20, 50));
                ReferenceDataGrid.Foreground = new SolidColorBrush(Colors.White);
                ReferenceDataGrid.RowBackground = new SolidColorBrush(Color.FromRgb(25, 25, 55));
                ReferenceDataGrid.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(35, 35, 70));
                ReferenceDataGrid.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 100));

                LeftPanelBorder.Background = new SolidColorBrush(Color.FromRgb(0, 60, 120));
                LeftPanelBorder.BorderBrush = new SolidColorBrush(Colors.Orange);
                OpenMapButton.Background = new SolidColorBrush(Color.FromRgb(0, 60, 120));
                OpenMapButton.Foreground = new SolidColorBrush(Colors.White);
                OpenMapButton.BorderBrush = new SolidColorBrush(Colors.Orange);
                ClearSearchButton.Background = new SolidColorBrush(Color.FromRgb(0, 60, 120));
                ClearSearchButton.Foreground = new SolidColorBrush(Colors.White);
                ClearSearchButton.BorderBrush = new SolidColorBrush(Colors.Orange);
                UserInfoLabel.Foreground = new SolidColorBrush(Colors.White);
                SetSidebarButtonsColor(new SolidColorBrush(Colors.White));

                var headerStyle = new Style(typeof(DataGridColumnHeader));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0, 60, 120))));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, new SolidColorBrush(Colors.Orange)));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, lineBrush));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
                ReferenceDataGrid.ColumnHeaderStyle = headerStyle;
            }
            else
            {
                // ═══ СВЕТЛАЯ ТЕМА: палитра шапки карты (белый + серый + оранжевый) ═══
                this.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));      // #F5F5F5 как шапка карты
                ReferenceDataGrid.Background = new SolidColorBrush(Colors.White);
                ReferenceDataGrid.Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)); // #333 текст
                ReferenceDataGrid.RowBackground = new SolidColorBrush(Colors.White);
                ReferenceDataGrid.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // #F5F5F5 вместо голубого
                ReferenceDataGrid.BorderBrush = new SolidColorBrush(Color.FromRgb(208, 208, 208));

                // Левая панель — белая карточка с серой рамкой
                LeftPanelBorder.Background = new SolidColorBrush(Colors.White);
                LeftPanelBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(208, 208, 208));

                // Заголовок «Справочники:» — оранжевый, как на карте
                if (LeftPanelBorder.Child is StackPanel sp)
                    foreach (var child in sp.Children)
                        if (child is TextBlock tb) tb.Foreground = new SolidColorBrush(Colors.Orange);

                // Кнопки пунктов — тёмно-серый текст на белом
                SetSidebarButtonsColor(new SolidColorBrush(Color.FromRgb(51, 51, 51)));

                // Кнопки сверху: белая с оранжевой рамкой (как «Режим редактирования» на карте)
                OpenMapButton.Background = new SolidColorBrush(Colors.White);
                OpenMapButton.Foreground = new SolidColorBrush(Colors.Orange);
                OpenMapButton.BorderBrush = new SolidColorBrush(Colors.Orange);
                ClearSearchButton.Background = new SolidColorBrush(Colors.White);
                ClearSearchButton.Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                ClearSearchButton.BorderBrush = new SolidColorBrush(Color.FromRgb(208, 208, 208));

                UserInfoLabel.Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51));

                // Заголовки колонок: белый фон, оранжевый текст, СВЕТЛО-СЕРЫЕ границы (без синего!)
                var headerStyle = new Style(typeof(DataGridColumnHeader));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, new SolidColorBrush(Colors.White)));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, new SolidColorBrush(Colors.Orange)));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, lineBrush));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
                ReferenceDataGrid.ColumnHeaderStyle = headerStyle;
            }

            var rowStyle = new Style(typeof(DataGridRow));
            rowStyle.Setters.Add(new Setter(DataGridRow.BorderBrushProperty, lineBrush));
            rowStyle.Setters.Add(new Setter(DataGridRow.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
            ReferenceDataGrid.RowStyle = rowStyle;
            UpdateUserInfoLabel();
        }
        // Обновляет строку режима: в режиме редактирования — красный текст как на карте
        private void UpdateUserInfoLabel()
        {
            if (isEditMode)
            {
                UserInfoLabel.Text = "✏ РЕЖИМ РЕДАКТИРОВАНИЯ";
                UserInfoLabel.Foreground = new SolidColorBrush(Color.FromRgb(227, 30, 36)); // #E31E24
                UserInfoLabel.FontWeight = FontWeights.Bold;
                UserInfoLabel.FontSize = 13;
            }
            else
            {
                UserInfoLabel.Text = "Режим: Просмотр справочных данных";
                UserInfoLabel.FontWeight = FontWeights.Normal;
                UserInfoLabel.FontSize = 12;
                // Базовый цвет — из темы
                UserInfoLabel.Foreground = ThemeManager.IsDarkTheme
                    ? new SolidColorBrush(Colors.White)
                    : new SolidColorBrush(Color.FromRgb(51, 51, 51));
            }
        }
        // Перекрасить текст кнопок в левой панели (пункты справочников)
        private void SetSidebarButtonsColor(Brush foreground)
        {
            if (LeftPanelBorder.Child is StackPanel sp)
                foreach (var child in sp.Children)
                    if (child is Button btn) btn.Foreground = foreground;
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
                // Если есть несохранённые изменения — спрашиваем перед переключением
                if (_hasUnsavedChanges)
                {
                    var result = MessageBox.Show(
                        "В текущем справочнике есть несохранённые изменения.\nСохранить перед переходом?",
                        "Несохранённые изменения",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Cancel) return; // остаёмся на текущем справочнике
                    if (result == MessageBoxResult.Yes) SaveChangesToDatabase();
                }

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
                SearchTextBox.Clear();
                _filterState.Clear();
                _columnValues.Clear();
                _numericFilters.Clear();
                _hasUnsavedChanges = false;
                UnsavedWarning.Visibility = Visibility.Collapsed;
                isEditMode = false;
                ReferenceDataGrid.IsReadOnly = true;
                EditButton.Content = "Режим редактирования";
                SaveButton.Visibility = Visibility.Collapsed;
                UpdateUserInfoLabel();

                DataTable data = await Task.Run(() => DatabaseHelper.GetTableData(tableName));

                if (data != null && data.Rows.Count > 0)
                {
                    if (ColumnTranslations.ContainsKey(tableName))
                    {
                        var translations = ColumnTranslations[tableName];
                        foreach (DataColumn column in data.Columns)
                            if (translations.ContainsKey(column.ColumnName))
                                column.ColumnName = translations[column.ColumnName];
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

        // ═══ ГЕНЕРАЦИЯ КОЛОНОК ═══
        private void ReferenceDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Column.HeaderTemplate = (DataTemplate)FindResource("ColumnHeaderFilterTemplate");
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
                string safeText = searchText.Replace("'", "''");
                var conditions = new List<string>();
                foreach (DataColumn col in currentDataTable.Columns)
                    if (col.DataType == typeof(string))
                        conditions.Add($"[{col.ColumnName}] LIKE '%{safeText}%'");
                view.RowFilter = conditions.Count > 0 ? string.Join(" OR ", conditions) : string.Empty;
            }
            ReferenceDataGrid.ItemsSource = view;
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            ReferenceDataGrid.Focus();
        }

        // 🖱️ ДВОЙНОЙ КЛИК
        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (isEditMode) return;
            if (ReferenceDataGrid.SelectedItem is DataRowView rowView && currentTableName == "tbl_Fire3_Unit")
            {
                if (rowView.Row.Table.Columns.Contains("Координата X") && rowView.Row.Table.Columns.Contains("Координата Y"))
                {
                    if (double.TryParse(rowView["Координата X"]?.ToString(), out double lat) &&
                        double.TryParse(rowView["Координата Y"]?.ToString(), out double lng) && lat != 0 && lng != 0)
                        OpenMapWithFocus(lat, lng);
                }
            }
        }

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
            MapWindow mapWindow = new MapWindow(currentUserName, currentUserPosition, "г. Новосибирск", WindowMode.View, userRole);
            mapWindow.Owner = this;
            mapWindow.Show();
        }

        private void OpenMapWithFocus(double lat, double lng)
        {
            MapWindow mapWindow = new MapWindow(currentUserName, currentUserPosition, "г. Новосибирск", WindowMode.View, userRole);
            mapWindow.Owner = this;
            mapWindow.Show();
            mapWindow.CenterOnCoordinates(lat, lng);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isEditMode)
            {
                isEditMode = true;
                ReferenceDataGrid.IsReadOnly = false;
                EditButton.Content = "Отменить редактирование";
                SaveButton.Visibility = Visibility.Visible;
            }
            else
            {
                isEditMode = false;
                ReferenceDataGrid.IsReadOnly = true;
                EditButton.Content = "Режим редактирования";
                SaveButton.Visibility = Visibility.Collapsed;
            }
            UpdateUserInfoLabel();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveChangesToDatabase();
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ReferenceDataGrid != null) ReferenceDataGrid.FontSize = e.NewValue;
        }

        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                double delta = e.Delta > 0 ? 1 : -1;
                double newSize = Math.Max(9, Math.Min(16, ReferenceDataGrid.FontSize + delta));
                ReferenceDataGrid.FontSize = newSize;
                if (FontSizeSlider != null) FontSizeSlider.Value = newSize;
                e.Handled = true;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 🎯 EXCEL-ФИЛЬТР: содержимое попапа строится КОДОМ
        // ═══════════════════════════════════════════════════════════

        private static string GetCellValue(DataRow row, string colName)
        {
            object v = row[colName];
            if (v == null || v == DBNull.Value) return null;
            string s = v.ToString();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        private bool IsNumericColumn(string colName)
        {
            var t = currentDataTable.Columns[colName].DataType;
            if (t != typeof(string) && t != typeof(object))
                return true; // настоящий int/double/decimal

            // Если тип string/object — пробуем распарсить первые непустые значения
            var samples = currentDataTable.AsEnumerable()
                .Select(r => GetCellValue(r, colName))
                .Where(v => v != null)
                .Take(20)
                .ToList();

            if (samples.Count == 0) return false;
            return samples.All(s => double.TryParse(s, out _));
        }

        // Открывается попап → строим содержимое кодом
        private void FilterToggle_Checked(object sender, RoutedEventArgs e)
        {
            var toggle = sender as ToggleButton;
            string colName = toggle?.Tag as string;
            if (string.IsNullOrEmpty(colName) || currentDataTable == null) return;
            if (!currentDataTable.Columns.Contains(colName)) return;

            _activeColumn = colName;
            _activeIsNumeric = IsNumericColumn(colName);

            // Находим попап-соседа в том же Grid (шаблон DataTemplate)
            var parent = VisualTreeHelper.GetParent(toggle) as Grid;
            _activePopup = null;
            if (parent != null)
                foreach (var child in parent.Children)
                    if (child is Popup p) { _activePopup = p; break; }
            if (_activePopup == null) return;

            // Кэш уникальных непустых значений (только для текстовых колонок)
            if (!_activeIsNumeric && !_columnValues.ContainsKey(colName))
            {
                _columnValues[colName] = currentDataTable.AsEnumerable()
                    .Select(r => GetCellValue(r, colName))
                    .Where(v => v != null)
                    .Distinct()
                    .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            // Строим содержимое попапа и присваиваем popup.Child
            _activePopup.Child = BuildFilterContent(colName);
        }

        // Строит Border → Grid → элементы фильтра
        private UIElement BuildFilterContent(string colName)
        {
            var root = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 50)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(90, 90, 122)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                MinWidth = 220,
                MaxHeight = 400
            };

            var grid = new Grid { Margin = new Thickness(8) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // поиск/условие
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // (Выбрать все) или поле значения
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // список значений
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // кнопки

            if (_activeIsNumeric)
            {
                // ── ЧИСЛОВОЙ РЕЖИМ: условие + значение ──
                _conditionCombo = new ComboBox
                {
                    FontSize = 11,
                    Margin = new Thickness(0, 0, 0, 6),
                    Style = (Style)FindResource("DarkComboStyle")   // ← добавить
                };
                foreach (var c in new[] { "Все", "Равно", "Не равно", "Больше", "Меньше", "Больше или равно", "Меньше или равно" })
                    _conditionCombo.Items.Add(c);
                _conditionCombo.SelectedIndex = 0;
                Grid.SetRow(_conditionCombo, 0);
                grid.Children.Add(_conditionCombo);

                _valueBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 6),
                    Style = (Style)FindResource("DarkTextBoxStyle")  // ← добавить
                };
                _valueBox.KeyDown += FilterBox_KeyDown;
                Grid.SetRow(_valueBox, 1);
                grid.Children.Add(_valueBox);
            }
            else
            {
                // ── ТЕКСТОВЫЙ РЕЖИМ: поиск + (Выбрать все) + галочки ──
                _searchBox = new TextBox
                {
                    FontSize = 11,
                    Height = 24,
                    Padding = new Thickness(4, 2, 4, 2),
                    Background = new SolidColorBrush(Color.FromRgb(42, 42, 74)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(90, 90, 122)),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                _searchBox.TextChanged += FilterSearch_TextChanged;
                _searchBox.KeyDown += FilterBox_KeyDown;
                Grid.SetRow(_searchBox, 0);
                grid.Children.Add(_searchBox);

                _selectAllCheck = new CheckBox
                {
                    Content = "(Выбрать все)",
                    Foreground = Brushes.White,
                    FontSize = 11,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                _selectAllCheck.Checked += SelectAllCheck_Changed;
                _selectAllCheck.Unchecked += SelectAllCheck_Changed;
                Grid.SetRow(_selectAllCheck, 1);
                grid.Children.Add(_selectAllCheck);

                var scroll = new ScrollViewer
                {
                    MaxHeight = 220,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Margin = new Thickness(0, 4, 0, 4)
                };
                _valuesPanel = new StackPanel();
                scroll.Content = _valuesPanel;
                Grid.SetRow(scroll, 2);
                grid.Children.Add(scroll);

                // Заполняем галочками
                var selected = _filterState.ContainsKey(colName)
                    ? _filterState[colName]
                    : new HashSet<string>(_columnValues[colName]);
                foreach (var value in _columnValues[colName])
                {
                    var cb = new CheckBox
                    {
                        Content = value,
                        Foreground = Brushes.White,
                        FontSize = 11,
                        Margin = new Thickness(0, 2, 0, 2),
                        IsChecked = selected.Contains(value),
                        Tag = value
                    };
                    cb.Checked += ValueCheck_Changed;
                    cb.Unchecked += ValueCheck_Changed;
                    _valuesPanel.Children.Add(cb);
                }
                if (_selectAllCheck != null)
                    _selectAllCheck.IsChecked = selected.Count == _columnValues[colName].Count;
            }

            // Кнопки ОК / Отмена
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            btnGrid.HorizontalAlignment = HorizontalAlignment.Right;
            btnGrid.Margin = new Thickness(0, 8, 0, 0);

            var ok = new Button
            {
                Content = "ОК",
                Width = 60,
                Height = 24,
                Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Foreground = Brushes.White,
                BorderBrush = Brushes.Transparent,
                Cursor = Cursors.Hand
            };
            ok.Click += FilterOk_Click;
            Grid.SetColumn(ok, 0);
            btnGrid.Children.Add(ok);

            var cancel = new Button
            {
                Content = "Отмена",
                Width = 70,
                Height = 24,
                Background = new SolidColorBrush(Color.FromRgb(0, 60, 120)),
                Foreground = Brushes.White,
                BorderBrush = Brushes.Orange,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            cancel.Click += FilterCancel_Click;
            Grid.SetColumn(cancel, 2);
            btnGrid.Children.Add(cancel);

            Grid.SetRow(btnGrid, 3);
            grid.Children.Add(btnGrid);

            root.Child = grid;
            return root;
        }

        private void ValueCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_valuesPanel == null || _selectAllCheck == null) return;
            int total = 0, checkedCount = 0;
            foreach (var child in _valuesPanel.Children)
                if (child is CheckBox cb)
                {
                    total++;
                    if (cb.IsChecked == true) checkedCount++;
                }
            _selectAllCheck.IsChecked = total > 0 && checkedCount == total;
        }

        private void SelectAllCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_valuesPanel == null) return;
            bool check = (sender as CheckBox)?.IsChecked == true;
            foreach (var child in _valuesPanel.Children)
                if (child is CheckBox cb) cb.IsChecked = check;
        }

        private void FilterSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_valuesPanel == null) return;
            string query = _searchBox?.Text?.Trim().ToLowerInvariant() ?? "";
            foreach (var child in _valuesPanel.Children)
                if (child is CheckBox cb)
                {
                    string val = cb.Content?.ToString()?.ToLowerInvariant() ?? "";
                    cb.Visibility = val.Contains(query) ? Visibility.Visible : Visibility.Collapsed;
                }
        }

        // Enter = ОК в любом поле фильтра
        private void FilterBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FilterOk_Click(sender, e);
                e.Handled = true;
            }
        }

        private void FilterOk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_activeColumn) || currentDataTable == null) return;

            if (_activeIsNumeric)
            {
                string condition = _conditionCombo?.SelectedItem?.ToString() ?? "Все";
                string value = _valueBox?.Text?.Trim();

                if (condition == "Все")
                {
                    _filterState.Remove(_activeColumn);
                    _numericFilters.Remove(_activeColumn);
                }
                else
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        MessageBox.Show("Укажите значение для фильтрации.", "Фильтр",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return; // не закрываем, не применяем
                    }
                    _numericFilters[_activeColumn] = (condition, value);
                    _filterState.Remove(_activeColumn);
                }
            }
            else
            {
                var selected = new HashSet<string>();
                if (_valuesPanel != null)
                    foreach (var child in _valuesPanel.Children)
                        if (child is CheckBox cb && cb.IsChecked == true && cb.Visibility == Visibility.Visible)
                            selected.Add(cb.Tag?.ToString());

                if (_columnValues.ContainsKey(_activeColumn) && selected.Count == _columnValues[_activeColumn].Count)
                    _filterState.Remove(_activeColumn);
                else
                    _filterState[_activeColumn] = selected;
                _numericFilters.Remove(_activeColumn);
            }

            ApplyFilters();
            if (_activePopup != null) _activePopup.IsOpen = false;
        }

        private void FilterCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_activePopup != null) _activePopup.IsOpen = false;
        }

        private string ConditionToOperator(string cond) => cond switch
        {
            "Равно" => "=",
            "Не равно" => "<>",
            "Больше" => ">",
            "Меньше" => "<",
            "Больше или равно" => ">=",
            "Меньше или равно" => "<=",
            _ => null
        };

        private void ApplyFilters()
        {
            if (currentDataTable == null) return;
            DataView view = currentDataTable.DefaultView;

            var parts = new List<string>();

            // Текстовые (галочковые) фильтры
            foreach (var kv in _filterState)
            {
                if (kv.Value == null || kv.Value.Count == 0) { parts.Add("1=0"); continue; }
                var orParts = kv.Value.Select(v => $"[{kv.Key}] = '{v.Replace("'", "''")}'");
                parts.Add("(" + string.Join(" OR ", orParts) + ")");
            }

            // Числовые условия
            foreach (var kv in _numericFilters)
            {
                string op = ConditionToOperator(kv.Value.cond);
                if (op == null) continue;
                string val = kv.Value.val.Trim();
                if (!double.TryParse(val, out double num)) continue; // не число — пропускаем

                // Определяем реальный тип данных в колонке
                Type colType = currentDataTable.Columns[kv.Key].DataType;
                bool isRealNumber = colType == typeof(int) || colType == typeof(long) ||
                                    colType == typeof(double) || colType == typeof(float) ||
                                    colType == typeof(decimal) || colType == typeof(short) ||
                                    colType == typeof(byte) || colType == typeof(uint);

                if (isRealNumber)
                {
                    parts.Add($"[{kv.Key}] {op} {val}");
                }
                else
                {
                    // Колонка строковая, но значения числовые — сравниваем как строку
                    // (для '5' и '05' разные результаты, но для наших данных ок)
                    parts.Add($"[{kv.Key}] {op} '{val}'");
                }
            }

            if (parts.Count == 0)
            {
                view.RowFilter = string.Empty;
            }
            else
            {
                try { view.RowFilter = string.Join(" AND ", parts); }
                catch
                {
                    MessageBox.Show("Фильтрация по этой колонке недоступна.", "Фильтр",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            ReferenceDataGrid.ItemsSource = view;
        }
        private void ReferenceDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                _hasUnsavedChanges = true;
                UnsavedWarning.Visibility = Visibility.Visible;
            }
        }

        private void ReferencesWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_hasUnsavedChanges) return;

            var result = MessageBox.Show(
                "В справочнике есть несохранённые изменения.\nСохранить перед закрытием?",
                "Несохранённые изменения",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Сохраняем, потом закрываем
                SaveChangesToDatabase();
            }
            else if (result == MessageBoxResult.Cancel)
            {
                // Отменяем закрытие — пользователь остаётся в окне
                e.Cancel = true;
            }
            // No — закрываем без сохранения
        }
        private void SaveChangesToDatabase()
        {
            // TODO: здесь будет реальное сохранение в БД через DatabaseHelper
            // Пока — просто сбрасываем флаг и скрываем плашку
            _hasUnsavedChanges = false;
            UnsavedWarning.Visibility = Visibility.Collapsed;

            MessageBox.Show("Изменения сохранены.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }  

}       