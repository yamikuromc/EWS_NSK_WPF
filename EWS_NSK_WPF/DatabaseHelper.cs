using System;
using System.Data;
using System.IO;
using System.Windows;
using Microsoft.Data.Sqlite; // Используем только одну библиотеку SQLite

namespace EWS_NSK_WPF
{
    public class DatabaseHelper
    {
        // Путь к базе данных (она лежит в той же папке, что и запускаемый файл)
        private static string DbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "INPPV.db");

        public static DataTable GetTableData(string tableName)
        {
            DataTable dataTable = new DataTable();
            string connectionString = $"Data Source={DbPath}";

            using (var connection = new SqliteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = $"SELECT * FROM {tableName};";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            dataTable.Load(reader);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при загрузке таблицы {tableName}: {ex.Message}");
                }
            }
            return dataTable;
        }

        public static int GetHydrantsCount()
        {
            int count = 0;
            string connectionString = $"Data Source={DbPath}";

            using (var connection = new SqliteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM tbl_EWS;";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        count = Convert.ToInt32(command.ExecuteScalar());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка базы данных: {ex.Message}");
                }
            }

            return count;
        }

        // Метод для записи действий пользователя в лог (tbl_Set_UserLog)
        public static void LogUserAction(int staffCod, string tableName, string recordId,
                                          string fieldName, string oldValue, string newValue,
                                          string actionType)
        {
            try
            {
                string connectionString = $"Data Source={DbPath}"; // Исправлено на DbPath

                using (var connection = new SqliteConnection(connectionString)) // Исправлено на SqliteConnection
                {
                    connection.Open();

                    string query = @"INSERT INTO tbl_Set_UserLog 
                            (Staff_COD, StaffUserData, TableName, RecordID, FieldName, 
                             OldValue, NewValue, ActionType) 
                            VALUES (@StaffCOD, @StaffUserData, @TableName, @RecordID, 
                                    @FieldName, @OldValue, @NewValue, @ActionType)";

                    using (var command = new SqliteCommand(query, connection)) // Исправлено на SqliteCommand
                    {
                        command.Parameters.AddWithValue("@StaffCOD", staffCod);
                        command.Parameters.AddWithValue("@StaffUserData", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                        command.Parameters.AddWithValue("@TableName", tableName);
                        command.Parameters.AddWithValue("@RecordID", recordId);
                        command.Parameters.AddWithValue("@FieldName", fieldName);
                        command.Parameters.AddWithValue("@OldValue", oldValue ?? "");
                        command.Parameters.AddWithValue("@NewValue", newValue ?? "");
                        command.Parameters.AddWithValue("@ActionType", actionType);

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка записи в лог: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}