namespace Tumiia.Domain.Analysis;

/// <summary>One line of text read from a file, with how sure the reader is about it.</summary>
/// <param name="Text">The line's text.</param>
/// <param name="Confidence">From 0 to 1: 1 for typed text.</param>
public sealed record TextLine(string Text, double Confidence);
