using DbSentinel.Parser;
using System.Text.Json;
using Azure.Storage.Blobs;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IO;
using Azure.Storage.Blobs.Models;
using DbSentinel.Collector.Services;
using Microsoft.SemanticKernel.Memory;

namespace DbSentinel.Collector
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ISlowLogParser _parser;
        private readonly IConfiguration _configuration;

        private SlowLogProcessor _slowLogProcessor;

        private readonly ISemanticTextMemory _memory;

        public Worker(ILogger<Worker> logger, ISlowLogParser parser, IConfiguration configuration, SlowLogProcessor slowLogProcessor, ISemanticTextMemory memory)
        {
            _logger = logger;
            _parser = parser;
            _configuration = configuration;
            _slowLogProcessor = slowLogProcessor;
            _memory = memory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            await DoWork(stoppingToken);

            return;

            // Lấy config, mặc định là 2 giờ sáng nếu không tìm thấy
            var _executionHour = 2;
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextRun = now.Date.AddHours(_executionHour);

                if (now > nextRun) nextRun = nextRun.AddDays(1);

                var delay = nextRun - now;
                await Task.Delay(delay, stoppingToken);

                // Thực thi logic Ingest...
                await DoWork(stoppingToken);
            }


        }

        private async Task DoWork(CancellationToken stoppingToken = default)
        {
            await FetchAndProcessLogsAsync();
        }

        private async Task FetchAndProcessLogsAsync()
        {
            _logger.LogInformation("Fetching logs from Azure Blob Storage...");

            var connectionString = _configuration["AZURE_STORAGE_CONNECTION_STRING"];
            var containerName = _configuration["AZURE_STORAGE_CONTAINER_NAME"];
            var basePrefix = _configuration["AZURE_STORAGE_BASE_PREFIX"];

            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(containerName))
            {
                _logger.LogError("Azure Storage connection string or container name is not configured. " +
                                 "Please set the AZURE_STORAGE_CONNECTION_STRING and AZURE_STORAGE_CONTAINER_NAME environment variables.");
                return;
            }

            try
            {
                var blobServiceClient = new BlobServiceClient(connectionString);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

                var previousDay = DateTime.Now.AddDays(-1);

                // Construct the base prefix for the blobs
                var prefix = $"{basePrefix}/y={previousDay.Year}/m={previousDay.Month:D2}/d={previousDay.Day:D2}";

                _logger.LogInformation("Using blob prefix: {prefix}", prefix);

                var rawLogs = new List<SlowLogEntry>();
                await foreach (var blobItem in blobContainerClient.GetBlobsAsync(BlobTraits.None, BlobStates.All, prefix, default))
                {
                    _logger.LogInformation("Processing blob: {blobName}", blobItem.Name);
                    var blobClient = blobContainerClient.GetBlobClient(blobItem.Name);
                    List<SlowLogEntry>? logs;
                    try
                    {
                        logs = await GetEntriesFromBlobAsync(blobClient, default);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse JSON content from blob: {blobName}", blobItem.Name);
                        continue;
                    }
                    // Assuming each blob contains one JSON object


                    if (logs != null)
                    {
                        rawLogs.AddRange(logs);
                    }
                    else
                    {
                        _logger.LogWarning("Found a null slow log entry.");
                    }
                }

                if (rawLogs.Count > 0)
                {
                    var aggregatedResults = _slowLogProcessor.ProcessLogs(rawLogs);
                    _logger.LogInformation("Aggregated Slow Query Results: {count} entries", aggregatedResults.Count);
                    foreach (var log in aggregatedResults)
                    {
                        // Tạo nội dung giàu ngữ cảnh cho Gemini
                        string contextText = $@"
                            SQL Pattern: {log.QueryTemplate}
                            Stats: Occurred {log.OccurrenceCount} times. 
                            Avg Latency: {log.AvgExecutionTime:F2}s, Max: {log.MaxExecutionTime:F2}s.
                            Avg Rows Examined: {log.AvgRowsExamined}.
                            Last Seen: {log.LastSeen:yyyy-MM-dd HH:mm:ss} in DB: {log.Database}";

                        // metadata để lọc nhanh trong Milvus
                        string metadata = $"db:{log.Database}|count:{log.OccurrenceCount}|latency:{log.MaxExecutionTime}";

                        // SaveInformationAsync sẽ dùng Fingerprint làm ID để tránh duplicate trong Milvus
                        await _memory.SaveInformationAsync(
                            collection: "slow_queries",
                            text: contextText,
                            id: log.Fingerprint,
                            description: "Aggregated Slow Query Pattern",
                            additionalMetadata: metadata
                        );
                    }
                }
                else
                {
                    _logger.LogInformation("No valid slow log entries found for processing.");
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching and processing logs from Azure Blob Storage.");
            }
        }


        private async Task<List<SlowLogEntry>> GetEntriesFromBlobAsync(BlobClient blobClient, CancellationToken ct)
{
    var entries = new List<SlowLogEntry>();

    // Mở stream trực tiếp từ Blob
    using (Stream stream = await blobClient.OpenReadAsync(new BlobOpenReadOptions(allowModifications: false), ct))
    using (StreamReader reader = new StreamReader(stream))
    {
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try 
            {
                // Azure MySQL log format: Mỗi dòng là một đối tượng JSON độc lập
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                var props = root.GetProperty("properties");

                entries.Add(new SlowLogEntry
                {
                    Timestamp = root.GetProperty("time").GetDateTime(),
                    SqlStatement = props.GetProperty("sql_text").GetString(),
                    ExecutionTime = props.GetProperty("query_time").GetDouble(),
                    RowsExamined = props.GetProperty("rows_examined").GetInt64(),
                    Database = props.GetProperty("db").GetString(),
                    UserHost = props.GetProperty("host").GetString()
                });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("Dòng log không hợp lệ trong file {BlobName}: {Error}", blobClient.Name, ex.Message);
            }
        }
    }
    return entries;
}

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
