namespace DbSentinel.Parser;

public class SlowLogEntry
{
    public DateTime Timestamp { get; set; }
    public string? UserHost { get; set; }
    public double ExecutionTime { get; set; }
    public long RowsExamined { get; set; }
    public string? SqlStatement { get; set; }
    public string? Database { get; set; }
}
