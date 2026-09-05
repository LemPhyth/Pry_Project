using Microsoft.AspNetCore.Routing;
using Pry.Api;
using Xunit;

namespace Pry.Api.Tests;

public sealed class EmbeddedBackendEndpointTests
{
    [Fact]
    public async Task BuildAsync_discovers_api_controllers_when_hosted_by_another_entry_assembly()
    {
        var dataDirectory = Path.Combine(Path.GetTempPath(), $"pry-embedded-api-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dataDirectory);
        try
        {
            await using var app = await BackendApplication.BuildAsync(configure: builder =>
            {
                builder.Configuration["Pry:DataDirectory"] = dataDirectory;
                builder.Configuration["Pry:Url"] = "http://127.0.0.1:5078";
            }, token: TestContext.Current.CancellationToken);

            var routes = ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(source => source.Endpoints)
                .OfType<RouteEndpoint>()
                .Select(endpoint => endpoint.RoutePattern.RawText)
                .ToArray();

            Assert.Contains("api/v1/runtime", routes);
            Assert.Contains("api/v1/runtime/compute-devices", routes);
            Assert.Contains("api/v1/preferences", routes);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(dataDirectory, recursive: true);
        }
    }
}
