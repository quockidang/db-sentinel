using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace DbSentinel.Parser;

public class MySqlSlowLogParser
{
    private static readonly Regex LogEntryRegex = new(
        @"# Time: (?<Time>[^\n]+)\n" +
        @"# User@Host: (?<UserHost>[^\n]+)\n" +
        @"# Query_time: (?<QueryTime>[\d.]+)  Lock_time: (?<LockTime>[\d.]+) Rows_sent: (?<RowsSent>\d+)  Rows_examined: (?<RowsExamined>\d+)\n" +
        @"(?:use (?<Database>[^;]+);\n)?(?:SET timestamp=\d+;\n)?(?<SqlText>(?:(?!(# Time:)).)*)",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public IEnumerable<SlowLogEntry> Parse(string logContent)
    {
        var matches = LogEntryRegex.Matches(logContent);

        foreach (Match match in matches)
        {
            var slowLogEntry = new SlowLogEntry();

            if (match.Groups["Time"].Success &&
                (DateTime.TryParseExact(match.Groups["Time"].Value.Trim(), "yyMMdd  H:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp) ||
                 DateTime.TryParseExact(match.Groups["Time"].Value.Trim(), "yyMMdd H:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out timestamp)))
            {
                slowLogEntry.Timestamp = timestamp;
            }

            if (match.Groups["UserHost"].Success)
            {
                slowLogEntry.UserHost = match.Groups["UserHost"].Value.Trim();
            }

            if (match.Groups["QueryTime"].Success && double.TryParse(match.Groups["QueryTime"].Value, out var executionTime))
            {
                slowLogEntry.ExecutionTime = executionTime;
            }
            
            if (match.Groups["RowsExamined"].Success && long.TryParse(match.Groups["RowsExamined"].Value, out var rowsExamined))
            {
                slowLogEntry.RowsExamined = rowsExamined;
            }

            if (match.Groups["SqlText"].Success)
            {
                slowLogEntry.SqlStatement = match.Groups["SqlText"].Value.Trim();
            }

            if (match.Groups["Database"].Success)
            {
                slowLogEntry.Database = match.Groups["Database"].Value.Trim();
            }

            yield return slowLogEntry;
        }
    }
}
