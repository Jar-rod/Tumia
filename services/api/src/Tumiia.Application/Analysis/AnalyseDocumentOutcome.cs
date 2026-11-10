namespace Tumiia.Application.Analysis;

/// <summary>What happened when the worker asked for a document to be analysed.</summary>
public enum AnalyseDocumentOutcome
{
    /// <summary>The document was analysed and moved to its next status.</summary>
    Analysed,

    /// <summary>Nothing to do: the document was already analysed (a repeated message).</summary>
    AlreadyAnalysed,

    /// <summary>Nothing to do: the document does not exist, or its file has not arrived.</summary>
    NotFound,
}
