using System.Configuration;
using System.Data.SqlClient;

namespace ShoeStoreApp
{
    public static class DatabaseHelper
    {
        private static string connString =
            ConfigurationManager.ConnectionStrings["ShoeStoreDB"].ConnectionString;

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(connString);
        }
    }
}