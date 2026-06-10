/*
 * File:    tests/ScrumFlix.IntegrationTests/ScheduleIntegrationTests.cs
 * Purpose: Integration smoke tests using WebApplicationFactory<Program>.
 *
 * PREREQUISITES (local):
 *   dotnet user-secrets set "ConnectionStrings:MySQLConnection" "Server=..."
 *   dotnet user-secrets set "Tmdb:ApiKey" "..."
 *
 * PREREQUISITES (CI):
 *   GitHub Actions secrets: MYSQL_CONNECTION, TMDB_API_KEY
 *   These are injected by the dotnet-ci.yml workflow as environment variables.
 *
 * TODO — SQLite in-memory swap:
 *   Uncomment the ConfigureServices block below to replace the real MySQL DB
 *   with an in-memory SQLite instance so tests run without a live connection.
 *   You will also need to:
 *     1. Add <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.x" />
 *        to this project (match the EF Core version in the main project).
 *     2. Call context.Database.EnsureCreated() or apply migrations in the
 *        factory setup to initialise the schema before tests run.
 */

using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ScrumFlix.Data;
using Xunit;

namespace ScrumFlix.IntegrationTests;

public class ScheduleIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ScheduleIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // ── SQLite in-memory DB swap (uncomment to run tests without live MySQL) ──
                // services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
                // services.RemoveAll(typeof(AppDbContext));
                // services.AddDbContext<AppDbContext>(options =>
                //     options.UseSqlite("DataSource=:memory:"));
                //
                // After enabling the swap, seed the schema in a one-time fixture:
                // var sp = services.BuildServiceProvider();
                // using var scope = sp.CreateScope();
                // var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // db.Database.EnsureCreated();
            });
        });
    }

    /// <summary>
    /// Verifies that the home dashboard route returns HTTP 200.
    /// Requires a live DB connection unless the SQLite swap above is enabled.
    /// </summary>
    [Fact]
    public async Task Get_HomeDashboard_ReturnsSuccess()
    {
        var client = _factory.CreateClient();
        //var response = await client.GetAsync("/");
        // Pass the TestContext cancellation token into GetAsync
        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Verifies that the staff login page loads without error.
    /// Does not attempt to authenticate — just checks the GET response.
    /// </summary>
    [Fact]
    public async Task Get_Login_ReturnsSuccess()
    {
        var client = _factory.CreateClient();
        // var response = await client.GetAsync("/Account/Login");
        // Pass the TestContext cancellation token into GetAsync
        var response = await client.GetAsync("/Account/Login", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Verifies that unauthenticated requests to the Admin area are
    /// redirected (302) rather than returning 200 or 500.
    /// </summary>
    [Fact]
    public async Task Get_AdminDashboard_Unauthenticated_Redirects()
    {
        // Do not follow redirects — we want to assert the 302 itself.
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        // var response = await client.GetAsync("/Admin/AdminHome/AdminDashboard");
        // Pass the TestContext cancellation token into GetAsync
        var response = await client.GetAsync("/Admin/AdminHome/AdminDashboard", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
}
