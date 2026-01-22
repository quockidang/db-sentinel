using System.Collections.Generic;
using System.Text.Json;

namespace DbSentinel.Parser
{
    public class JsonSlowLogParser : ISlowLogParser
    {
        public SlowLogEntry? Parse(string logContent)
        {
            try
            {
                var azureSlowLog = JsonSerializer.Deserialize<AzureMySqlSlowLog>(logContent);
                return new SlowLogEntry
                {
                    Timestamp = azureSlowLog.Time,
                    UserHost = azureSlowLog.Properties.Host,
                    ExecutionTime = azureSlowLog.Properties.QueryTime,
                    RowsExamined = azureSlowLog.Properties.RowsExamined,
                    SqlStatement = azureSlowLog.Properties.SqlText,
                    Database = azureSlowLog.Properties.Db
                };
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Error parsing JSON slow log: {ex.Message}");
            }
            return null;
        }
    }
}
