using System.Collections.Generic;

namespace DbSentinel.Parser
{
    public interface ISlowLogParser
    {
        SlowLogEntry? Parse(string logContent);
    }
}
