using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using MySql.Data.MySqlClient;

namespace DbSentinel.Brain.Tools
{
    public class ExplainTool
    {
        private readonly IConfiguration _configuration;

        public ExplainTool(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [KernelFunction, Description("Gets the EXPLAIN plan for a given SQL query from the database.")]
        public async Task<string> GetExplainPlanAsync(
            [Description("The SQL query to explain.")] string sqlQuery,
            [Description("The database to run the query against.")] string database)
        {
            var connectionString = _configuration.GetConnectionString("Database");
            if (string.IsNullOrEmpty(connectionString))
            {
                return "Error: Database connection string is not configured.";
            }

            var fullConnectionString = $"{connectionString}Database={database};";

            var explainOutput = new StringBuilder();
            try
            {
                await using var connection = new MySqlConnection(fullConnectionString);
                await connection.OpenAsync();

                var explainQuery = $"EXPLAIN {sqlQuery}";
                await using var command = new MySqlCommand(explainQuery, connection);
                await using var reader = await command.ExecuteReaderAsync();

                // Append headers
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    explainOutput.Append(reader.GetName(i)).Append("\t");
                }
                explainOutput.AppendLine();

                // Append rows
                while (await reader.ReadAsync())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        explainOutput.Append(reader[i]).Append("\t");
                    }
                    explainOutput.AppendLine();
                }
            }
            catch (Exception ex)
            {
                return $"Error executing EXPLAIN command: {ex.Message}";
            }

            return explainOutput.ToString();
        }
    }
}
