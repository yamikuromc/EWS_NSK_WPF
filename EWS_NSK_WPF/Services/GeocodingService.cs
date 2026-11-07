using EWS_NSK_WPF.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;


namespace EWS_NSK_WPF.Services
{
    public static class GeocodingService
    {
        // Клиент для запросов в интернет
        private static readonly HttpClient _httpClient = new HttpClient();

        // Путь к нашей базе данных
        private static string DbPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "INPPV.db");

        static GeocodingService()
        {
            // ВАЖНО: Nominatim требует, чтобы мы представились. Иначе вернет ошибку 403.
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("EWS_NSK_WPF/1.0 (Inessa's Coursework)");
        }

        /// <summary>
        /// Главная функция: получает координаты по адресу.
        /// Сначала ищет в кэше, если нет — спрашивает у Nominatim и сохраняет.
        /// </summary>
        public static async Task<(double Lat, double Lng)?> GetCoordinatesAsync(string address, FireStation station = null)
        {
            if (string.IsNullOrWhiteSpace(address)) return null;

            // Нормализуем адрес для кэша (убираем пробелы по краям, нижний регистр), 
            // чтобы "Ленина 1" и "ленина 1 " считались одним адресом
            string normalizedAddress = address.Trim().ToLower();

            // 1. Проверяем локальный кэш
            var cached = GetFromCache(normalizedAddress);
            if (cached.HasValue)
            {
                System.Diagnostics.Debug.WriteLine($"✅ Найдено в кэше: {address}");
                return cached;
            }

            // 2. Если в кэше нет — идем в интернет
            try
            {
                System.Diagnostics.Debug.WriteLine($"🌐 Запрос к Nominatim: {address}");

                // ОГРАНИЧИВАЕМ ПОИСК СТРОГО РАМКАМИ (Краснообск + ближайшие микрорайоны)
                // viewbox=minLon,minLat,maxLon,maxLat
                // bounded=1 заставляет искать ТОЛЬКО внутри этой рамки
                string url = $"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(address)}" +
                             $"&viewbox=82.95,54.90,83.05,54.96&bounded=1&limit=1&countrycodes=ru";

                var response = await _httpClient.GetStringAsync(url);
                var results = JsonSerializer.Deserialize<NominatimResult[]>(response);

                if (results != null && results.Length > 0)
                {
                    // Парсим координаты
                    double lat = double.Parse(results[0].lat, CultureInfo.InvariantCulture);
                    double lng = double.Parse(results[0].lon, CultureInfo.InvariantCulture);

                    // Показываем, ЧТО ИМЕННО нашёл Nominatim
                    System.Diagnostics.Debug.WriteLine($" Nominatim нашёл: {results[0].display_name}");
                    System.Diagnostics.Debug.WriteLine($"✅ Координаты: {lat}, {lng}");

                    if (station != null)
                    {
                        double distToStation = Math.Sqrt(Math.Pow(lat - station.CenterLat, 2) + Math.Pow(lng - station.CenterLng, 2)) * 111000;
                        if (distToStation > station.RadiusMeters)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠ Адрес найден, но находится слишком далеко от ПСЧ ({distToStation / 1000:F1} км > {station.RadiusMeters / 1000:F0} км)");
                            return null;
                        }
                        System.Diagnostics.Debug.WriteLine($"✅ Адрес в пределах района выезда ({distToStation / 1000:F2} км от ПСЧ)");
                    }

                    // 3. Сохраняем в кэш
                    SaveToCache(normalizedAddress, lat, lng);

                    return (lat, lng);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠ Nominatim ничего не нашел для: {address}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка геокодинга: {ex.Message}");
            }

            return null;
        }

        // --- Вспомогательные методы для работы с БД ---

        private static (double Lat, double Lng)? GetFromCache(string address)
        {
            try
            {
                using var conn = new SqliteConnection($"Data Source={DbPath}");
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Latitude, Longitude FROM tbl_GeocodeCache WHERE Address = @addr";
                cmd.Parameters.AddWithValue("@addr", address);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    double lat = reader.GetDouble(0);
                    double lng = reader.GetDouble(1);
                    return (lat, lng);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка чтения кэша: {ex.Message}");
            }
            return null;
        }

        private static void SaveToCache(string address, double lat, double lng)
        {
            try
            {
                using var conn = new SqliteConnection($"Data Source={DbPath}");
                conn.Open();
                var cmd = conn.CreateCommand();
                // INSERT OR REPLACE — если адрес уже есть (на всякий случай), обновим его
                cmd.CommandText = "INSERT OR REPLACE INTO tbl_GeocodeCache (Address, Latitude, Longitude) VALUES (@addr, @lat, @lng)";
                cmd.Parameters.AddWithValue("@addr", address);
                cmd.Parameters.AddWithValue("@lat", lat);
                cmd.Parameters.AddWithValue("@lng", lng);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения в кэш: {ex.Message}");
            }
        }
    }

    // Класс для разбора JSON ответа от Nominatim
    public class NominatimResult
    {
        public string lat { get; set; }
        public string lon { get; set; }
        public string display_name { get; set; }
    }
}