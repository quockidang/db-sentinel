using DbSentinel.Brain.Tools;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Semantic Kernel with Gemini
var geminiApiKey = builder.Configuration["GEMINI_API_KEY"];
if (string.IsNullOrEmpty(geminiApiKey))
{
    throw new InvalidOperationException("GEMINI_API_KEY is not configured.");
}

builder.Services.AddSingleton<ExplainTool>(); // Register the tool
builder.Services.AddKernel()
    .AddGoogleAIGeminiChatCompletion("gemini-2.0-flash", geminiApiKey);
    

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
