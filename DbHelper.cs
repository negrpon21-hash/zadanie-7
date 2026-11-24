using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Configuration;

namespace TourAgencyApp
{
    internal static class DbHelper
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["TourDB"].ConnectionString;

        public static MySqlConnection GetConnection()
        {
            var conn = new MySqlConnection(ConnectionString);
            conn.Open();
            return conn;
        }

        public static List<Tour> LoadTours()
        {
            var tours = new List<Tour>();
            using var conn = GetConnection();

            var cmd = new MySqlCommand(
                @"SELECT t.Id, t.Name, t.TicketCount, t.Price, t.IsActual,
                         t.Country_Code, t.Description, t.ImagePreview
                  FROM Tour t
                  ORDER BY t.Id", conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var tour = new Tour
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    TicketCount = reader.GetInt32("TicketCount"),
                    Price = reader.GetDecimal("Price"),
                    IsActual = reader.GetBoolean("IsActual"),
                    CountryCode = reader.IsDBNull(reader.GetOrdinal("Country_Code")) ? "" : reader.GetString("Country_Code"),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString("Description"),
                    ImagePreview = reader.IsDBNull(reader.GetOrdinal("ImagePreview")) ? null : (byte[])reader["ImagePreview"]
                };
                tours.Add(tour);
            }

            return tours;
        }

        public static void LoadTypes(List<Tour> tours)
        {
            if (tours.Count == 0) return;

            using var conn = GetConnection();
            var cmd = new MySqlCommand(
                @"SELECT tt.TourId, tp.Name
                  FROM TypeOfTour tt
                  JOIN Type tp ON tp.Id = tt.TypeId", conn);

            var typeMap = new Dictionary<int, List<string>>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int tourId = reader.GetInt32("TourId");
                string typeName = reader.GetString("Name");
                if (!typeMap.ContainsKey(tourId))
                    typeMap[tourId] = new List<string>();
                typeMap[tourId].Add(typeName);
            }

            foreach (var tour in tours)
            {
                if (typeMap.TryGetValue(tour.Id, out var types))
                    tour.Types = types;
            }
        }

        public static List<string> LoadTypeNames()
        {
            var types = new List<string>();
            using var conn = GetConnection();
            var cmd = new MySqlCommand("SELECT Name FROM Type ORDER BY Name", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                types.Add(reader.GetString("Name"));
            return types;
        }
    }
}
