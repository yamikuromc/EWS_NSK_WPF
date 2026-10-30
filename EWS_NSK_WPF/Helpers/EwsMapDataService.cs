using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using EWS_NSK_WPF.Models;

namespace EWS_NSK_WPF.Helpers
{
    public class EwsMapDataService
    {
        private readonly string _dbPath;

        public EwsMapDataService(string dbPath)
        {
            _dbPath = dbPath;
        }

        public List<Ewss> GetAllEwss()
        {
            var hydrants = new List<Ewss>();
            if (!File.Exists(_dbPath)) return hydrants;

            try
            {
                // Mode=ReadOnly + Cache=Shared разрешают множественное чтение файла
                string connectionString = $"Data Source={_dbPath};Mode=ReadOnly;Pooling=False;Cache=Shared";
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                string query = @"
                    SELECT 
                        EWS_Unit_COD, EWS_Map_ID, EWS_Type_COD, EWS_Number, EWS_Name,
                        EWS_Status_COD, EWS_TroubleEWS_COD, EWS_PipeType_COD,
                        EWS_DiameterWS_COD, EWS_DiameterPK_COD, EWS_CountPA,
                        EWS_Value_COD, EWS_Owner_COD,
                        EWS_GeoCoord_X, EWS_GeoCoord_Y,
                        EWS_State, EWS_StateNote, EWS_AddressObject_COD, EWS_HouseNumber,
                        EWS_PriViazka, EWS_PrLeft, EWS_PrRight, EWS_PrStright,
                        EWS_PriViazka_GeoX, EWS_PriViazka_GeoY,
                        Record_Created, Record_User_COD, Record_Status, EWS_Journal,
                        EWS_Display, EWS_BindDisplay
                    FROM tbl_EWS";

                using var cmd = new SqliteCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                int markerId = 1;
                while (reader.Read())
                {
                    double lat = 0, lng = 0;
                    double.TryParse(reader["EWS_GeoCoord_X"]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out lat);
                    double.TryParse(reader["EWS_GeoCoord_Y"]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out lng);

                    if (lat == 0 && lng == 0) continue;

                    var ewss = new Ewss
                    {
                        EwsUnitCod = Convert.ToInt32(reader["EWS_Unit_COD"] ?? 0),
                        EwsMapId = Convert.ToInt32(reader["EWS_Map_ID"] ?? 0),
                        EwsTypeCod = Convert.ToInt32(reader["EWS_Type_COD"] ?? 0),
                        EwsNumber = Convert.ToInt32(reader["EWS_Number"] ?? 0),
                        EwsName = reader["EWS_Name"]?.ToString() ?? "",
                        EwsStatusCod = Convert.ToInt32(reader["EWS_Status_COD"] ?? 0),
                        EwsTroubleEwsCod = reader["EWS_TroubleEWS_COD"]?.ToString() ?? "",
                        EwsPipeTypeCod = Convert.ToInt32(reader["EWS_PipeType_COD"] ?? 0),
                        EwsDiameterWsCod = Convert.ToInt32(reader["EWS_DiameterWS_COD"] ?? 0),
                        EwsDiameterPkCod = Convert.ToInt32(reader["EWS_DiameterPK_COD"] ?? 0),
                        EwsCountPa = Convert.ToInt32(reader["EWS_CountPA"] ?? 0),
                        EwsValueCod = Convert.ToInt32(reader["EWS_Value_COD"] ?? 0),
                        EwsOwnerCod = Convert.ToInt32(reader["EWS_Owner_COD"] ?? 0),
                        LatitudeD = lat,
                        LongitudeD = lng,
                        EwsState = reader["EWS_State"]?.ToString() ?? "",
                        EwsStateNote = reader["EWS_StateNote"]?.ToString() ?? "",
                        EwsAddressObjectCod = reader["EWS_AddressObject_COD"]?.ToString() ?? "",
                        EwsHouseNumber = reader["EWS_HouseNumber"]?.ToString() ?? "",
                        EwsPriviazka = reader["EWS_PriViazka"]?.ToString() ?? "",
                        EwsPrLeft = Convert.ToInt32(reader["EWS_PrLeft"] ?? 0),
                        EwsPrRight = Convert.ToInt32(reader["EWS_PrRight"] ?? 0),
                        EwsPrStright = Convert.ToInt32(reader["EWS_PrStright"] ?? 0),
                        EwsPriviazkaGeoX = reader["EWS_PriViazka_GeoX"]?.ToString() ?? "",
                        EwsPriviazkaGeoY = reader["EWS_PriViazka_GeoY"]?.ToString() ?? "",
                        RecordCreated = reader["Record_Created"]?.ToString() ?? "",
                        RecordUserCod = Convert.ToInt32(reader["Record_User_COD"] ?? 0),
                        RecordStatus = reader["Record_Status"]?.ToString() ?? "",
                        EwsJournal = reader["EWS_Journal"]?.ToString() ?? "",
                        EwsDisplay = Convert.ToInt32(reader["EWS_Display"] ?? 0),
                        EwsBindDisplay = Convert.ToInt32(reader["EWS_BindDisplay"] ?? 0),
                        MarkerId = markerId++
                    };
                    hydrants.Add(ewss);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки гидрантов: {ex.Message}");
            }
            return hydrants;
        }

        public Dictionary<int, string> GetStatuses()
        {
            var statuses = new Dictionary<int, string>();
            try
            {
                string connectionString = $"Data Source={_dbPath};Mode=ReadOnly;Pooling=False;Cache=Shared";
                using var connection = new SqliteConnection(connectionString);
                connection.Open();
                using var cmd = new SqliteCommand("SELECT * FROM tbl_CheckEWS_StatusEWS", connection);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int id = Convert.ToInt32(reader[0] ?? 0);
                    string name = reader[1]?.ToString() ?? "";
                    if (id > 0 && !string.IsNullOrEmpty(name)) statuses[id] = name;
                }
            }
            catch { }
            return statuses;
        }

