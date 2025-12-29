using System.Text.Json.Serialization;
using TaxCopilot.Api.Middleware;
using TaxCopilot.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TaxCopilot API",
        Version = "v1",
        Description = "Azure RAG backend for tax document analysis"
    });
});

// Add infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TaxCopilot API v1");
    });
}

app.UseHttpsRedirection();

// Add correlation ID middleware
app.UseCorrelationId();

app.UseAuthorization();

app.MapControllers();

// Initialize resources on startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var blobStorage = scope.ServiceProvider.GetRequiredService<TaxCopilot.Application.Interfaces.IBlobStorageService>();
    var searchService = scope.ServiceProvider.GetRequiredService<TaxCopilot.Application.Interfaces.ISearchService>();

    try
    {
        logger.LogInformation("Initializing Azure resources on startup...");
        await blobStorage.EnsureContainerExistsAsync();
        await searchService.EnsureIndexExistsAsync();
        logger.LogInformation("Azure resources initialized successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize Azure resources on startup. You may need to call POST /api/admin/init manually.");
    }
}

app.Run();

