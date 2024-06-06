using Microsoft.Data.SqlClient;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace EmployeeDataUpload_V3.DatabaseHelper
{
    public class DbHelper
    {
        public void DbConnect()
        {
            string connectionString = ConfigurationManager.AppSettings["connectionString"];

            // SQL query to select data from a table
            string query = "SELECT * FROM dbo.Documents";

            try
            {
                // Create and open a connection to the database
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("Connected to the database");

                    // Create a command object with the SQL query and the connection
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Execute the SQL query and retrieve the data
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // Loop through the result set and print data
                            while (reader.Read())
                            {
                                // Example: Print values from columns "ColumnName1" and "ColumnName2"
                                Console.WriteLine($"Column1: {reader["[FileName]"]}, Column2: {reader["[ModuleCategory]"]}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }
    }
}
