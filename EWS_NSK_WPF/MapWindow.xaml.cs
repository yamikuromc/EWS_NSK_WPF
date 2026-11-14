using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using EWS_NSK_WPF.Helpers;
using EWS_NSK_WPF.Models; 
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using System.Text.RegularExpressions;
using EWS_NSK_WPF.Services;

namespace EWS_NSK_WPF
{

    public partial class MapWindow : Window
    {

        // Публичный метод для центрирования карты на координатах извне
        public void CenterOnCoordinates(double lat, double lng, int zoom = 16)
        {
            MainMap.Position = new PointLatLng(lat, lng);
            MainMap.Zoom = zoom;
        }
        private EwsMapDataService? _ewsService;
        private int currentUserRole;
        private List<Ewss> _allHydrants = new List<Ewss>();
        private bool _isSearching = false;

        // <--- НОВОЕ: Поле для хранения данных пожарной части пользователя
        private FireStation _userStation;

        private WindowMode _requestedMode;
        private bool _isEditMode = false;

        private bool _hasUnsavedChanges = false;
        private List<Ewss> _modifiedHydrants = new List<Ewss>();
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                _hasUnsavedChanges = value;
                // Предупреждение видно ТОЛЬКО когда есть несохранённые изменения
                UnsavedWarningBorder.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // <--- НОВОЕ: Добавлен параметр userStation в конструктор
        public MapWindow(string userName, string position, string accessLevel, WindowMode requestedMode, int userRole = 0, FireStation userStation = null)
        {
            InitializeComponent();
            currentUserRole = userRole;
            _userStation = userStation; // <--- НОВОЕ: Сохраняем данные части

            // === 1. НАСТРОЙКА КАРТЫ ===
            string mbTilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NewLoadMap.mbtiles");

            if (File.Exists(mbTilesPath))
            {
                try
                {
                    MBTilesProvider.Instance.LoadMBTilesFile(mbTilesPath);
                    MainMap.MapProvider = MBTilesProvider.Instance;
                }
                catch
                {
                    MainMap.MapProvider = GMapProviders.OpenStreetMap;
                }
            }
            else
            {
                MainMap.MapProvider = GMapProviders.OpenStreetMap;
            }

            MainMap.Manager.Mode = AccessMode.ServerAndCache;

            if (_userStation != null)
            {
                MainMap.Position = new PointLatLng(_userStation.CenterLat, _userStation.CenterLng);
                UserInfoLabel.Text = $"🚒 {_userStation.StationName}";   // убрали имя и должность
            }
            else
            {
                MainMap.Position = new PointLatLng(55.0084, 82.9357);
                UserInfoLabel.Text = "";   // пусто — имя не показываем
            }

            MainMap.MinZoom = 10;
            MainMap.MaxZoom = 18;
            MainMap.Zoom = 14;
            MainMap.DragButton = System.Windows.Input.MouseButton.Left;
            MainMap.LevelsKeepInMemory = 5;

            MainMap.OnMapZoomChanged += MainMap_OnMapZoomChanged;

            // === 2. ЗАГРУЗКА ГИДРАНТОВ ===
            LoadHydrantsToMap();
            // Подписка на закрытие окна карты
            this.Closing += MapWindow_Closing;

            // 🎯 НОВОЕ: Обработка режима открытия окна
            _requestedMode = requestedMode;
            if (_requestedMode == WindowMode.Edit && currentUserRole >= 2)
            {
                EnterEditMode_Click(null, null);
            }
            else if (currentUserRole >= 2)
            {
                // Показываем кнопку входа в режим редактирования только админам
                // (Убедись, что в XAML у кнопки EditModeToggleButton стоит Visibility="Collapsed")
                EditModeToggleButton.Visibility = Visibility.Visible;
            }


            this.Loaded += (s, e) => ApplyTheme();

            // === 3. ОСВОБОЖДЕНИЕ РЕСУРСОВ ===
            this.Closed += (s, e) =>
            {
                try
                {
                    MainMap.OnMapZoomChanged -= MainMap_OnMapZoomChanged;
                    MainMap.Markers?.Clear();
                    MainMap.MapProvider = null;
                    MainMap.Dispose();
                }
                catch { }

                try
                {
                    MBTilesProvider.Instance?.Close();
                }
                catch { }
            };
        }
        // Метод-обработчик для XAML (вызывается при нажатии кнопки "Режим редактирования")
        private void EnterEditMode_Click(object sender, RoutedEventArgs e)
        {
            _isEditMode = true;
            EditModeToggleButton.Visibility = Visibility.Collapsed;
            EditModeActions.Visibility = Visibility.Visible;
            EditModeIndicator.Visibility = Visibility.Visible;
            // ⚠ НЕ трогаем UnsavedWarningBorder — оно появится только при реальном изменении
        }

