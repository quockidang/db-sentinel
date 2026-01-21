using DbSentinel.Parser;
using System.Linq;
using Xunit;

namespace DbSentinel.Parser.Tests;

public class MySqlSlowLogParserTests
{
    [Fact]
    public void Parse_ShouldCorrectlyParseSingleLogEntry()
    {
        // Arrange
        var logContent = @"
# Time: 240121  15:02:03
# User@Host: root[root] @ localhost []
# Query_time: 5.123456  Lock_time: 0.000000 Rows_sent: 10  Rows_examined: 100000
use my_database;
SET timestamp=1705820523;
SELECT * FROM users WHERE email = 'test@example.com';
";
        var parser = new MySqlSlowLogParser();

        // Act
        var result = parser.Parse(logContent).ToList();

        // Assert
        Assert.Single(result);
        var entry = result[0];

        Assert.Equal(new System.DateTime(2024, 1, 21, 15, 2, 3), entry.Timestamp);
        Assert.Equal("root[root] @ localhost []", entry.UserHost);
        Assert.Equal(5.123456, entry.ExecutionTime);
        Assert.Equal(100000, entry.RowsExamined);
        Assert.Equal("my_database", entry.Database);
        Assert.Equal("SELECT * FROM users WHERE email = 'test@example.com';", entry.SqlStatement);
    }
}