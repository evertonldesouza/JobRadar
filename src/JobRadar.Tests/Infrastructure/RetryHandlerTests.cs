using System.Net;
using JobRadar.Infrastructure.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobRadar.Tests.Infrastructure;

public class RetryHandlerTests
{
    private sealed class StubHandler(params HttpStatusCode[] sequence) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var status = sequence[Math.Min(Calls, sequence.Length - 1)];
            Calls++;
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }

    [Fact]
    public async Task Send_WithTransientFailures_RetriesUntilSuccess()
    {
        var stub = new StubHandler(HttpStatusCode.InternalServerError, HttpStatusCode.BadGateway, HttpStatusCode.OK);
        var handler = new ResilientRetryHandler(NullLogger<ResilientRetryHandler>.Instance) { InnerHandler = stub };
        using var client = new HttpClient(handler);

        var response = await client.GetAsync("https://example.com/jobs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, stub.Calls);
    }

    [Fact]
    public async Task Send_WithClientError_DoesNotRetry()
    {
        var stub = new StubHandler(HttpStatusCode.NotFound);
        var handler = new ResilientRetryHandler(NullLogger<ResilientRetryHandler>.Instance) { InnerHandler = stub };
        using var client = new HttpClient(handler);

        var response = await client.GetAsync("https://example.com/jobs");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(1, stub.Calls);
    }
}
