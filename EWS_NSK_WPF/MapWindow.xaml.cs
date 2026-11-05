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

namespace EWS_NSK_WPF
{
    public partial class MapWindow : Window
    {
        private EwsMapDataService? _ewsService;
        private int currentUserRole;
        private List<Ewss> _allHydrants = new List<Ewss>();

        public MapWindow(string userName, string position, string accessLevel, WindowMode requestedMode, int userRole = 0)
        {
            InitializeComponent();
            currentUserRole = userRole;

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
            MainMap.Position = new PointLatLng(55.0084, 82.9357);
            MainMap.MinZoom = 10;
            MainMap.MaxZoom = 18;
            MainMap.Zoom = 14;
            MainMap.DragButton = System.Windows.Input.MouseButton.Left;
            MainMap.LevelsKeepInMemory = 5;

            MainMap.OnMapZoomChanged += MainMap_OnMapZoomChanged;
            UserInfoLabel.Text = $"Пользователь: {userName} ({position})";

            // === 2. ЗАГРУЗКА ГИДРАНТОВ ===
            LoadHydrantsToMap();

            // Применяем тему после загрузки
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

                // <--- НОВОЕ: Справочник адресных объектов
                var addressObjects = new Dictionary<string, (string Name, string Adm)>();

                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
                {
                    conn.Open();
                    var cmd = new Microsoft.Data.Sqlite.SqliteCommand("SELECT * FROM tbl_CheckEWS_StatusEWS", conn);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        int id = SafeToInt(r[0]);
                        string name = r[1]?.ToString() ?? "";
                        if (id > 0 && !string.IsNullOrEmpty(name)) statuses[id] = name;
                    }
                }

                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
                {
                    conn.Open();
                    var cmd = new Microsoft.Data.Sqlite.SqliteCommand("SELECT * FROM tbl_EWS_DiameterWS", conn);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        int id = SafeToInt(r[0]);
                        string d = r[1]?.ToString() ?? "";
                        if (id > 0 && !string.IsNullOrEmpty(d)) diameters[id] = d;
                    }
                }

                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
                {
                    conn.Open();
                    var cmd = new Microsoft.Data.Sqlite.SqliteCommand("SELECT * FROM tbl_EWS_Owner", conn);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        int id = SafeToInt(r[0]);
                        string name = r[1]?.ToString() ?? "";
                        if (id > 0 && !string.IsNullOrEmpty(name)) owners[id] = name;
                    }
                }

                // <--- НОВОЕ: Загрузка справочника адресов
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
                {
                    conn.Open();
                    var cmd = new Microsoft.Data.Sqlite.SqliteCommand(
                        "SELECT AddressObject_ID, AddressObject_Name, AddressObject_Adm FROM tbl_RS4_AddressObject", conn);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        string id = r["AddressObject_ID"]?.ToString() ?? "";
                        string name = r["AddressObject_Name"]?.ToString() ?? "";
                        string adm = r["AddressObject_Adm"]?.ToString() ?? "";

                        if (!string.IsNullOrEmpty(id))
                        {
                            addressObjects[id] = (name, adm);
                        }
                    }
                }

                // === СОПОСТАВЛЕНИЕ СПРАВОЧНИКОВ С ГИДРАНТАМИ ===
                foreach (var h in hydrants)
                {
                    if (h.EwsStatusCod > 0 && statuses.TryGetValue(h.EwsStatusCod, out var s)) h.StatusName = s;
                    if (h.EwsDiameterWsCod > 0 && diameters.TryGetValue(h.EwsDiameterWsCod, out var d)) h.PipeInfo = d;
                    if (h.EwsOwnerCod > 0 && owners.TryGetValue(h.EwsOwnerCod, out var o)) h.OwnerName = o;

                    // <--- НОВОЕ: Привязка названия улицы/микрорайона к гидранту
                    if (!string.IsNullOrEmpty(h.EwsAddressObjectCod) &&
                        addressObjects.TryGetValue(h.EwsAddressObjectCod, out var addrInfo))
                    {
                        h.AddressObjectName = addrInfo.Name;
                        h.AddressObjectAdm = addrInfo.Adm;
                    }
                }

                int currentZoom = (int)MainMap.Zoom;

                var sortedHydrants = hydrants.OrderBy(h =>
                {
                    if (h.EwsStatusCod == 2) return 3;
                    if (h.EwsStatusCod == 3) return 2;
                    if (h.EwsStatusCod == 1) return 1;
                    return 0;
                }).ToList();

                foreach (var h in sortedHydrants)
                {
                    var marker = HydrantMarker.CreateMarker(h, currentZoom, currentUserRole);

                    if (marker.Shape is FrameworkElement element)
                    {
                        element.ToolTip = $"🚒 Гидрант №{h.DisplayNumber}\n" +
                                          $"📊 Статус: {h.StatusName}\n" +
                                          $"📍 Адрес: {h.AddressText}\n" +
                                          $"🔧 Труба: {h.PipeInfo}";
                    }

                    MainMap.Markers.Add(marker);
                }

                _allHydrants = hydrants;
                UserInfoLabel.Text += $" | ✅ Загружено гидрантов на карту: {hydrants.Count}";
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
            foreach (var marker in MainMap.Markers)
            {
                HydrantMarker.UpdateMarkerSize(marker, currentZoom);
            }
        }

        // === ПРОСТОЙ И НАДЁЖНЫЙ МЕТОД ТЕМЫ ===
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

                // === КНОПКИ УПРАВЛЕНИЯ (Тёмная тема) ===
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

                // === КНОПКИ УПРАВЛЕНИЯ (Светлая тема) ===
                var lightBg = new SolidColorBrush(Colors.White);
                var lightBorder = new SolidColorBrush(Color.FromRgb(221, 221, 221)); // #DDD
                var lightText = new SolidColorBrush(Colors.Black);

                ZoomButtonsBorder.Background = lightBg;
                ZoomInButton.Background = lightBg; ZoomInButton.BorderBrush = lightBorder; ZoomInButton.Foreground = lightText;
                ZoomOutButton.Background = lightBg; ZoomOutButton.BorderBrush = lightBorder; ZoomOutButton.Foreground = lightText;

                HomeButtonBorder.Background = lightBg;
                HomeButton.Background = lightBg; HomeButton.BorderBrush = lightBorder; HomeButton.Foreground = lightText;
            }
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (MainMap.Zoom < MainMap.MaxZoom) MainMap.Zoom++;
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (MainMap.Zoom > MainMap.MinZoom) MainMap.Zoom--;
        }

        private void GoHome_Click(object sender, RoutedEventArgs e)
        {
            MainMap.Position = new PointLatLng(55.0084, 82.9357);
            MainMap.Zoom = 14;
        }

        private void SearchHydrant_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void SearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                PerformSearch();
                e.Handled = true;
            }
        }

        private void PerformSearch()
        {
            string rawQuery = SearchTextBox.Text.Trim();
            if (string.IsNullOrEmpty(rawQuery)) return;

            Ewss foundHydrant = null;
            string queryLower = rawQuery.ToLower();

            // Определяем тип запроса
            bool hasLetters = queryLower.Any(c => char.IsLetter(c));
            bool hasOnlyDigits = !string.IsNullOrWhiteSpace(queryLower) && queryLower.All(c => char.IsDigit(c) || char.IsWhiteSpace(c) || c == '-' || c == '.');
            var allNumbers = System.Text.RegularExpressions.Regex.Matches(rawQuery, @"\d+\.?\d*")
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(m => m.Value)
                .ToArray();

            System.Diagnostics.Debug.WriteLine($"🔍 Запрос: \"{rawQuery}\" | Буквы: {hasLetters} | Только цифры: {hasOnlyDigits} | Числа: [{string.Join(", ", allNumbers)}]");

            // === 1. ПРОВЕРКА: ЭТО КООРДИНАТЫ? ===
            // Ищем два числа (где дробная часть может быть через точку ИЛИ запятую), 
            // разделенных запятой, пробелом или точкой с запятой.
            var coordPattern = System.Text.RegularExpressions.Regex.Matches(
                rawQuery,
                @"(\d+[.,]?\d*)[,\s;]+(\d+[.,]?\d*)"
            );

            if (coordPattern.Count > 0)
            {
                try
                {
                    // ВАЖНО: Заменяем запятую на точку внутри самих чисел перед парсингом, 
                    // чтобы InvariantCulture корректно их прочитал
                    string val1Str = coordPattern[0].Groups[1].Value.Replace(',', '.');
                    string val2Str = coordPattern[0].Groups[2].Value.Replace(',', '.');

                    double val1 = double.Parse(val1Str, System.Globalization.CultureInfo.InvariantCulture);
                    double val2 = double.Parse(val2Str, System.Globalization.CultureInfo.InvariantCulture);

                    System.Diagnostics.Debug.WriteLine($"🔍 Координаты распознаны: val1={val1}, val2={val2}");

                    // Проверяем, что это похоже на координаты Новосибирска (широта ~55, долгота ~83)
                    bool looksLikeCoords = (val1 > 50 && val1 < 60 && val2 > 70 && val2 < 90) ||
                                           (val2 > 50 && val2 < 60 && val1 > 70 && val1 < 90);

                    if (looksLikeCoords)
                    {
                        double lat = (Math.Abs(val1 - 55) < Math.Abs(val2 - 55)) ? val1 : val2;
                        double lng = (Math.Abs(val1 - 83) < Math.Abs(val2 - 83)) ? val1 : val2;

                        System.Diagnostics.Debug.WriteLine($"📍 Определили: Широта={lat}, Долгота={lng}");

                        // Радиус поиска 0.01 градуса (~1 км)
                        var candidates = _allHydrants
                            .Where(h => Math.Abs(h.LatitudeD - lat) < 0.01 && Math.Abs(h.LongitudeD - lng) < 0.01)
                            .OrderBy(h => Math.Sqrt(Math.Pow(h.LatitudeD - lat, 2) + Math.Pow(h.LongitudeD - lng, 2)))
                            .ToList();

                        if (candidates.Any())
                        {
                            foundHydrant = candidates.First();
                            double dist = Math.Sqrt(Math.Pow(foundHydrant.LatitudeD - lat, 2) + Math.Pow(foundHydrant.LongitudeD - lng, 2)) * 111000;
                            System.Diagnostics.Debug.WriteLine($"✅ Найдено по координатам! Гидрант №{foundHydrant.DisplayNumber}, расстояние: {dist:F0} м");
                        }
                        else
                        {
                            var nearest = _allHydrants.OrderBy(h => Math.Sqrt(Math.Pow(h.LatitudeD - lat, 2) + Math.Pow(h.LongitudeD - lng, 2))).First();
                            System.Diagnostics.Debug.WriteLine($"❌ В радиусе 1км ничего нет. Ближайший гидрант: №{nearest.DisplayNumber} (коорд: {nearest.LatitudeD}, {nearest.LongitudeD})");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠ Числа найдены, но не похожи на координаты Новосибирска (val1={val1}, val2={val2})");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠ Ошибка парсинга координат: {ex.Message}");
                }
            }

            // === 2. ЕСЛИ НЕ НАШЛИ — ОПРЕДЕЛЯЕМ СТРАТЕГИЮ ПО ТИПУ ЗАПРОСА ===
            if (foundHydrant == null)
            {
                // === 2. ЕСЛИ НЕ НАШЛИ — ОПРЕДЕЛЯЕМ СТРАТЕГИЮ ПО ТИПУ ЗАПРОСА ===
                if (foundHydrant == null)
                {
                    if (hasLetters)
                    {
                        // === СТРАТЕГИЯ А: УМНЫЙ АДРЕСНЫЙ ПОИСК ===
                        System.Diagnostics.Debug.WriteLine($"📍 Стратегия: умный адресный поиск по \"{queryLower}\"");

                        var tokens = queryLower.Split(new[] { ' ', ',', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
                        var words = tokens.Where(t => t.Any(c => char.IsLetter(c))).ToList();
                        var numbers = tokens.Where(t => t.All(char.IsDigit)).ToList();

                        System.Diagnostics.Debug.WriteLine($"   Слова: [{string.Join(", ", words)}], Числа: [{string.Join(", ", numbers)}]");

                        // 1. Ищем ТОЧНОЕ совпадение
                        foundHydrant = _allHydrants.FirstOrDefault(h =>
                        {
                            bool wordsMatch = words.All(word =>
                                (!string.IsNullOrEmpty(h.AddressObjectName) && h.AddressObjectName.ToLower().Contains(word)) ||
                                (!string.IsNullOrEmpty(h.AddressObjectAdm) && h.AddressObjectAdm.ToLower().Contains(word)) ||
                                (!string.IsNullOrEmpty(h.EwsStateNote) && h.EwsStateNote.ToLower().Contains(word))
                            );

                            if (!wordsMatch) return false;

                            if (numbers.Any())
                            {
                                return numbers.All(num =>
                                    !string.IsNullOrEmpty(h.EwsHouseNumber) &&
                                    System.Text.RegularExpressions.Regex.IsMatch(h.EwsHouseNumber.ToLower(), $@"\b{num}\b")
                                );
                            }

                            return true;
                        });

                        // 2. Если не нашли — ищем ближайший на улице
                        if (foundHydrant == null && words.Any())
                        {
                            System.Diagnostics.Debug.WriteLine($" Точное совпадение не найдено. Ищем гидранты на улице...");

                            var hydrantsOnStreet = _allHydrants.Where(h =>
                                (!string.IsNullOrEmpty(h.AddressObjectName) && words.All(w => h.AddressObjectName.ToLower().Contains(w))) ||
                                (!string.IsNullOrEmpty(h.AddressObjectAdm) && words.All(w => h.AddressObjectAdm.ToLower().Contains(w)))
                            ).ToList();

                            System.Diagnostics.Debug.WriteLine($"   Найдено {hydrantsOnStreet.Count} гидрантов на улице");

                            if (hydrantsOnStreet.Any())
                            {
                                int targetHouseNumber = numbers.Any() ? int.Parse(numbers.First()) : 0;

                                var sortedHydrants = hydrantsOnStreet.OrderBy(h =>
                                {
                                    // Приоритет 1: статус (исправен = 0, неисправен = 1)
                                    int statusPriority = (h.EwsStatusCod != 2) ? 0 : 1;

                                    // Приоритет 2: близость номера дома
                                    int houseDiff = 9999;
                                    if (targetHouseNumber > 0 && !string.IsNullOrEmpty(h.EwsHouseNumber))
                                    {
                                        var houseMatch = System.Text.RegularExpressions.Regex.Match(h.EwsHouseNumber, @"\d+");
                                        if (houseMatch.Success && int.TryParse(houseMatch.Value, out int houseNum))
                                        {
                                            houseDiff = Math.Abs(houseNum - targetHouseNumber);
                                        }
                                    }

                                    System.Diagnostics.Debug.WriteLine($"   Гидрант №{h.DisplayNumber}: дом='{h.EwsHouseNumber}', статус={(h.EwsStatusCod == 2 ? "неисправен" : "исправен")}, отклонение={houseDiff}");

                                    return (statusPriority, houseDiff);
                                });

                                foundHydrant = sortedHydrants.First();
                                System.Diagnostics.Debug.WriteLine($"✅ Выбран: Гидрант №{foundHydrant.DisplayNumber}, адрес: {foundHydrant.AddressObjectName}, д. {foundHydrant.EwsHouseNumber}");
                            }
                            else
                            {
                                // === 3. Если улица не найдена — покажем какие есть ===
                                var allStreets = _allHydrants.Select(h => h.AddressObjectName).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s);
                                System.Diagnostics.Debug.WriteLine($"⚠ Улица не найдена! Доступные улицы в базе:");
                                foreach (var street in allStreets.Take(15))
                                {
                                    System.Diagnostics.Debug.WriteLine($"   - {street}");
                                }
                            }
                        }

                        if (foundHydrant == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ По адресу \"{queryLower}\" ничего не найдено");
                        }
                    }
                }
                    else if (hasOnlyDigits)
                    {
                        // === СТРАТЕГИЯ Б: ТОЛЬКО ЧИСЛА (номер гидранта, возможно с диаметром) ===
                        System.Diagnostics.Debug.WriteLine($"🔢 Стратегия: поиск по номеру");

                        if (allNumbers.Length >= 2)
                        {
                            // Формат "16 100" → номер=16, диаметр=100
                            int number = int.Parse(allNumbers[0]);
                            int diameter = int.Parse(allNumbers[1]);
                            System.Diagnostics.Debug.WriteLine($"   Ищем: номер={number}, диаметр={diameter}");

                            foundHydrant = _allHydrants.FirstOrDefault(h =>
                                h.EwsNumber == number &&
                                (h.EwsDiameterWsCod == diameter || h.PipeInfo.Contains(diameter.ToString()))
                            );

                            // Если точного совпадения нет — ищем просто по номеру
                            if (foundHydrant == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"   Точное совпадение не найдено, ищем только по номеру {number}");
                                foundHydrant = _allHydrants.FirstOrDefault(h => h.EwsNumber == number);
                            }
                        }
                        else if (allNumbers.Length == 1)
                        {
                            // Формат "16" → просто номер
                            int number = int.Parse(allNumbers[0]);
                            System.Diagnostics.Debug.WriteLine($"   Ищем номер: {number}");
                            foundHydrant = _allHydrants.FirstOrDefault(h => h.EwsNumber == number);
                        }

                        if (foundHydrant != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ Найдено по номеру! Гидрант №{foundHydrant.DisplayNumber}, диаметр: {foundHydrant.PipeInfo}");
                        }
                    }
                }

                // === 3. РЕЗУЛЬТАТ ===
                if (foundHydrant != null)
                {
                    MainMap.Position = new PointLatLng(foundHydrant.LatitudeD, foundHydrant.LongitudeD);
                    MainMap.Zoom = 17;

                    var detailsWindow = new HydrantDetailsWindow(foundHydrant, currentUserRole);
                    detailsWindow.Owner = this;
                    detailsWindow.ShowDialog();
                }
                else
                {
                    MessageBox.Show($"Гидрант по запросу \"{rawQuery}\" не найден.\n\n" +
                        "Попробуйте:\n" +
                        "• Ввести номер гидранта (например: 16)\n" +
                        "• Ввести номер и диаметр (например: 16 100)\n" +
                        "• Ввести название улицы (например: Северная)\n" +
                        "• Ввести координаты (например: 54.92, 82.98)",
                        "Не найдено", MessageBoxButton.OK, MessageBoxImage.Information);
                }

            
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            SearchTextBox.Focus();
        }
    }
}