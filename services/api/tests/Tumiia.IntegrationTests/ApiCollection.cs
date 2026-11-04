namespace Tumiia.IntegrationTests;

/// <summary>
/// Groups the integration tests so they share one API and one database container, instead of starting a
/// new container for every test class.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<TumiiaApiFactory>
{
    /// <summary>The collection name used in <c>[Collection(ApiCollection.Name)]</c>.</summary>
    public const string Name = "API";
}
