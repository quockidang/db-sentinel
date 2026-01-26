using System.Text;
using System.Text.RegularExpressions;
using DbSentinel.Collector;
using DbSentinel.Collector.Services;
using DbSentinel.Parser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.Milvus;
using Microsoft.SemanticKernel.Memory;
using Milvus.Client; // Cần cài thêm package Milvus.Client

var builder = WebApplication.CreateBuilder(args);


// --- 1. Đăng ký Milvus Store ---
builder.Services.AddSingleton<IMemoryStore>(sp =>
{
    // 1. Khởi tạo Advanced Milvus Client
    // Cho phép cấu hình Timeout, SSL, hoặc Credentials nếu cần
    var milvusClient = new MilvusClient(
        builder.Configuration["Milvus:Host"] ?? "localhost",
        port: 19530
    );

    // 2. Cấu hình các tham số chuyên sâu cho RAG
    return new MilvusMemoryStore(
        milvusClient,
        vectorSize: 768, // Kích thước của Gemini text-embedding-004
        metricType: SimilarityMetricType.Cosine, // Tối ưu cho Gemini
        consistencyLevel: ConsistencyLevel.Session // Cân bằng giữa Performance và Consistency
    );
});

builder.Services.AddSingleton<ISemanticTextMemory>(sp =>
{
    var memoryStore = sp.GetRequiredService<IMemoryStore>();

    // Khởi tạo service tạo embedding của Gemini riêng lẻ
    var embeddingService = new GoogleAIEmbeddingGenerator(
        modelId: "text-embedding-004",
        apiKey: builder.Configuration["Gemini:ApiKey"]!
    );

    // Xây dựng Memory bằng cách truyền service vào
    return new SemanticTextMemory(memoryStore, embeddingService);
});


// --- 1. Đăng ký Dịch vụ CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("DemoPolicy", policy =>
    {
        policy.WithOrigins("http://127.0.0.1:5001", "http://localhost:5001", "null") // null dành cho mở file trực tiếp
              .AllowAnyMethod()   // Cho phép GET, POST, v.v.
              .AllowAnyHeader();  // Cho phép các header như Content-Type
    });
});



builder.Services.AddSingleton<ISlowLogParser, JsonSlowLogParser>();
builder.Services.AddSingleton<SlowLogProcessor>();
//builder.Services.AddHostedService<Worker>();

builder.Services.AddKernel()
    .AddGoogleAIGeminiChatCompletion("gemini-2.0-flash", builder.Configuration["Gemini:ApiKey"]!);

// The IConfiguration and ILogger are automatically registered by CreateApplicationBuilder.
// The DI container will automatically resolve the ILogger<JsonSlowLogParser> dependency.

var app = builder.Build();

// THỨ TỰ CỰC KỲ QUAN TRỌNG:
app.UseRouting();
app.UseCors("DemoPolicy");
app.UseHttpsRedirection(); // Đảm bảo chuyển hướng HTTP sang HTTPS
// Thêm Endpoint mẫu để test Agent
app.MapGet("/ask-advisor", async (string q, Kernel kernel, ISemanticTextMemory memory) =>
{
    var results = memory.SearchAsync("slow_queries", q, limit: 3, minRelevanceScore: 0.3);

    var contextBuilder = new StringBuilder();
    var rawQueries = new List<string>();

    await foreach (var res in results)
    {
        contextBuilder.AppendLine(res.Metadata.Text);
        // Trích xuất SQL thô từ text để hiển thị list riêng
        var sqlMatch = Regex.Match(res.Metadata.Text, @"Pattern:\s*(.*?)($|\|)", RegexOptions.Singleline);
        if (sqlMatch.Success) rawQueries.Add(sqlMatch.Groups[1].Value.Trim());
    }

    if (rawQueries.Count == 0) return Results.NotFound(new { message = "Không tìm thấy dữ liệu." });

    var prompt = $@"
        Bạn là một chuyên gia tối ưu hóa MySQL. Hãy phân tích dựa trên dữ liệu sau:
        {contextBuilder}

        YÊU CẦU:
        1. Trả lời bằng tiếng Việt, giọng văn chuyên nghiệp.
        2. Sử dụng Markdown: **Bold** cho từ khóa, `code` cho tên bảng/cột.
        3. Cấu trúc câu trả lời:
           - ## 🔍 Phân tích nguyên nhân
           - ## 💡 Giải pháp đề xuất
           - ## 🛠 Lệnh tối ưu (nếu có)";

    var result = await kernel.InvokePromptAsync(prompt);

    return Results.Ok(new AdvisorResponse
    {
        Question = q,
        Answer = result.ToString(),
        RawQueries = rawQueries.Distinct().ToList()
    });
});

app.Run();
