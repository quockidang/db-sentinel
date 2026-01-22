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

namespace DbSentinel.Collector
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ISlowLogParser _parser;
        private readonly IConfiguration _configuration;
        private Timer? _timer;

        public Worker(ILogger<Worker> logger, ISlowLogParser parser, IConfiguration configuration)
        {
            _logger = logger;
            _parser = parser;
            _configuration = configuration;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(() => _logger.LogInformation("Worker service is stopping."));

            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromDays(1));

            return Task.CompletedTask;
        }

        private void DoWork(object? state)
        {
            var now = DateTime.Now;
            var nextRunTime = now.Date.AddHours(8); // Schedule for 8 AM
            if (now > nextRunTime)
            {
                nextRunTime = nextRunTime.AddDays(1); // If 8 AM has passed, schedule for next day
            }

            var delay = nextRunTime - now;
            _logger.LogInformation("Next run scheduled for: {runTime}", nextRunTime);

            _timer?.Change(delay, TimeSpan.FromDays(1));

            _logger.LogInformation("Worker running at: {time}", DateTime.Now);
            FetchAndProcessLogsAsync().GetAwaiter().GetResult();
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
                await foreach (var blobItem in blobContainerClient.GetBlobsAsync(BlobTraits.None, BlobStates.All, prefix, default))
                {
                    _logger.LogInformation("Processing blob: {blobName}", blobItem.Name);
                    var blobClient = blobContainerClient.GetBlobClient(blobItem.Name);
                    var response = await blobClient.DownloadAsync();

                    using var reader = new StreamReader(response.Value.Content);
                    var blobContent = await reader.ReadToEndAsync();
                    SlowLogEntry? slowLogEntry = null;
                    try
                    {
                        slowLogEntry = _parser.Parse(blobContent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse JSON content from blob: {blobName}", blobItem.Name);
                        continue;
                    }
                    // Assuming each blob contains one JSON object


                    if (slowLogEntry != null)
                    {
                        _logger.LogInformation("Parsed Slow Log Entry:\n" +
                                               "Timestamp: {Timestamp}\n" +
                                               "UserHost: {UserHost}\n" +
                                               "ExecutionTime: {ExecutionTime}\n" +
                                               "RowsExamined: {RowsExamined}\n" +
                                               "Database: {Database}\n" +
                                               "SqlStatement: {SqlStatement}",
                                               slowLogEntry.Timestamp,
                                               slowLogEntry.UserHost,
                                               slowLogEntry.ExecutionTime,
                                               slowLogEntry.RowsExamined,
                                               slowLogEntry.Database,
                                               slowLogEntry.SqlStatement);
                        // Placeholder for sending to AI
                        _logger.LogInformation("Handing off to AI for processing...");
                    }
                    else
                    {
                        _logger.LogWarning("Found a null slow log entry.");
                    }
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching and processing logs from Azure Blob Storage.");
            }
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
}
