using System.Net;
using Microsoft.AspNetCore.Http;
using Tumiia.Api.Security;

namespace Tumiia.UnitTests.Security;

public class ClientIpResolverTests
{
    private static DefaultHttpContext Request(bool verifiedByCloudFront, string? viewerAddress)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");
        if (viewerAddress is not null)
        {
            context.Request.Headers[ClientIpResolver.ViewerAddressHeader] = viewerAddress;
        }

        if (verifiedByCloudFront)
        {
            context.Items[OriginVerifyMiddleware.VerifiedItemKey] = true;
        }

        return context;
    }

    [Fact]
    public void Resolve_WhenRequestCameThroughCloudFront_UsesTheViewerAddress()
    {
        var context = Request(verifiedByCloudFront: true, "198.51.100.10:46532");

        var ip = ClientIpResolver.Resolve(context);

        ip.ShouldBe("198.51.100.10");
    }

    [Fact]
    public void Resolve_WhenHeaderIsPresentButOriginIsNotVerified_IgnoresTheHeader()
    {
        var context = Request(verifiedByCloudFront: false, "198.51.100.10:46532");

        var ip = ClientIpResolver.Resolve(context);

        ip.ShouldBe("10.0.0.5");
    }

    [Fact]
    public void Resolve_WhenVerifiedButHeaderIsGarbage_FallsBackToTheConnection()
    {
        var context = Request(verifiedByCloudFront: true, "not-an-ip");

        var ip = ClientIpResolver.Resolve(context);

        ip.ShouldBe("10.0.0.5");
    }

    [Theory]
    [InlineData("198.51.100.10:46532", "198.51.100.10")]
    [InlineData("2001:db8::1:443", "2001:db8::1")]
    [InlineData("[2001:db8::1]:443", "2001:db8::1")]
    [InlineData("198.51.100.10", null)]
    [InlineData("", null)]
    [InlineData("hello:80", null)]
    public void ParseViewerAddress_WhenGivenIpAndPort_ReturnsTheIp(string header, string? expected)
    {
        var ip = ClientIpResolver.ParseViewerAddress(header);

        ip.ShouldBe(expected);
    }
}
