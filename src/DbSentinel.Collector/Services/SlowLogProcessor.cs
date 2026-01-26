

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DbSentinel.Parser;

namespace DbSentinel.Collector.Services
{
    public class SlowLogProcessor
    {
        public List<AggregatedSlowQuery> ProcessLogs(List<SlowLogEntry> rawLogs)
        {
            var result = rawLogs
                .Where(l => !string.IsNullOrEmpty(l.SqlStatement))
                .GroupBy(l => GetFingerprint(NormalizeSql(l.SqlStatement!)))
                .Select(g =>
                {
                    var first = g.First();
                    var template = NormalizeSql(first.SqlStatement!);
                    return new AggregatedSlowQuery
                    {
                        Fingerprint = g.Key,
                        QueryTemplate = template,
                        OccurrenceCount = g.Count(),
                        MaxExecutionTime = g.Max(x => x.ExecutionTime),
                        AvgExecutionTime = g.Average(x => x.ExecutionTime),
                        AvgRowsExamined = (long)g.Average(x => x.RowsExamined),
                        Database = first.Database,
                        LastSeen = g.Max(x => x.Timestamp)
                    };
                })
                // Chỉ giữ lại những query thực sự có vấn đề hoặc tần suất cao
                .Where(q => q.AvgExecutionTime > 1.0 || q.OccurrenceCount > 5)
                .ToList();

            return result;
        }

        private string NormalizeSql(string sql)
        {
            // 1. Lowercase và xóa khoảng trắng thừa
            sql = Regex.Replace(sql.ToLower(), @"\s+", " ").Trim();
            // 2. Chuyển các giá trị cụ thể (Số, Chuỗi trong nháy) thành dấu ?
            return Regex.Replace(sql, @"'\d+'|\d+|'[^']*'|""[^""]*""", "?");
        }

        private string GetFingerprint(string normalizedSql)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(normalizedSql));
            return Convert.ToHexString(hash);
        }
    }



    public class AggregatedSlowQuery
    {
        public string QueryTemplate { get; set; } = string.Empty;
        public string Fingerprint { get; set; } = string.Empty; // MD5 Hash của template
        public int OccurrenceCount { get; set; }
        public double MaxExecutionTime { get; set; }
        public double AvgExecutionTime { get; set; }
        public long AvgRowsExamined { get; set; }
        public string? Database { get; set; }
        public DateTime LastSeen { get; set; }
    }
}