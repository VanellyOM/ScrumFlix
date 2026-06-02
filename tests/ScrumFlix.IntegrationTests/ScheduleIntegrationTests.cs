using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace ScrumFlix.IntegrationTests;

public class ScheduleIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ScheduleIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // Use a test-friendly DB or in-memory provider
            builder.ConfigureServices(services =>
            {
                // Remove real DB context and replace with SQLite/in-memory for tests
                // services.RemoveAll(typeof(AppDbContext));
                // services.AddDbContext<AppDbContext>(options =>
                //    options.UseSqlite("DataSource=:memory:")
                // );
            });
        });
    }

    [Fact]
    public async Task Get_HomeDashboard_ReturnsSuccess()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
