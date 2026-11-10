using System.Text;
using System.Text.Unicode;
using Tumiia.Domain.Analysis;

namespace Tumiia.Infrastructure.Analysis.Reading;

/// <summary>Reads .txt and .csv files (and pasted text): UTF-8, or Windows-1252 when the bytes are not valid UTF-8.</summary>
public static class PlainTextReader
{
    /// <summary>Reads the file as one page, one line per line of text.</summary>
    public static DocumentText Read(byte[] content)
    {
        var text = Utf8.IsValid(content) ? Encoding.UTF8.GetString(content) : Encoding.Latin1.GetString(content);
        var lines = text.TrimStart('﻿')
            .Split('\n')
            .Select(line => line.TrimEnd('\r').Trim())
            .Where(line => line.Length > 0)
            .Select(line => new TextLine(line, 1.0))
            .ToList();
        return new DocumentText { Pages = [new TextPage(1, lines)] };
    }
}
