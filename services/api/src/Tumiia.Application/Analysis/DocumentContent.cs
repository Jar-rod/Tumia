using Tumiia.Domain.Analysis;

namespace Tumiia.Application.Analysis;

/// <summary>One document as the hosted AI gets it: the original bytes, what it really is, and the text TUMIIA read itself.</summary>
/// <param name="Bytes">The original file.</param>
/// <param name="Format">Its real format.</param>
/// <param name="FileName">Its original name.</param>
/// <param name="Text">The text TUMIIA read from it (empty for scans and photos).</param>
public sealed record DocumentContent(byte[] Bytes, FileFormat Format, string FileName, DocumentText Text);
