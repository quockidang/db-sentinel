using System.Collections.Generic;
using DbSentinel.Parser;

namespace DbSentinel.Brain.Models
{
    public class AnalysisResult
    {
        public SlowLogEntry? OriginalEntry { get; set; }
        public string? RootCauseAnalysis { get; set; }
        public List<Suggestion>? Suggestions { get; set; }
    }
}