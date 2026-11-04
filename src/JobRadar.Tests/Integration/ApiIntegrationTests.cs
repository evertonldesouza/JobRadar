using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace JobRadar.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<JobRadarApiFactory>
{
    private readonly HttpClient _client;
    private readonly JobRadarApiFactory _factory;

    public ApiIntegrationTests(JobRadarApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.Seed();
    }

    [Fact]
    public async Task HealthLive_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetJobs_ReturnsPagedShape()
    {
        var response = await _client.GetAsync("/api/Jobs?page=1&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.TryGetProperty("items", out var items));
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.True(json.RootElement.TryGetProperty("totalCount", out _));
    }

    [Fact]
    public async Task Sync_WithoutAuth_Returns401()
    {
        var response = await _client.PostAsync("/api/Jobs/sync", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Favorites_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/Favorites");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400ProblemDetails()
    {
        var response = await _client.PostAsJsonAsync("/api/Auth/register", new
        {
            name = "Ana",
            email = $"ana-{Guid.NewGuid():N}@exemplo.com",
            password = "curta"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("errors", body);
    }

    [Fact]
    public async Task AuthFlow_Register_Me_Refresh_Logout()
    {
        var email = $"user-{Guid.NewGuid():N}@exemplo.com";

        var register = await _client.PostAsJsonAsync("/api/Auth/register", new
        {
            name = "Usuário Teste",
            email,
            password = "senha-forte-123"
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        using var sessionJson = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
        var access = sessionJson.RootElement.GetProperty("token").GetString();
        var refresh = sessionJson.RootElement.GetProperty("refreshToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(access));
        Assert.False(string.IsNullOrWhiteSpace(refresh));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
        var me = await _client.GetAsync("/api/Auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        using var meJson = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        Assert.Equal(email, meJson.RootElement.GetProperty("email").GetString());

        // Refresh rotaciona: um novo refresh é emitido e o antigo deixa de valer.
        var refreshed = await _client.PostAsJsonAsync("/api/Auth/refresh", new { refreshToken = refresh });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        using var refreshedJson = JsonDocument.Parse(await refreshed.Content.ReadAsStringAsync());
        var newRefresh = refreshedJson.RootElement.GetProperty("refreshToken").GetString();
        Assert.NotEqual(refresh, newRefresh);

        var reuseOld = await _client.PostAsJsonAsync("/api/Auth/refresh", new { refreshToken = refresh });
        Assert.Equal(HttpStatusCode.Unauthorized, reuseOld.StatusCode);

        // Logout revoga o refresh atual (a reutilização acima já revogou todas as
        // sessões por segurança; aqui o logout precisa apenas responder 204).
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", refreshedJson.RootElement.GetProperty("token").GetString());
        var logout = await _client.PostAsJsonAsync("/api/Auth/logout", new { refreshToken = newRefresh });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
    }

    [Fact]
    public async Task GetJobById_WithSeededJob_ReturnsJobDetail()
    {
        var response = await _client.GetAsync($"/api/Jobs/{_factory.SeededJobId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(_factory.SeededJobId, json.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("Python Backend Developer", json.RootElement.GetProperty("title").GetString());
        var technologies = json.RootElement.GetProperty("technologies")
            .EnumerateArray()
            .Select(t => t.GetString() ?? string.Empty)
            .ToList();
        Assert.Contains("python", technologies);
    }

    [Fact]
    public async Task GetJobById_WithUnknownId_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync($"/api/Jobs/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("detail", body);
    }

    [Fact]
    public async Task GetJobs_FilteredByTechnology_FindsJobsWithoutExactTagCase()
    {
        // "PYTHON" (caixa alta) precisa achar as vagas tagueadas como "python".
        var response = await _client.GetAsync("/api/Jobs?technology=PYTHON&page=1&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("totalCount").GetInt32() >= 2);
    }

    [Fact]
    public async Task MergeFavorites_WithoutAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/Favorites/merge", new { jobIds = Array.Empty<Guid>() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MergeFavorites_SyncsLocalFavoritesWithoutDuplicates()
    {
        var email = $"fav-{Guid.NewGuid():N}@exemplo.com";
        var register = await _client.PostAsJsonAsync("/api/Auth/register", new
        {
            name = "Favoritos",
            email,
            password = "senha-forte-123"
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        using var session = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
        var access = session.RootElement.GetProperty("token").GetString();

        // Cada teste recebe um HttpClient novo da factory: o header não vaza entre testes.
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);

        var seeded = _factory.SeededJobId;
        var unknown = Guid.NewGuid();

        var first = await _client.PostAsJsonAsync("/api/Favorites/merge",
            new { jobIds = new[] { seeded, unknown, seeded } });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        using var firstJson = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        var items = firstJson.RootElement.EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal(seeded, items[0].GetProperty("id").GetGuid());

        // Idempotente: repetir não duplica nem falha.
        var second = await _client.PostAsJsonAsync("/api/Favorites/merge", new { jobIds = new[] { seeded } });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        using var secondJson = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        Assert.Single(secondJson.RootElement.EnumerateArray());

        var list = await _client.GetAsync("/api/Favorites");
        using var listJson = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.Single(listJson.RootElement.EnumerateArray());
    }
}
