using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using EWS_NSK_WPF.Helpers;
using EWS_NSK_WPF.Models;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;

namespace EWS_NSK_WPF
{
    public partial class MapWindow : Window
    {
        private EwsMapDataService? _ewsService;

        public MapWindow(string userName, string position, string accessLevel, WindowMode requestedMode)
        {
            InitializeComponent();

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
                    // Если с MBTiles что-то пошло не так, используем обычный онлайн OSM
                    MainMap.MapProvider = GMapProviders.OpenStreetMap;
                }
            }
            else
            {
                MainMap.MapProvider = GMapProviders.OpenStreetMap;
            }

            MainMap.Manager.Mode = AccessMode.ServerAndCache;
            MainMap.Position = new PointLatLng(55.0084, 82.9357); // Центр Новосибирска
            MainMap.MinZoom = 10;
            MainMap.MaxZoom = 18;
            MainMap.Zoom = 14;
            MainMap.DragButton = System.Windows.Input.MouseButton.Left;
            MainMap.LevelsKeepInMemory = 5;

            // Подписываемся на изменение зума для масштабирования маркеров
            MainMap.OnMapZoomChanged += MainMap_OnMapZoomChanged;

            // Сначала устанавливаем базовый текст пользователя
            UserInfoLabel.Text = $"Пользователь: {userName} ({position})";

            // === 2. ЗАГРУЗКА ГИДРАНТОВ ===
            LoadHydrantsToMap();

            // Применяем тему после загрузки окна
            this.Loaded += (s, e) => ApplyTheme();

            // === 3. ПРАВИЛЬНОЕ ОСВОБОЖДЕНИЕ РЕСУРСОВ (решает проблему зависания при закрытии) ===
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

        /// <summary>
        /// Загружает гидранты из БД и добавляет их на карту
        /// </summary>
        private void LoadHydrantsToMap()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "INPPV.db");

            try
            {
                var hydrants = new List<EWS_NSK_WPF.Models.Ewss>();

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
                        double.TryParse(coordX, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out lat);
                        double.TryParse(coordY, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out lng);

                        if (lat == 0 && lng == 0) continue;

                        hydrants.Add(new EWS_NSK_WPF.Models.Ewss
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

                // Справочники
                var statuses = new Dictionary<int, string>();
                var diameters = new Dictionary<int, string>();
                var owners = new Dictionary<int, string>();

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

                // Сопоставление справочников
                foreach (var h in hydrants)
                {
                    if (h.EwsStatusCod > 0 && statuses.TryGetValue(h.EwsStatusCod, out var s))
                        h.StatusName = s;
                    if (h.EwsDiameterWsCod > 0 && diameters.TryGetValue(h.EwsDiameterWsCod, out var d))
                        h.PipeInfo = d;
                    if (h.EwsOwnerCod > 0 && owners.TryGetValue(h.EwsOwnerCod, out var o))
                        h.OwnerName = o;
                }


                // Создание маркеров
                int currentZoom = (int)MainMap.Zoom;

                // Сортируем: сначала синие (1), потом жёлтые (3), потом красные (2) и остальные
                // Так красные добавляются последними и оказываются СВЕРХУ на карте
                var sortedHydrants = hydrants.OrderBy(h =>
                {
                    if (h.EwsStatusCod == 2) return 3;      // Красные — самые важные, рисуем последними
                    if (h.EwsStatusCod == 3) return 2;      // Жёлтые — средняя важность
                    if (h.EwsStatusCod == 1) return 1;      // Синие — рисуем первыми
                    return 0;                                // Остальные (серые/чёрные) — в самом низу
                }).ToList();

                foreach (var h in sortedHydrants)
                {
                    var marker = HydrantMarker.CreateMarker(h, currentZoom);

                    if (marker.Shape is FrameworkElement element)
                    {
                        element.ToolTip = $"🚒 Гидрант №{h.DisplayNumber}\n" +
                                          $"📊 Статус: {h.StatusName}\n" +
                                          $"📍 Адрес: {h.AddressText}\n" +
                                          $"🔧 Труба: {h.PipeInfo}";
                    }

                    MainMap.Markers.Add(marker);
                }

                UserInfoLabel.Text += $" | ✅ Загружено гидрантов на карту: {hydrants.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}\n\n{ex.StackTrace}", "Ошибка загрузки",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int SafeToInt(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            string str = value.ToString();
            if (string.IsNullOrEmpty(str) || str == "-") return 0;
            return int.TryParse(str, out int result) ? result : 0;
        }
        /// <summary>
        /// Обновляет размер маркеров при изменении зума карты
        /// </summary>
        private void MainMap_OnMapZoomChanged()
        {
            int currentZoom = (int)MainMap.Zoom;

            foreach (var marker in MainMap.Markers)
            {
                HydrantMarker.UpdateMarkerSize(marker, currentZoom);
            }
        }

        /// <summary>
        /// Метод применения темы к этому окну
        /// </summary>
        private void ApplyTheme()
        {
            if (ThemeManager.IsDarkTheme)
            {
                this.Background = new SolidColorBrush(Color.FromRgb(20, 20, 40));
                MapTitleText.Foreground = new SolidColorBrush(Colors.Orange);
                UserInfoLabel.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                this.Background = new SolidColorBrush(Color.FromRgb(245, 245, 250));
                MapTitleText.Foreground = new SolidColorBrush(Colors.Orange);
                UserInfoLabel.Foreground = new SolidColorBrush(Color.FromRgb(0, 40, 100));
            }
        }

        // === ОБРАБОТЧИКИ КНОПОК УПРАВЛЕНИЯ КАРТОЙ ===

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

        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}