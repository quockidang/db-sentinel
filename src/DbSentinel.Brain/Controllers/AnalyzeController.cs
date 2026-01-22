using DbSentinel.Brain.Models;
using DbSentinel.Brain.Tools;
using DbSentinel.Parser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using System.Text.Json;
using System.Threading.Tasks;

namespace DbSentinel.Brain.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyzeController : ControllerBase
    {
        private readonly ILogger<AnalyzeController> _logger;
        private readonly Kernel _kernel;
        private readonly ExplainTool _explainTool;

        public AnalyzeController(ILogger<AnalyzeController> logger, Kernel kernel, ExplainTool explainTool)
        {
            _logger = logger;
            _kernel = kernel;
            _explainTool = explainTool;
            
            // Add the tool to the kernel's plugins
            _kernel.Plugins.AddFromObject(_explainTool, "Database");
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SlowLogEntry logEntry)
        {
            if (logEntry == null)
            {
                return BadRequest("Slow log entry cannot be null.");
            }

            _logger.LogInformation("Received slow log entry for analysis: {SqlStatement}", logEntry.SqlStatement);

            // Define the prompt that uses the tool
            var prompt = @"
You are a senior database performance analyst. Your goal is to analyze a slow query and provide a root cause analysis and actionable suggestions.

**Step 1: Initial Analysis**
Analyze the user's input, which includes the SQL query, execution time, and rows examined.
Form a hypothesis about why the query is slow.

**Step 2: Tool Use**
Based on your hypothesis, use the `Database.GetExplainPlan` tool to get the execution plan for the query.
This is a mandatory step.

**Step 3: Final Report**
Analyze the execution plan along with the original query data.
Provide a final report in JSON format. The JSON should have two keys: 'rootCauseAnalysis' (a string) and 'suggestions' (an array of objects, where each object has 'type', 'summary', 'action', and 'justification' keys).

---
**User Input:**
- **SQL Query:** {{$input}}
- **Execution Time:** {{$executionTime}} seconds
- **Rows Examined:** {{$rowsExamined}}
- **Database:** {{$database}}
---
";

            var arguments = new KernelArguments
            {
                { "input", logEntry.SqlStatement },
                { "executionTime", logEntry.ExecutionTime },
                { "rowsExamined", logEntry.RowsExamined },
                { "database", logEntry.Database }
            };

            // Invoke the prompt and let the kernel orchestrate the tool use
            var result = await _kernel.InvokePromptAsync(prompt, arguments);
            var resultJson = result.GetValue<string>();

            _logger.LogInformation("AI analysis result (JSON): {resultJson}", resultJson);

            try
            {
                // Deserialize the AI's JSON response into our model
                //var analysisData = JsonSerializer.Deserialize<AnalysisResult>(resultJson ?? "{}", new JsonSerializerOptions
                //{
                //    PropertyNameCaseInsensitive = true
                //});

                //if (analysisData == null)
                //{
                //    return StatusCode(500, "Failed to deserialize AI analysis result.");
                //}

                //analysisData.OriginalEntry = logEntry; // Add original entry to the final result
                return Ok(resultJson);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize AI's JSON output. Raw output: {resultJson}", resultJson);
                return StatusCode(500, $"Failed to parse AI response: {ex.Message}");
            }
        }
    }
}
