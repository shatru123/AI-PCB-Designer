using System.Text;
using System.Xml;
using PCBDesigner.Application.Abstractions;
using PCBDesigner.Domain.Drawings;

namespace PCBDesigner.Infrastructure.Recognition;

/// <summary>
/// Conservative, deterministic first-pass classifier. It identifies explicit PCB/CAD
/// vocabulary in SVG/PDF content and inspects raster dimensions/signatures. It does not
/// perform OCR, infer electrical connectivity, or replace engineering review.
/// </summary>
public sealed class HeuristicDrawingClassifier : IDrawingClassifier
{
    private static readonly string[] SchematicTerms =
        ["resistor", "capacitor", "transistor", "opamp", "operational amplifier",
         "schematic", "circuit diagram", "pin", "vcc", "gnd", "r1", "c1", "q1"];
    private static readonly string[] LayoutTerms =
        ["pcb", "printed circuit board", "copper", "solder mask", "silkscreen",
         "gerber", "excellon", "track", "via", "footprint", "edge.cuts", "f.cu", "b.cu"];

    public async Task<DrawingClassificationResult> ClassifyAsync(
        Stream drawing, string fileExtension, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await drawing.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        var ext = fileExtension.ToLowerInvariant();

        if (ext == ".svg")
        {
            var text = ReadSafeSvgText(bytes);
            return ClassifyText(text, "SVG text analysis");
        }

        if (ext == ".pdf")
        {
            var text = Encoding.Latin1.GetString(bytes);
            var extracted = ExtractPdfTextCandidates(text);
            var result = ClassifyText(extracted, "PDF embedded-text heuristic");
            return result with
            {
                Warnings = result.Warnings.Append(
                    "PDF text extraction is heuristic; scanned pages require OCR and visual review.").ToArray()
            };
        }

        if (ext is ".png" or ".jpg" or ".jpeg")
        {
            var (width, height) = ReadRasterDimensions(bytes, ext);
            return new DrawingClassificationResult(
                DrawingKind.Unknown, 0, "Raster metadata inspection",
                ["File signature and image dimensions inspected."],
                ["Raster image recognition/OCR is not configured. Classification remains unknown until visual or AI review."],
                width, height);
        }

        return new DrawingClassificationResult(
            DrawingKind.Unknown, 0, "Unsupported content",
            [], ["No classifier is available for this file type."]);
    }

    private static DrawingClassificationResult ClassifyText(string text, string method)
    {
        var normalized = text.ToLowerInvariant();
        var schematic = SchematicTerms.Where(term => normalized.Contains(term)).Distinct().ToArray();
        var layout = LayoutTerms.Where(term => normalized.Contains(term)).Distinct().ToArray();

        if (schematic.Length > layout.Length && schematic.Length >= 2)
            return new(DrawingKind.CircuitSchematic, Math.Min(.90, .55 + schematic.Length * .05),
                method, schematic.Select(x => $"Schematic indicator: {x}").ToArray(),
                ["Classification is a first-pass hint. Verify every component, pin and connection."]);

        if (layout.Length > schematic.Length && layout.Length >= 2)
            return new(DrawingKind.ExistingPcbLayout, Math.Min(.90, .55 + layout.Length * .05),
                method, layout.Select(x => $"PCB-layout indicator: {x}").ToArray(),
                ["Classification is a first-pass hint. Verify scale, layers, drill data and board geometry."]);

        return new(DrawingKind.Unknown, 0, method,
            schematic.Concat(layout).Select(x => $"Ambiguous indicator: {x}").ToArray(),
            ["Insufficient or conflicting evidence. Select the drawing type manually; do not infer connectivity."]);
    }

    private static string ReadSafeSvgText(byte[] bytes)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 10_000_000,
            IgnoreComments = true
        };
        using var stream = new MemoryStream(bytes);
        using var reader = XmlReader.Create(stream, settings);
        var builder = new StringBuilder();
        while (reader.Read())
        {
            if (reader.NodeType is XmlNodeType.Text or XmlNodeType.CDATA or XmlNodeType.Attribute)
                builder.Append(' ').Append(reader.Value);
            if (builder.Length > 2_000_000) break;
        }
        return builder.ToString();
    }

    private static string ExtractPdfTextCandidates(string content)
    {
        // Deliberately limited: collect printable PDF string fragments, not a full PDF parser.
        var fragments = System.Text.RegularExpressions.Regex.Matches(
            content, @"\((?<text>(?:\\.|[^\\)]){2,})\)")
            .Select(match => match.Groups["text"].Value.Replace("\\(", "(").Replace("\\)", ")"));
        return string.Join(' ', fragments);
    }

    private static (int? Width, int? Height) ReadRasterDimensions(byte[] bytes, string ext)
    {
        if (ext == ".png" && bytes.Length >= 24 &&
            bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return (ReadInt32BigEndian(bytes, 16), ReadInt32BigEndian(bytes, 20));

        if (ext is ".jpg" or ".jpeg" && bytes.Length > 4 && bytes[0] == 0xff && bytes[1] == 0xd8)
        {
            var i = 2;
            while (i + 9 < bytes.Length)
            {
                if (bytes[i] != 0xff) { i++; continue; }
                var marker = bytes[i + 1];
                if (marker is 0xd8 or 0xd9 or 0x01 || marker is >= 0xd0 and <= 0xd7) { i += 2; continue; }
                var length = bytes[i + 2] * 256 + bytes[i + 3];
                if (length < 2 || i + 2 + length > bytes.Length) break;
                if (marker is 0xc0 or 0xc1 or 0xc2 or 0xc3 or 0xc5 or 0xc6 or 0xc7 or 0xc9 or 0xca or 0xcb or 0xcd or 0xce or 0xcf)
                    return (bytes[i + 7] * 256 + bytes[i + 8], bytes[i + 5] * 256 + bytes[i + 6]);
                i += 2 + length;
            }
        }
        return (null, null);
    }

    private static int ReadInt32BigEndian(byte[] bytes, int offset) =>
        (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
}
