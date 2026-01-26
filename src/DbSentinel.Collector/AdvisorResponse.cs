namespace DbSentinel.Collector;

public class AdvisorResponse
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty; // Chứa nội dung tư vấn (Markdown)
    public List<string> RawQueries { get; set; } = new(); // Danh sách SQL thô để hiển thị riêng
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}