        // Метод-обработчик для XAML (вызывается при нажатии кнопки "Выйти")
        private void ExitEditMode_Click(object sender, RoutedEventArgs e)
        {
            _isEditMode = false;
            EditModeToggleButton.Visibility = currentUserRole >= 2 ? Visibility.Visible : Visibility.Collapsed;
            EditModeActions.Visibility = Visibility.Collapsed;
            EditModeIndicator.Visibility = Visibility.Collapsed;
            // ⚠ Предупреждение НЕ скрываем — если есть несохранённые изменения, оно должно напоминать о себе
        }

        // Этот метод вызывается из HydrantDetailsViewModel при нажатии "Сохранить"
        public void RefreshHydrantInList(Ewss updatedHydrant)
        {
            var existing = _allHydrants.FirstOrDefault(h => h.MarkerId == updatedHydrant.MarkerId);
            if (existing != null)
            {
                existing.EwsStatusCod = updatedHydrant.EwsStatusCod;
                existing.StatusName = updatedHydrant.StatusName;
            }

            // Гидрант сохранён → убираем из списка изменённых
            _modifiedHydrants.RemoveAll(h => h.MarkerId == updatedHydrant.MarkerId);

            // Если больше ничего не изменено — гасим предупреждение
            if (_modifiedHydrants.Count == 0)
            {
                HasUnsavedChanges = false;
            }
        }


        private void LoadHydrantsToMap()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "INPPV.db");

