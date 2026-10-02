using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Windows;
using System.Data;

namespace EWS_NSK_WPF
{
    public class DatabaseHelper
    {
       
        public static DataTable GetTableData(string tableName)
        {
                DataTable dataTable = new DataTable();
                string connectionString = $"Data Source={DbPath}";

            using (var connection = new SqliteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    // Запрос: выбрать всё из переданной таблицы
                    string query = $"SELECT * FROM {tableName};";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            // Загружаем данные из reader в таблицу
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

    // Путь к базе данных (она лежит в той же папке, что и запускаемый файл)
    private static string DbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "INPPV.db");

        public static int GetHydrantsCount()
        {
            int count = 0;

            // Строка подключения к нашей базе
            string connectionString = $"Data Source={DbPath}";

            using (var connection = new SqliteConnection(connectionString))
            {
                try
                {
                    connection.Open(); // Открываем связь с базой

                    // Простой SQL-запрос: посчитать все строки в таблице гидрантов
                    string query = "SELECT COUNT(*) FROM tbl_EWS;";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        // Выполняем запрос и получаем одно число
                        count = Convert.ToInt32(command.ExecuteScalar());
                    }
                }
                catch (Exception ex)
                {
                    // Если что-то пошло не так, мы об этом узнаем
                    Console.WriteLine($"Ошибка базы данных: {ex.Message}");
                }
            }

            return count;
        }
    }
}