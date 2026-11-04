using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;

namespace Tumiia.IntegrationTests;

/// <summary>
/// Enforces "every API is in Swagger": each controller action the app exposes must appear in its version's
/// OpenAPI document, with a summary and every response code it declares.
/// </summary>
[Collection(ApiCollection.Name)]
public class SwaggerDocumentTests(TumiiaApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    /// <summary>Every controller action, as ASP.NET Core's API explorer sees it.</summary>
    private List<ApiDescription> AllActions()
    {
        var provider = factory.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        return provider.ApiDescriptionGroups.Items.SelectMany(group => group.Items).ToList();
    }

    private async Task<JsonNode> GetDocumentAsync(string groupName)
    {
        var json = await _client.GetStringAsync($"/openapi/{groupName}.json", TestContext.Current.CancellationToken);
        return JsonNode.Parse(json)!;
    }

    /// <summary>Finds the OpenAPI operation for an action, or null if it is missing from the document.</summary>
    private static JsonNode? FindOperation(JsonNode document, ApiDescription action)
    {
        var path = "/" + action.RelativePath!.Split('?')[0];
        var method = action.HttpMethod!.ToLowerInvariant();
        return document["paths"]?[path]?[method];
    }

    [Fact]
    public void ApiExplorer_WhenAppStarts_FindsAtLeastOneAction()
    {
        var actions = AllActions();

        actions.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task EveryControllerAction_IsInOpenApiDocumentWithASummary()
    {
        var problems = new List<string>();

        foreach (var action in AllActions())
        {
            var document = await GetDocumentAsync(action.GroupName!);
            var operation = FindOperation(document, action);
            var name = $"{action.HttpMethod} /{action.RelativePath}";

            if (operation is null)
            {
                problems.Add($"{name} is missing from /openapi/{action.GroupName}.json");
            }
            else if (string.IsNullOrWhiteSpace(operation["summary"]?.GetValue<string>()))
            {
                problems.Add($"{name} has no summary. Add a /// <summary> comment to the action.");
            }
        }

        problems.ShouldBeEmpty();
    }

    [Fact]
    public async Task EveryControllerAction_DocumentsEveryResponseCodeItDeclares()
    {
        var problems = new List<string>();

        foreach (var action in AllActions())
        {
            var document = await GetDocumentAsync(action.GroupName!);
            var responses = FindOperation(document, action)?["responses"]?.AsObject();

            foreach (var declared in action.SupportedResponseTypes)
            {
                if (responses is null || !responses.ContainsKey(declared.StatusCode.ToString()))
                {
                    problems.Add($"{action.HttpMethod} /{action.RelativePath} does not document status {declared.StatusCode}");
                }
            }
        }

        problems.ShouldBeEmpty();
    }

    [Fact]
    public async Task OpenApiDocument_WhenGenerated_OffersBearerTokenAuthorisation()
    {
        var document = await GetDocumentAsync("v1");

        var scheme = document["components"]?["securitySchemes"]?["Bearer"];
        scheme.ShouldNotBeNull();
        scheme["scheme"]!.GetValue<string>().ShouldBe("bearer");
    }

    [Fact]
    public async Task SwaggerUi_WhenEnabled_IsServed()
    {
        var response = await _client.GetAsync("/swagger/index.html", TestContext.Current.CancellationToken);

        response.IsSuccessStatusCode.ShouldBeTrue();
    }
}
