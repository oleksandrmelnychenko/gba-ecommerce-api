using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

namespace GBA.Ecommerce.Api.Tests;

public sealed class EcommerceApiFixture : IDisposable {
    private readonly HttpClientHandler _handler;
    private readonly RateLimitPartitionHeaderHandler _rateLimitHandler;

    public EcommerceApiFixture() {
        Config = new ApiTestConfig();
        _handler = new HttpClientHandler();

        if (Config.AllowInsecureTls) {
            _handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        _rateLimitHandler = new RateLimitPartitionHeaderHandler {
            InnerHandler = _handler
        };

        Client = new HttpClient(_rateLimitHandler) {
            BaseAddress = Config.BaseUri,
            Timeout = TimeSpan.FromSeconds(20)
        };

        Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        Client.DefaultRequestHeaders.UserAgent.ParseAdd("gba-ecommerce-api-tests/1.0");
    }

    public ApiTestConfig Config { get; }
    public HttpClient Client { get; }

    public void Dispose() {
        Client.Dispose();
    }

    private sealed class RateLimitPartitionHeaderHandler : DelegatingHandler {
        private static int _nextPartition = Random.Shared.Next(1, 200);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            if (!request.Headers.Contains("X-Forwarded-For")) {
                request.Headers.TryAddWithoutValidation("X-Forwarded-For", BuildRateLimitPartitionIp());
            }

            return base.SendAsync(request, cancellationToken);
        }

        private static string BuildRateLimitPartitionIp() {
            int suffix = Interlocked.Increment(ref _nextPartition) % 240 + 10;
            return $"203.0.113.{suffix}";
        }
    }
}
