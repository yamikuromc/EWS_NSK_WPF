using System;
using System.Data;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Windows;
using Microsoft.Data.Sqlite;

namespace EWS_NSK_WPF
{
    public class DatabaseHelper
    {
        // Путь к базе данных (ищем в папке Resources)
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

        // === НОВЫЙ МЕТОД: Хеширование пароля (SHA256) ===
        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // === ОБНОВЛЁННЫЙ МЕТОД: Проверка логина и пароля (возвращает 5 значений) ===
        public static (bool Success, string FullName, string Position, int Role, int StaffId) AuthenticateUser(string login, string passwordHash)
        {
            string connectionString = $"Data Source={DbPath}";

            using (var connection = new SqliteConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string query = @"
                        SELECT u.Staff_ID, s.Staff_FIO, s.Staff_post, s.Staff_Role 
                        FROM tbl_Auth_Users u
                        JOIN tbl_Fire4_UnitStaff s ON u.Staff_ID = s.Staff_ID
                        WHERE u.Login = @Login AND u.PasswordHash = @PasswordHash AND u.IsActive = 1";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Login", login);
                        command.Parameters.AddWithValue("@PasswordHash", passwordHash);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int staffId = Convert.ToInt32(reader["Staff_ID"]);
                                string fullName = reader["Staff_FIO"].ToString() ?? "Неизвестно";
                                string position = reader["Staff_post"].ToString() ?? "Сотрудник";

                                int role = 0;
                                if (reader["Staff_Role"] != DBNull.Value)
                                {
                                    role = Convert.ToInt32(reader["Staff_Role"]);
                                }

                                // Возвращаем 5 значений: успех, ФИО, должность, роль, ID сотрудника
                                return (true, fullName, position, role, staffId);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка авторизации: {ex.Message}");
                }
            }

            // Если пользователь не найден или произошла ошибка
            return (false, "", "", 0, 0);
        }

        // === МЕТОД: Запись действий пользователя в лог ===
        public static void LogUserAction(int staffCod, string tableName, string recordId,
                                          string fieldName, string oldValue, string newValue,
                                          string actionType)
        {
            try
            {
                string connectionString = $"Data Source={DbPath}";

                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();

                    string query = @"INSERT INTO tbl_Set_UserLog 
                            (Staff_COD, StaffUserData, TableName, RecordID, FieldName, 
                             OldValue, NewValue, ActionType) 
                            VALUES (@StaffCOD, @StaffUserData, @TableName, @RecordID, 
                                    @FieldName, @OldValue, @NewValue, @ActionType)";

                    using (var command = new SqliteCommand(query, connection))
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