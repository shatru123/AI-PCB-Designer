using System.Text;
using PCBDesigner.Domain.Drawings;
using PCBDesigner.Infrastructure.Recognition;

namespace PCBDesigner.Tests;

public sealed class HeuristicDrawingClassifierTests
{
    [Fact]
    public async Task ClassifiesSvgWithSchematicIndicators()
    {
        const string svg = """<svg xmlns="http://www.w3.org/2000/svg"><text>circuit schematic resistor capacitor</text></svg>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svg));

        var result = await new HeuristicDrawingClassifier().ClassifyAsync(stream, ".svg", default);

        Assert.Equal(DrawingKind.CircuitSchematic, result.Kind);
        Assert.True(result.Confidence > 0);
        Assert.Contains(result.Evidence, item => item.Contains("resistor"));
    }

    [Fact]
    public async Task ClassifiesSvgWithPcbLayoutIndicators()
    {
        const string svg = """<svg xmlns="http://www.w3.org/2000/svg"><text>PCB copper silkscreen footprint</text></svg>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svg));

        var result = await new HeuristicDrawingClassifier().ClassifyAsync(stream, ".svg", default);

        Assert.Equal(DrawingKind.ExistingPcbLayout, result.Kind);
    }

    [Fact]
    public async Task LeavesAmbiguousSvgUnknown()
    {
        const string svg = """<svg xmlns="http://www.w3.org/2000/svg"><text>drawing title</text></svg>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svg));

        var result = await new HeuristicDrawingClassifier().ClassifyAsync(stream, ".svg", default);

        Assert.Equal(DrawingKind.Unknown, result.Kind);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public async Task RejectsSvgWithDocumentTypeDeclaration()
    {
        const string svg = """<!DOCTYPE svg [<!ENTITY xxe SYSTEM "file:///etc/passwd">]><svg xmlns="http://www.w3.org/2000/svg"><text>&xxe;</text></svg>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svg));

        await Assert.ThrowsAsync<System.Xml.XmlException>(() =>
            new HeuristicDrawingClassifier().ClassifyAsync(stream, ".svg", default));
    }

    [Fact]
    public async Task ReadsPngDimensionsWithoutClaimingRecognition()
    {
        var png = new byte[24];
        new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }.CopyTo(png, 0);
        png[19] = 100;
        png[23] = 50;
        using var stream = new MemoryStream(png);

        var result = await new HeuristicDrawingClassifier().ClassifyAsync(stream, ".png", default);

        Assert.Equal(DrawingKind.Unknown, result.Kind);
        Assert.Equal(100, result.WidthPixels);
        Assert.Equal(50, result.HeightPixels);
        Assert.Contains(result.Warnings, warning => warning.Contains("OCR"));
    }
}
