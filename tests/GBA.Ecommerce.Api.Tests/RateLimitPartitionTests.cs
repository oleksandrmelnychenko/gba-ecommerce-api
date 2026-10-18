using System.Net;
using Microsoft.AspNetCore.Http;

namespace GBA.Ecommerce.Api.Tests;

public sealed class RateLimitPartitionTests {
    [Fact]
    public void Proxied_requests_are_partitioned_by_the_forwarded_client_address() {
        Assert.Equal("203.0.113.7", Startup.GetClientPartitionKey(Request("172.19.0.8", ("X-Forwarded-For", "203.0.113.7"))));
        Assert.Equal("203.0.113.7", Startup.GetClientPartitionKey(Request("172.19.0.8", ("X-Forwarded-For", "203.0.113.7, 10.0.0.2"))));
    }

    [Theory]
    [InlineData("CF-Connecting-IP")]
    [InlineData("X-Real-IP")]
    public void Client_controlled_headers_cannot_choose_the_partition(string header) {
        string first = Startup.GetClientPartitionKey(Request("172.19.0.8", ("X-Forwarded-For", "203.0.113.7"), (header, "198.51.100.1")));
        string second = Startup.GetClientPartitionKey(Request("172.19.0.8", ("X-Forwarded-For", "203.0.113.7"), (header, "198.51.100.2")));

        Assert.Equal("203.0.113.7", first);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Public_peers_cannot_forward_an_address() {
        Assert.Equal("198.51.100.9", Startup.GetClientPartitionKey(Request("198.51.100.9", ("X-Forwarded-For", "203.0.113.7"))));
    }

    private static HttpContext Request(string peer, params (string Name, string Value)[] headers) {
        DefaultHttpContext context = new();
        context.Connection.RemoteIpAddress = IPAddress.Parse(peer);
        foreach ((string name, string value) in headers) {
            context.Request.Headers[name] = value;
        }

        return context;
    }
}