        public Dictionary<int, string> GetDiameters()
        {
            var diameters = new Dictionary<int, string>();
            try
            {
                string connectionString = $"Data Source={_dbPath};Mode=ReadOnly;Pooling=False;Cache=Shared";
                using var connection = new SqliteConnection(connectionString);
                connection.Open();
                using var cmd = new SqliteCommand("SELECT * FROM tbl_EWS_DiameterWS", connection);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int id = Convert.ToInt32(reader[0] ?? 0);
                    string diameter = reader[1]?.ToString() ?? "";
                    if (id > 0 && !string.IsNullOrEmpty(diameter)) diameters[id] = diameter;
                }
            }
            catch { }
            return diameters;
        }

        public Dictionary<int, string> GetOwners()
        {
            var owners = new Dictionary<int, string>();
            try
            {
                string connectionString = $"Data Source={_dbPath};Mode=ReadOnly;Pooling=False;Cache=Shared";
                using var connection = new SqliteConnection(connectionString);
                connection.Open();
                using var cmd = new SqliteCommand("SELECT * FROM tbl_EWS_Owner", connection);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int id = Convert.ToInt32(reader[0] ?? 0);
                    string name = reader[1]?.ToString() ?? "";
                    if (id > 0 && !string.IsNullOrEmpty(name)) owners[id] = name;
                }
            }
            catch { }
            return owners;
        }

        public List<Ewss> GetAllEwssWithDisplay()
        {
            var hydrants = GetAllEwss();
            var statuses = GetStatuses();
            var diameters = GetDiameters();
            var owners = GetOwners();

            foreach (var h in hydrants)
            {
                if (h.EwsStatusCod > 0 && statuses.TryGetValue(h.EwsStatusCod, out var statusName))
                    h.StatusName = statusName;
                if (h.EwsDiameterWsCod > 0 && diameters.TryGetValue(h.EwsDiameterWsCod, out var diameter))
                    h.PipeInfo = diameter;
                if (h.EwsOwnerCod > 0 && owners.TryGetValue(h.EwsOwnerCod, out var ownerName))
                    h.OwnerName = ownerName;
            }
            return hydrants;
        }
    }
}