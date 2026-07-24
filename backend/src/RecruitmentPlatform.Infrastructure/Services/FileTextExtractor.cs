using System.Text;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Logging;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;
using UglyToad.PdfPig;

namespace RecruitmentPlatform.Infrastructure.Services;

/// <summary>
/// Extracts plain text from PDF (via PdfPig) and DOCX (via OpenXml) documents. Failures are
/// swallowed and reported as empty text so a bad file never breaks the analysis flow.
/// </summary>
public class FileTextExtractor : ITextExtractor
{
    private const string PdfContentType = "application/pdf";
    private const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    private readonly ILogger<FileTextExtractor> _logger;

    public FileTextExtractor(ILogger<FileTextExtractor> logger) => _logger = logger;

    public async Task<string> ExtractTextAsync(Stream content, string contentType, string fileName, CancellationToken cancellationToken = default)
    {
        // Buffer into memory so the parsers get a seekable stream.
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        try
        {
            if (contentType == PdfContentType || extension == ".pdf")
            {
                return ExtractPdf(buffer);
            }
            if (contentType == DocxContentType || extension == ".docx")
            {
                return ExtractDocx(buffer);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract text from {FileName} ({ContentType})", fileName, contentType);
            return string.Empty;
        }

        // Legacy .doc and unknown formats are not supported for extraction.
        return string.Empty;
    }

    private static string ExtractPdf(Stream stream)
    {
        var sb = new StringBuilder();
        using var document = PdfDocument.Open(stream);
        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }
        return sb.ToString();
    }

    private static string ExtractDocx(Stream stream)
    {
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document?.Body;
        return body is null ? string.Empty : body.InnerText;
    }
}
