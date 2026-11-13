using PCBDesigner.Application.Abstractions;
using PCBDesigner.Application.Drawings;
using PCBDesigner.Infrastructure.Recognition;
using PCBDesigner.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

var storageRoot = builder.Configuration["Storage:RootPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "uploads");

builder.Services.AddSingleton<IDrawingStorage>(_ => new LocalDrawingStorage(storageRoot));
builder.Services.AddSingleton<IDrawingClassifier, PlaceholderDrawingClassifier>();
builder.Services.AddScoped<IUploadDrawingService, UploadDrawingService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "PCBDesigner.Api",
    recognition = "Not configured",
    pcbEngine = "Not configured"
}));

app.MapPost("/api/drawings", async (
    IFormFile? file,
    IUploadDrawingService uploadService,
    CancellationToken cancellationToken) =>
{
    if (file is null || file.Length == 0)
        return Results.BadRequest(new { error = "Upload a non-empty image or PDF file." });

    const long maxBytes = 20 * 1024 * 1024;
    if (file.Length > maxBytes)
        return Results.BadRequest(new { error = "Maximum upload size is 20 MB." });

    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    var allowed = new[] { ".png", ".jpg", ".jpeg", ".pdf", ".svg" };
    if (!allowed.Contains(extension))
        return Results.BadRequest(new { error = "Supported formats: PNG, JPG, PDF, SVG." });

    await using var stream = file.OpenReadStream();
    var result = await uploadService.UploadAsync(
        new UploadDrawingRequest(file.FileName, file.ContentType, file.Length, stream),
        cancellationToken);

    return Results.Accepted($"/api/drawings/{result.Id}", result);
})
.DisableAntiforgery();

app.Run();
