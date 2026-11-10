using Anthropic.Models.Messages;
using Tumiia.Application.Analysis;
using Tumiia.Domain.Analysis;

namespace Tumiia.Infrastructure.Analysis.Bedrock;

/// <summary>
/// Builds the request for one document. PDFs (typed or scanned) are sent as PDFs, so the model sees the pages;
/// photos and scans as JPEG images; everything else as the text TUMIIA read from it. The answer must follow
/// <see cref="BedrockPrompt.OutputSchema"/>.
/// </summary>
public static class BedrockRequestBuilder
{
    /// <summary>The request for <paramref name="content"/>, or why it can't be sent.</summary>
    public static BedrockRequest Build(DocumentContent content, IReadOnlyList<TaxonomyCategory> categories, BedrockSettings settings)
    {
        var (document, problem, note) = DocumentBlock(content, settings);
        if (document is null)
        {
            return new BedrockRequest(null, problem, null);
        }

        var parameters = new MessageCreateParams
        {
            Model = settings.ModelId,
            MaxTokens = settings.MaxTokens,
            System = BedrockPrompt.System(categories),
            OutputConfig = new OutputConfig
            {
                Effort = EffortFor(settings.Effort),
                Format = new JsonOutputFormat { Schema = BedrockPrompt.OutputSchema(categories) },
            },
            Messages =
            [
                new UserMessageParam
                {
                    Content = new List<ContentBlockParam>
                    {
                        document,
                        new TextBlockParam { Text = $"File name: {content.FileName}\nWhat is this document, and what facts are in it?" },
                    },
                },
            ],
        };
        return new BedrockRequest(parameters, null, note);
    }

    /// <summary>The configured effort, or low when the setting is not one of low, medium, high or max.</summary>
    public static Effort EffortFor(string setting) => setting.Trim().ToLowerInvariant() switch
    {
        "medium" => Effort.Medium,
        "high" => Effort.High,
        "max" => Effort.Max,
        _ => Effort.Low,
    };

    private static (ContentBlockParam? Block, string? Problem, string? Note) DocumentBlock(DocumentContent content, BedrockSettings settings)
    {
        if (content.Format.Format == DetectedFormat.Pdf)
        {
            return content.Bytes.Length > settings.MaxPdfMegabytes * 1024L * 1024L
                ? (null, $"The PDF is larger than {settings.MaxPdfMegabytes} MB, so the AI did not read it.", null)
                : (new DocumentBlockParam { Source = new Base64PdfSource { Data = Convert.ToBase64String(content.Bytes) } }, null, null);
        }

        if (content.Format.IsImage)
        {
            var jpeg = ImageShrinker.ToJpeg(content.Bytes, settings.MaxImageEdgePixels);
            return jpeg is null
                ? (null, "The AI can't read this kind of image (TIFF and HEIC). Share it as JPEG or PNG for a better result.", null)
                : (new ImageBlockParam { Source = new Base64ImageSource { Data = Convert.ToBase64String(jpeg), MediaType = MediaType.ImageJpeg } }, null, null);
        }

        return TextBlock(content.Text, settings.MaxTextCharacters);
    }

    // Typed text goes inside <document> tags, so the model can tell the file's words from the question.
    private static (ContentBlockParam? Block, string? Problem, string? Note) TextBlock(DocumentText text, int maxCharacters)
    {
        var all = string.Join('\n', text.Pages.Select(page => $"[Page {page.Number}]\n" + string.Join('\n', page.Lines.Select(line => line.Text))).Concat(text.FileHeaderFacts));
        if (string.IsNullOrWhiteSpace(all))
        {
            return (null, "There was nothing readable to send to the AI.", null);
        }

        var cut = all.Length > maxCharacters;
        var sent = cut ? all[..maxCharacters] : all;
        var note = cut ? $"Only the first {maxCharacters:N0} characters were read by the AI." : null;
        return (new TextBlockParam { Text = $"<document>\n{sent}\n</document>" }, null, note);
    }
}
