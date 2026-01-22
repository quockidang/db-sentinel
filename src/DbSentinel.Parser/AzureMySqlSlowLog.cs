using System;
using System.Text.Json.Serialization;

namespace DbSentinel.Parser
{
    public class AzureMySqlSlowLog
    {
        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("properties")]
        public AzureMySqlSlowLogProperties? Properties { get; set; }

        [JsonPropertyName("resourceId")]
        public string? ResourceId { get; set; }

        [JsonPropertyName("time")]
        public DateTime Time { get; set; }

        [JsonPropertyName("operationName")]
        public string? OperationName { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("ServerType")]
        public string? ServerType { get; set; }
    }

    public class AzureMySqlSlowLogProperties
    {
        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("event_class")]
        public string? EventClass { get; set; }

        [JsonPropertyName("replication_set_role")]
        public string? ReplicationSetRole { get; set; }

        [JsonPropertyName("start_time")]
        public DateTime StartTime { get; set; }

        [JsonPropertyName("query_time")]
        public double QueryTime { get; set; }

        [JsonPropertyName("lock_time")]
        public double LockTime { get; set; }

        [JsonPropertyName("rows_sent")]
        public long RowsSent { get; set; }

        [JsonPropertyName("rows_examined")]
        public long RowsExamined { get; set; }

        [JsonPropertyName("last_insert_id")]
        public long LastInsertId { get; set; }

        [JsonPropertyName("insert_id")]
        public long InsertId { get; set; }

        [JsonPropertyName("server_id")]
        public long ServerId { get; set; }

        [JsonPropertyName("thread_id")]
        public long ThreadId { get; set; }

        [JsonPropertyName("host")]
        public string? Host { get; set; }

        [JsonPropertyName("db")]
        public string? Db { get; set; }

        [JsonPropertyName("sql_text")]
        public string? SqlText { get; set; }
    }
}
