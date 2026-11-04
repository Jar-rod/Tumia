namespace Tumiia.Infra.Tests;

/// <summary>
/// Puts every test class in one collection, so they share one set of synthesized stacks and run one at a time.
/// The CDK runs every call through a single Node.js process, which cannot serve tests running in parallel.
/// </summary>
[CollectionDefinition(Name)]
public sealed class CdkCollection : ICollectionFixture<SynthesizedStacks>
{
    /// <summary>The collection name used in <c>[Collection(CdkCollection.Name)]</c>.</summary>
    public const string Name = "CDK";
}