            try
            {
                var hydrants = new List<Ewss>();

                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
                {
                    connection.Open();
                    var cmd = new Microsoft.Data.Sqlite.SqliteCommand(@"
        SELECT 
            [Ответств.ПСЧ], EWS_Map_ID, [Тип ИНППВ], [Номер ПГ], EWS_Name,
            [Статус ИНППВ], EWS_TroubleEWS_COD, [Тип сети],
            [Диаметр сети], [Диаметр ПК], [Кол-во ПА],
            [Значение осн.п.], [Ведомственная принадлежность],
            EWS_GeoCoord_X, EWS_GeoCoord_Y,
            [Местоположение ПГ], [Местоположение ПГ (примечание)], 
            [Адресный объект - COD], [Номер дома],
            [Расположение указателя], влево, вправо, прямо,
            EWS_PriViazka_GeoX, EWS_PriViazka_GeoY,
            Record_Created, Record_User_COD, [Статус записи], [№Журнала ППВ (тома)],
            EWS_Display, EWS_BindDisplay
        FROM tbl_EWS", connection);

                    using var reader = cmd.ExecuteReader();
                    int markerId = 1;

                    while (reader.Read())
                    {
                        string coordX = reader["EWS_GeoCoord_X"]?.ToString() ?? "";
                        string coordY = reader["EWS_GeoCoord_Y"]?.ToString() ?? "";
                        coordX = coordX.Replace(',', '.');
                        coordY = coordY.Replace(',', '.');

                        double lat = 0, lng = 0;
                        double.TryParse(coordX, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out lat);
                        double.TryParse(coordY, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out lng);

                        if (lat == 0 && lng == 0) continue;

                        hydrants.Add(new Ewss
                        {
                            EwsUnitCod = SafeToInt(reader["Ответств.ПСЧ"]),
                            EwsMapId = SafeToInt(reader["EWS_Map_ID"]),
                            EwsTypeCod = SafeToInt(reader["Тип ИНППВ"]),
                            EwsNumber = SafeToInt(reader["Номер ПГ"]),
                            EwsName = reader["EWS_Name"]?.ToString() ?? "",
                            EwsStatusCod = SafeToInt(reader["Статус ИНППВ"]),
                            EwsTroubleEwsCod = reader["EWS_TroubleEWS_COD"]?.ToString() ?? "",
                            EwsPipeTypeCod = SafeToInt(reader["Тип сети"]),
                            EwsDiameterWsCod = SafeToInt(reader["Диаметр сети"]),
                            EwsDiameterPkCod = SafeToInt(reader["Диаметр ПК"]),
                            EwsCountPa = SafeToInt(reader["Кол-во ПА"]),
                            EwsValueCod = SafeToInt(reader["Значение осн.п."]),
                            EwsOwnerCod = SafeToInt(reader["Ведомственная принадлежность"]),
                            LatitudeD = lat,
                            LongitudeD = lng,
                            EwsState = reader["Местоположение ПГ"]?.ToString() ?? "",
                            EwsStateNote = reader["Местоположение ПГ (примечание)"]?.ToString() ?? "",
                            EwsAddressObjectCod = reader["Адресный объект - COD"]?.ToString() ?? "",
                            EwsHouseNumber = reader["Номер дома"]?.ToString() ?? "",
                            EwsPriviazka = reader["Расположение указателя"]?.ToString() ?? "",
                            EwsPrLeft = SafeToInt(reader["влево"]),
                            EwsPrRight = SafeToInt(reader["вправо"]),
                            EwsPrStright = SafeToInt(reader["прямо"]),
                            EwsPriviazkaGeoX = reader["EWS_PriViazka_GeoX"]?.ToString() ?? "",
                            EwsPriviazkaGeoY = reader["EWS_PriViazka_GeoY"]?.ToString() ?? "",
                            RecordCreated = reader["Record_Created"]?.ToString() ?? "",
                            RecordUserCod = SafeToInt(reader["Record_User_COD"]),
                            RecordStatus = reader["Статус записи"]?.ToString() ?? "",
                            EwsJournal = reader["№Журнала ППВ (тома)"]?.ToString() ?? "",
                            EwsDisplay = SafeToInt(reader["EWS_Display"]),
                            EwsBindDisplay = SafeToInt(reader["EWS_BindDisplay"]),
                            MarkerId = markerId++
                        });
                    }
                }

                // === ЗАГРУЗКА СПРАВОЧНИКОВ ===
                var statuses = new Dictionary<int, string>();
                var diameters = new Dictionary<int, string>();
                var owners = new Dictionary<int, string>();
                var addressObjects = new Dictionary<string, (string Name, string Adm)>();

                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
                {
                    conn.Open();
                    var cmd = new Microsoft.Data.Sqlite.SqliteCommand("SELECT * FROM tbl_CheckEWS_StatusEWS", conn);
                    using var r = cmd.ExecuteReader();
                    while (r.Read()) { int id = SafeToInt(r[0]); string name = r[1]?.ToString() ?? ""; if (id > 0 && !string.IsNullOrEmpty(name)) statuses[id] = name; }
                }
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
                {
                    conn.Open();
                    var cmd = new Microsoft.Data.Sqlite.SqliteCommand("SELECT * FROM tbl_EWS_DiameterWS", conn);
                    using var r = cmd.ExecuteReader();
                    while (r.Read()) { int id = SafeToInt(r[0]); string d = r[1]?.ToString() ?? ""; if (id > 0 && !string.IsNullOrEmpty(d)) diameters[id] = d; }
                }
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
                {
                    conn.Open();
                    var cmd = new Microsoft.Data.Sqlite.SqliteCommand("SELECT * FROM tbl_EWS_Owner", conn);
                    using var r = cmd.ExecuteReader();
                    while (r.Read()) { int id = SafeToInt(r[0]); string name = r[1]?.ToString() ?? ""; if (id > 0 && !string.IsNullOrEmpty(name)) owners[id] = name; }
                }
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
                {
                    conn.Open();
                    var cmd = new Microsoft.Data.Sqlite.SqliteCommand("SELECT AddressObject_ID, AddressObject_Name, AddressObject_Adm FROM tbl_RS4_AddressObject", conn);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        string id = r["AddressObject_ID"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(id)) addressObjects[id] = (r["AddressObject_Name"]?.ToString() ?? "", r["AddressObject_Adm"]?.ToString() ?? "");
                    }
                }

                foreach (var h in hydrants)
                {
                    if (h.EwsStatusCod > 0 && statuses.TryGetValue(h.EwsStatusCod, out var s)) h.StatusName = s;
                    if (h.EwsDiameterWsCod > 0 && diameters.TryGetValue(h.EwsDiameterWsCod, out var d)) h.PipeInfo = d;
                    if (h.EwsOwnerCod > 0 && owners.TryGetValue(h.EwsOwnerCod, out var o)) h.OwnerName = o;
                    if (!string.IsNullOrEmpty(h.EwsAddressObjectCod) && addressObjects.TryGetValue(h.EwsAddressObjectCod, out var addrInfo))
                    {
                        h.AddressObjectName = addrInfo.Name;
                        h.AddressObjectAdm = addrInfo.Adm;
                    }
                }

                // <--- НОВОЕ: ФИЛЬТРАЦИЯ ПО РАДИУСУ ВЫЕЗДА ПСЧ
                if (_userStation != null)
                {
                    var filteredHydrants = hydrants.Where(h =>
                    {
                        double dist = Math.Sqrt(
                            Math.Pow(h.LatitudeD - _userStation.CenterLat, 2) +
                            Math.Pow(h.LongitudeD - _userStation.CenterLng, 2)
                        ) * 111000;

                        return dist <= _userStation.RadiusMeters;
                    }).ToList();

                    System.Diagnostics.Debug.WriteLine($"✅ Загружено {filteredHydrants.Count} гидрантов в районе выезда ПСЧ (из {hydrants.Count} всего)");
                    hydrants = filteredHydrants; // Заменяем список на отфильтрованный!
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠ Пользователь не привязан к ПСЧ, показываем все гидранты ({hydrants.Count})");
                }

                int currentZoom = (int)MainMap.Zoom;
                var sortedHydrants = hydrants.OrderBy(h => h.EwsStatusCod == 2 ? 3 : (h.EwsStatusCod == 3 ? 2 : (h.EwsStatusCod == 1 ? 1 : 0))).ToList();

                foreach (var h in sortedHydrants)
                {
                    var marker = HydrantMarker.CreateMarker(h, currentZoom, currentUserRole);
                    if (marker.Shape is FrameworkElement element)
                    {
                        element.ToolTip = $"🚒 Гидрант №{h.DisplayNumber}\n📊 Статус: {h.StatusName}\n📍 Адрес: {h.AddressText}\n🔧 Труба: {h.PipeInfo}";
                    }
                    MainMap.Markers.Add(marker);
                }


                _allHydrants = hydrants;
                UserInfoLabel.Text += $" | ✅ Загружено: {_allHydrants.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}\n\n{ex.StackTrace}", "Ошибка загрузки", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int SafeToInt(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            string str = value.ToString();
            if (string.IsNullOrEmpty(str) || str == "-") return 0;
            return int.TryParse(str, out int result) ? result : 0;
        }

        private void MainMap_OnMapZoomChanged()
        {
            int currentZoom = (int)MainMap.Zoom;
            foreach (var marker in MainMap.Markers) HydrantMarker.UpdateMarkerSize(marker, currentZoom);
        }

        private void ApplyTheme()
        {
            if (ThemeManager.IsDarkTheme)
            {
                this.Background = new SolidColorBrush(Color.FromRgb(20, 20, 40));
                HeaderBorder.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                MapTitleText.Foreground = new SolidColorBrush(Colors.Orange);
                UserInfoLabel.Foreground = new SolidColorBrush(Colors.White);
                SearchTextBox.Background = new SolidColorBrush(Color.FromRgb(42, 42, 74));
                SearchTextBox.Foreground = new SolidColorBrush(Colors.White);
                SearchTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 100));
                var darkBg = new SolidColorBrush(Color.FromRgb(42, 42, 74));
                var darkBorder = new SolidColorBrush(Color.FromRgb(60, 60, 100));
                var darkText = new SolidColorBrush(Colors.White);
                ZoomButtonsBorder.Background = darkBg;
                ZoomInButton.Background = darkBg; ZoomInButton.BorderBrush = darkBorder; ZoomInButton.Foreground = darkText;
                ZoomOutButton.Background = darkBg; ZoomOutButton.BorderBrush = darkBorder; ZoomOutButton.Foreground = darkText;
                HomeButtonBorder.Background = darkBg;
                HomeButton.Background = darkBg; HomeButton.BorderBrush = darkBorder; HomeButton.Foreground = darkText;
            }
            else
            {
                this.Background = new SolidColorBrush(Color.FromRgb(245, 245, 250));
                HeaderBorder.Background = new SolidColorBrush(Color.FromRgb(240, 240, 245));
                MapTitleText.Foreground = new SolidColorBrush(Colors.Orange);
                UserInfoLabel.Foreground = new SolidColorBrush(Color.FromRgb(0, 40, 100));
                SearchTextBox.Background = new SolidColorBrush(Colors.White);
                SearchTextBox.Foreground = new SolidColorBrush(Colors.Black);
                SearchTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                var lightBg = new SolidColorBrush(Colors.White);
                var lightBorder = new SolidColorBrush(Color.FromRgb(221, 221, 221));
                var lightText = new SolidColorBrush(Colors.Black);
                ZoomButtonsBorder.Background = lightBg;
                ZoomInButton.Background = lightBg; ZoomInButton.BorderBrush = lightBorder; ZoomInButton.Foreground = lightText;
                ZoomOutButton.Background = lightBg; ZoomOutButton.BorderBrush = lightBorder; ZoomOutButton.Foreground = lightText;
                HomeButtonBorder.Background = lightBg;
                HomeButton.Background = lightBg; HomeButton.BorderBrush = lightBorder; HomeButton.Foreground = lightText;
            }
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e) { if (MainMap.Zoom < MainMap.MaxZoom) MainMap.Zoom++; }
        private void ZoomOut_Click(object sender, RoutedEventArgs e) { if (MainMap.Zoom > MainMap.MinZoom) MainMap.Zoom--; }
        private void GoHome_Click(object sender, RoutedEventArgs e)
        {
            if (_userStation != null)
            {
                MainMap.Position = new PointLatLng(_userStation.CenterLat, _userStation.CenterLng);
            }
            else
            {
                MainMap.Position = new PointLatLng(55.0084, 82.9357);
            }
            MainMap.Zoom = 14;
        }

        private async void SearchHydrant_Click(object sender, RoutedEventArgs e) { await PerformSearch(); }
        private async void SearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter) { await PerformSearch(); e.Handled = true; }
        }

        private async Task PerformSearch()
        {
            string rawQuery = SearchTextBox.Text.Trim();
            if (string.IsNullOrEmpty(rawQuery)) return;

            if (_isSearching) { System.Diagnostics.Debug.WriteLine("⚠ Поиск уже выполняется, ждём..."); return; }
            _isSearching = true;

            try
            {
                Ewss foundHydrant = null;
                string queryLower = rawQuery.ToLower();

                bool hasLetters = queryLower.Any(c => char.IsLetter(c));
                bool hasOnlyDigits = !string.IsNullOrWhiteSpace(queryLower) && queryLower.All(c => char.IsDigit(c) || char.IsWhiteSpace(c) || c == '-' || c == '.');
                var allNumbers = System.Text.RegularExpressions.Regex.Matches(rawQuery, @"\d+\.?\d*").Cast<System.Text.RegularExpressions.Match>().Select(m => m.Value).ToArray();

                System.Diagnostics.Debug.WriteLine($"🔍 Запрос: \"{rawQuery}\" | Буквы: {hasLetters} | Только цифры: {hasOnlyDigits} | Числа: [{string.Join(", ", allNumbers)}]");

                // === 1. ПРОВЕРКА: ЭТО КООРДИНАТЫ? ===
                var coordPattern = System.Text.RegularExpressions.Regex.Matches(rawQuery, @"(\d+[.,]?\d*)[,\s;]+(\d+[.,]?\d*)");
                if (coordPattern.Count > 0)
                {
                    try
                    {
                        string val1Str = coordPattern[0].Groups[1].Value.Replace(',', '.');
                        string val2Str = coordPattern[0].Groups[2].Value.Replace(',', '.');
                        double val1 = double.Parse(val1Str, System.Globalization.CultureInfo.InvariantCulture);
                        double val2 = double.Parse(val2Str, System.Globalization.CultureInfo.InvariantCulture);

                        bool looksLikeCoords = (val1 > 50 && val1 < 60 && val2 > 70 && val2 < 90) || (val2 > 50 && val2 < 60 && val1 > 70 && val1 < 90);

                        if (looksLikeCoords)
                        {
                            double lat = (Math.Abs(val1 - 55) < Math.Abs(val2 - 55)) ? val1 : val2;
                            double lng = (Math.Abs(val1 - 83) < Math.Abs(val2 - 83)) ? val1 : val2;

                            var candidates = _allHydrants.Where(h => Math.Abs(h.LatitudeD - lat) < 0.01 && Math.Abs(h.LongitudeD - lng) < 0.01)
                                .OrderBy(h => Math.Sqrt(Math.Pow(h.LatitudeD - lat, 2) + Math.Pow(h.LongitudeD - lng, 2))).ToList();

                            if (candidates.Any())
                            {
                                foundHydrant = candidates.First();
                                double dist = Math.Sqrt(Math.Pow(foundHydrant.LatitudeD - lat, 2) + Math.Pow(foundHydrant.LongitudeD - lng, 2)) * 111000;
                                System.Diagnostics.Debug.WriteLine($"✅ Найдено по координатам! Гидрант №{foundHydrant.DisplayNumber}, расстояние: {dist:F0} м");
                            }
                        }
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"⚠ Ошибка парсинга координат: {ex.Message}"); }
                }

                // === 2. ЕСЛИ НЕ КООРДИНАТЫ — ИЩЕМ ДАЛЬШЕ ===
                if (foundHydrant == null)
                {
                    if (hasLetters)
                    {
                        System.Diagnostics.Debug.WriteLine($"📍 Стратегия: адресный поиск по \"{queryLower}\"");
                        var tokens = queryLower.Split(new[] { ' ', ',', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
                        var words = tokens.Where(t => t.Any(c => char.IsLetter(c))).ToList();
                        var numbers = tokens.Where(t => t.All(char.IsDigit)).ToList();

                        foundHydrant = _allHydrants.FirstOrDefault(h =>
                        {
                            bool wordsMatch = words.All(word =>
                            (!string.IsNullOrEmpty(h.AddressObjectName) && h.AddressObjectName.ToLower().Contains(word)) ||
                            (!string.IsNullOrEmpty(h.AddressObjectAdm) && h.AddressObjectAdm.ToLower().Contains(word)) ||
                            (!string.IsNullOrEmpty(h.EwsStateNote) && h.EwsStateNote.ToLower().Contains(word))
                        );


                            if (!wordsMatch) return false;
                            if (numbers.Any()) return numbers.All(num => !string.IsNullOrEmpty(h.EwsHouseNumber) && System.Text.RegularExpressions.Regex.IsMatch(h.EwsHouseNumber.ToLower(), $@"\b{num}\b"));
                            return true;
                        });

                        if (foundHydrant != null) System.Diagnostics.Debug.WriteLine($"✅ Найдено локально: Гидрант №{foundHydrant.DisplayNumber}");
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"🌐 Локально не найдено. Запрашиваем координаты у Nominatim...");

                            // <--- НОВОЕ: Передаем _userStation в сервис геокодинга
                            var coords = await GeocodingService.GetCoordinatesAsync(rawQuery, _userStation);

                            if (coords.HasValue)
                            {
                                System.Diagnostics.Debug.WriteLine($"✅ Геокодинг успешен: {coords.Value.Lat}, {coords.Value.Lng}");
                                foundHydrant = _allHydrants.OrderBy(h => {
                                    double dist = Math.Sqrt(Math.Pow(h.LatitudeD - coords.Value.Lat, 2) + Math.Pow(h.LongitudeD - coords.Value.Lng, 2));
                                    int statusPriority = (h.EwsStatusCod != 2) ? 0 : 1;
                                    return (statusPriority, dist);
                                }).FirstOrDefault();

                                if (foundHydrant != null)
                                {
                                    double distMeters = Math.Sqrt(Math.Pow(foundHydrant.LatitudeD - coords.Value.Lat, 2) + Math.Pow(foundHydrant.LongitudeD - coords.Value.Lng, 2)) * 111000;
                                    System.Diagnostics.Debug.WriteLine($"🎯 Найден ближайший географически: Гидрант №{foundHydrant.DisplayNumber}, расстояние: {distMeters:F0} м");
                                }
                            }
                            else { System.Diagnostics.Debug.WriteLine($"❌ Nominatim не смог определить координаты для: {rawQuery}"); }
                        }
                    }
                    else if (hasOnlyDigits)
                    {
                        System.Diagnostics.Debug.WriteLine($"🔢 Стратегия: поиск по номеру");
                        if (allNumbers.Length >= 2)
                        {
                            int number = int.Parse(allNumbers[0]);
                            int diameter = int.Parse(allNumbers[1]);
                            foundHydrant = _allHydrants.FirstOrDefault(h => h.EwsNumber == number && (h.EwsDiameterWsCod == diameter || h.PipeInfo.Contains(diameter.ToString())));
                            if (foundHydrant == null) foundHydrant = _allHydrants.FirstOrDefault(h => h.EwsNumber == number);
                        }
                        else if (allNumbers.Length == 1)
                        {
                            int number = int.Parse(allNumbers[0]);
                            foundHydrant = _allHydrants.FirstOrDefault(h => h.EwsNumber == number);
                        }
                        if (foundHydrant != null) System.Diagnostics.Debug.WriteLine($"✅ Найдено по номеру! Гидрант №{foundHydrant.DisplayNumber}");
                    }
                }

                // === 3. РЕЗУЛЬТАТ ===
                if (foundHydrant != null)
                {
                    MainMap.Position = new PointLatLng(foundHydrant.LatitudeD, foundHydrant.LongitudeD);
                    MainMap.Zoom = 17;

                    
                    var detailsWindow = new HydrantDetailsWindow(foundHydrant, currentUserRole, _isEditMode);


                    detailsWindow.Owner = this;

                    // 🎯 ПОДПИСКА НА МГНОВЕННУЮ ПЕРЕКРАСКУ (при поиске)
                    if (detailsWindow.ViewModel != null)
                    {
                        detailsWindow.ViewModel.OnStatusChanged = (updatedHydrant) =>
                        {
                            var marker = MainMap.Markers.FirstOrDefault(m => m.Tag as int? == updatedHydrant.MarkerId);
                            if (marker != null)
                            {
                                HydrantMarker.UpdateMarkerColors(marker, updatedHydrant);
                            }
                            MarkHydrantModified(updatedHydrant);
                        };
                    }

                    detailsWindow.ShowDialog();
                }
                else
                {
                    MessageBox.Show($"Гидрант по запросу \"{rawQuery}\" не найден.\n\nПопробуйте:\n• Ввести номер гидранта (например: 16)\n• Ввести адрес (например: Полевая 35)\n• Ввести координаты (например: 54.92, 82.98)", "Не найдено", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            finally { _isSearching = false; }
        }
        // Публичное свойство, чтобы HydrantMarker мог узнать текущий режим
        public bool IsInEditMode => _isEditMode;
        private void ClearSearch_Click(object sender, RoutedEventArgs e) { SearchTextBox.Clear(); SearchTextBox.Focus(); }

        private void MapWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "У вас есть несохраненные изменения!\n\nСохранить их перед закрытием?",
                    "Несохраненные изменения",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // TODO: Здесь будет вызов сохранения всех изменений в БД
                    MessageBox.Show("Изменения сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    _hasUnsavedChanges = false;
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true; // Отменяем закрытие
                }
                // Если No - закрываем без сохранения
            }


        }
        // Помечает конкретный гидрант как изменённый + включает предупреждение
        public void MarkHydrantModified(Ewss hydrant)
        {
            if (!_modifiedHydrants.Any(h => h.MarkerId == hydrant.MarkerId))
            {
                _modifiedHydrants.Add(hydrant);
            }
            HasUnsavedChanges = true;
        }

        // Старый метод оставляем как обёртку (на всякий случай)
        public void MarkAsUnsavedChanges()
        {
            HasUnsavedChanges = true;
        }
        // ==========================================
        // Сохранение изменений из режима редактирования
        // ==========================================
        private async void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (_modifiedHydrants.Count == 0)
            {
                MessageBox.Show("Нет несохранённых изменений.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "INPPV.db");
                using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
                await conn.OpenAsync();

                int savedCount = 0;
                foreach (var h in _modifiedHydrants)
                {
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE tbl_EWS SET [Статус ИНППВ] = @s WHERE EWS_Map_ID = @id";
                    cmd.Parameters.AddWithValue("@s", h.EwsStatusCod);
                    cmd.Parameters.AddWithValue("@id", h.EwsMapId);
                    await cmd.ExecuteNonQueryAsync();
                    savedCount++;
                }

                // Очищаем список измененных и гасим предупреждение
                _modifiedHydrants.Clear();
                HasUnsavedChanges = false;

                MessageBox.Show($"Успешно сохранено {savedCount} изменённый(ых) гидрант(а/ов)!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


    }

}