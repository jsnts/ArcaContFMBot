using System;
using System.Data;
using System.Data.SqlClient;

namespace Bot.Api.Data
{
    public class DatabaseService
    {
        private readonly string connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseService"/> class.
        /// </summary>
        /// <param name="serverName">The name of the server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="username">The username to use for authentication.</param>
        /// <param name="password">The password to use for authentication.</param>
        public DatabaseService(string serverName, string databaseName, string username, string password)
        {
            // Create the connection string using the provided parameters.
            connectionString = $"Server=tcp:{serverName},1433;Initial Catalog={databaseName};Persist Security Info=False;User ID={username};Password={password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        }

        /// <summary>
        /// Executes the specified SQL query.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        public void ExecuteQuery(string query)
        {
            // Create a new SqlConnection object using the connection string.
            using (var connection = new SqlConnection(connectionString))
            {
                // Open the connection.
                connection.Open();

                // Create a new SqlCommand object using the query and the connection.
                using (var command = new SqlCommand(query, connection))
                {
                    // Execute the query.
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Executes the specified SQL query and returns a SqlDataReader object.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <returns>A SqlDataReader object that contains the results of the query.</returns>
        public SqlDataReader ExecuteReader(string query)
        {
            // Create a new SqlConnection object using the connection string.
            var connection = new SqlConnection(connectionString);

            // Open the connection.
            connection.Open();

            // Create a new SqlCommand object using the query and the connection.
            var command = new SqlCommand(query, connection);

            // Execute the query and return a SqlDataReader object.
            var reader = command.ExecuteReader(CommandBehavior.CloseConnection);

            return reader;
        }

    }
}
