using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tumiia.Api.Startup;

namespace Tumiia.UnitTests.Startup;

public class ControllersSetupTests
{
    [Fact]
    public void AddTumiiaControllers_WhenCalled_MakesEveryActionNeedASignedInUserByDefault()
    {
        var services = new ServiceCollection();

        services.AddTumiiaControllers();

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<MvcOptions>>().Value;
        options.Filters.OfType<AuthorizeFilter>().ShouldHaveSingleItem();
    }
}
