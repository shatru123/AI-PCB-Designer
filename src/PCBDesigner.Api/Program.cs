using PCBDesigner.Application.Abstractions;
using PCBDesigner.Application.Drawings;
using PCBDesigner.Infrastructure.Recognition;
using PCBDesigner.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

var storageRoot = builder.Configuration["Storage:RootPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "uploads");

builder.Services.AddSingleton<IDrawingStorage>(_ => new LocalDrawingStorage(storageRoot));
builder.Services.AddSingleton<IDrawingClassifier, HeuristicDrawingClassifier>();
builder.Services.AddScoped<IUploadDrawingService, UploadDrawingService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "PCBDesigner.Api",
    recognition = "Deterministic SVG/PDF text heuristics and raster metadata inspection",
    rasterOcr = "Not configured",
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
    if (extension is not (".png" or ".jpg" or ".jpeg" or ".pdf" or ".svg"))
        return Results.BadRequest(new { error = "Supported formats: PNG, JPG, PDF, SVG." });

    await using var stream = new MemoryStream();
    await file.CopyToAsync(stream, cancellationToken);
    var bytes = stream.ToArray();
    if (!HasValidSignature(bytes, extension))
        return Results.BadRequest(new { error = "File content does not match its extension or is invalid." });

    stream.Position = 0;
    var result = await uploadService.UploadAsync(
        new UploadDrawingRequest(file.FileName, file.ContentType, file.Length, stream),
        cancellationToken);

    return Results.Accepted($"/api/drawings/{result.Id}", result);
})
.DisableAntiforgery();

app.Run();

static bool HasValidSignature(byte[] bytes, string extension) => extension switch
{
    ".png" => bytes.Length >= 24 && bytes.AsSpan(0, 8)
        .SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
    ".jpg" or ".jpeg" => bytes.Length >= 4 && bytes[0] == 0xff && bytes[1] == 0xd8 &&
                         bytes[^2] == 0xff && bytes[^1] == 0xd9,
    ".pdf" => bytes.Length >= 5 && System.Text.Encoding.ASCII.GetString(bytes, 0, 5) == "%PDF-",
    ".svg" => IsSvg(bytes),
    _ => false
};

static bool IsSvg(byte[] bytes)
{
    try
    {
        using var stream = new MemoryStream(bytes);
        using var reader = System.Xml.XmlReader.Create(stream, new System.Xml.XmlReaderSettings
        {
            DtdProcessing = System.Xml.DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 10_000_000
        });
        while (reader.Read())
            if (reader.NodeType == System.Xml.XmlNodeType.Element)
                return reader.LocalName.Equals("svg", StringComparison.OrdinalIgnoreCase);
    }
    catch (System.Xml.XmlException) { }
    return false;
}
