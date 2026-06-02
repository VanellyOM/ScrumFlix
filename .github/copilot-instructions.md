Repository: ScrumFlix (ASP.NET Core MVC)

Purpose: Provide concise guidance so future Copilot/CLI sessions understand how to build, run, test and follow repository-specific conventions.

1) Build, test, and lint commands

- Restore .NET tools (used for EF Core CLI):
  - cd ScrumFlix && dotnet tool restore

- Build (project):
  - dotnet build ScrumFlix\ScrumFlix.csproj

- Run (local development):
  - dotnet run --project ScrumFlix\ScrumFlix.csproj

- Publish (release):
  - dotnet publish -c Release -o ./publish --project ScrumFlix\ScrumFlix.csproj

- EF Core migrations / DB
  - (run from ScrumFlix folder)
  - dotnet ef migrations add <Name>
  - dotnet ef database update
  - Note: dotnet-ef tool manifest lives at ScrumFlix\dotnet-tools.json — run dotnet tool restore first.

- Tests
  - No test projects were found in the repository root. If/when test projects are added, run the full suite with:
    - dotnet test
  - To run a single test by fully-qualified name (example):
    - dotnet test --filter "FullyQualifiedName=MyNamespace.MyTests.MyTestMethod"
  - Or filter by display name: --filter "DisplayName=MyTestName"

- Lint / format
  - No repository linter is configured. Optional: install/run dotnet-format:
    - dotnet tool install -g dotnet-format
    - dotnet format --folder ScrumFlix

2) High-level architecture (big-picture)

- ASP.NET Core MVC app (Program.cs uses minimal-host pattern). Entry project: ScrumFlix\ScrumFlix.csproj.
- Persistence: EF Core (Microsoft.EntityFrameworkCore) with Pomelo MySQL provider. Connection string key: "ConnectionStrings:MySQLConnection" (user-secrets or env).
- Logging: Serilog two-stage setup (bootstrap logger then full pipeline). LoggingConfiguration wires Console, MySQL sink persistence, and SMTP email alerts. Serilog levels/filters are read from configuration (appsettings). See Infrastructure/LoggingConfiguration.cs.
- Security: NetEscapades security headers centralized in Infrastructure/SecurityHeadersConfiguration.cs (CSP, HSTS, COOP/COEP, remove Server header).
- Image processing & cache: SixLabors.ImageSharp.Web middleware (configured and must run before routing). Static image cache path: wwwroot/cache/tmdb/.
- Background services: SeatReservationExpiryService (hosted service) and other scoped/singleton services like SeatService, QrCodeService, EmailService, TmdbSyncService.
- Integration: TMDb client for movie metadata (Tmdb:ApiKey), SignalR hub at /scheduleHub for real-time features, QuestPDF for PDF generation, QRCoder for QR PNGs.
- Session: DistributedMemoryCache + ASP.NET Core Session used for shopping cart and auth (cookie name: .ScrumFlix.Session).

3) Key repository-specific conventions and gotchas

- Middleware order is deliberate and must be preserved. Important order (Program.cs comments):
  1. Security headers middleware (UseSecurityHeaders) — must be first
  2. UseHttpsRedirection
  3. UseStaticFiles
  4. CorrelationIdMiddleware
  5. UseSerilogRequestLogging (placed after static files so asset hits are not logged)
  6. UseRouting
  7. UseSession (session required before RoleAuthorizationFilter/Authorization)
  8. UseAuthorization
  9. MapControllerRoute / MapHub

- Service registration order: Logging (Serilog) must be configured before other services. DB context (AddDbContext) should be registered early. Some services depend on lifetime: SeatService scoped; QrCodeService singleton; hosted services must be registered for lifetime work.

- EF Core MySQL server version is intentionally pinned (MySqlServerVersion 8.0.45) to avoid AutoDetect network calls during DI.

- BCrypt is used for password hashing (BCrypt.Net-Next) — do not replace the hasher without migrating stored password hashes.

- Do NOT call EnsureCreated() against production Aiven DB. EnsureCreated is included only for local/dev startup checks; seeding code is commented and must remain commented for production.

- User secrets & required env keys:
  - ConnectionStrings:MySQLConnection
  - Tmdb:ApiKey
  - Email:SmtpHost, Email:SmtpPort, Email:SmtpUser, Email:SmtpPassword, Email:From
  - Logging:Email:SmtpHost (Serilog alert sink settings)

4) Existing AI assistant or helper files

- No CLAUDE.md, AGENTS.md, .cursorrules, .windsurfrules, .clinerules, or similar AI assistant rule files were present when this note was created.

5) Where to look next (quick pointers)

- Program.cs — application wiring, middleware, and service registration (single best file for understanding startup order).
- Infrastructure/LoggingConfiguration.cs — Serilog setup and filter rationale.
- Infrastructure/SecurityHeadersConfiguration.cs — CSP and header tuning guidance.
- Services/* and Controllers/* — domain logic for cart, auth, seats, and TMDb sync.

---

Expanded examples (environment variables, EF Core migrations, tests & linting)

Environment variable / user-secrets snippets (examples)
- ConnectionStrings:MySQLConnection = "Server=127.0.0.1;Port=3306;Database=scrumflix_dev;User=root;Password=secret;"
- Tmdb:ApiKey = "your_tmdb_api_key_here"
- Email:SmtpHost = "smtp.example.com"
- Email:SmtpPort = 587
- Email:SmtpUser = "smtp-user"
- Email:SmtpPassword = "smtp-password"
- Email:From = "noreply@scrumflix.local"
- Logging:Email:SmtpHost = "smtp.example.com"  # Serilog alert sink settings (if used)

Tips:
- Use dotnet user-secrets for local dev (run from ScrumFlix project):
  dotnet user-secrets init
  dotnet user-secrets set "ConnectionStrings:MySQLConnection" "Server=..."

EF Core migrations / common workflows (examples)
- From the repository root (recommended):
  cd ScrumFlix
  dotnet tool restore
  dotnet ef migrations add InitialCreate
  dotnet ef database update

- If multiple projects exist or you need to specify startup/project paths:
  dotnet ef migrations add AddSomething --project ScrumFlix.csproj --startup-project ..\ScrumFlix

- To scaffold a migration SQL script instead of applying immediately:
  dotnet ef migrations script -o ..\migrations\deploy.sql --idempotent

Tests and linting (examples / how-to)
- No test projects are present. Recommended xUnit pattern:
  dotnet new xunit -o tests\ScrumFlix.Tests
  dotnet add tests\ScrumFlix.Tests reference ScrumFlix\ScrumFlix.csproj
  dotnet test

- Run a single test by fully-qualified name or display name:
  dotnet test --filter "FullyQualifiedName=MyNamespace.MyTests.MyTestMethod"
  dotnet test --filter "DisplayName=MyTestName"

- Formatting / linting:
  - Install dotnet-format (optional): dotnet tool install -g dotnet-format
  - Run formatter for the project: dotnet format ScrumFlix\ScrumFlix.csproj

- CI suggestions:
  - Add a pipeline job that runs: dotnet restore, dotnet build, dotnet test, dotnet format --check

CI example (GitHub Actions)
- Suggested workflow file: .github/workflows/dotnet.yml

name: .NET
on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest
    strategy:
      matrix:
        dotnet-version: [10.0.x]
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ matrix.dotnet-version }}
      - name: Restore tools
        run: |
          cd ScrumFlix
          dotnet tool restore
      - name: Restore
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore -c Release
      - name: Format check (optional)
        run: dotnet format --verify-no-changes || true
      - name: Run tests
        run: dotnet test --no-build --verbosity normal

Notes:
- CI should not apply EF migrations to production databases. Use migration scripts (below) and a controlled deploy pipeline.

Sample tests project scaffold (xUnit)
- Create test project:
  cd .
  dotnet new xunit -o tests\ScrumFlix.Tests
  dotnet add tests\ScrumFlix.Tests reference ScrumFlix\ScrumFlix.csproj

- Example test file: tests\ScrumFlix.Tests\ExampleTests.cs

using Xunit;

namespace ScrumFlix.Tests;

public class ExampleTests
{
    [Fact]
    public void Sample_true_is_true()
    {
        Assert.True(true);
    }
}

- Guidance: prefer small, fast unit tests for services. For controllers, add integration tests using WebApplicationFactory<TEntryPoint> in a separate Integration test project.

Migration deployment checklist (safe production flow)
1. Create the migration locally and generate an idempotent SQL script:
   - cd ScrumFlix
   - dotnet ef migrations add <Name>
   - dotnet ef migrations script --idempotent -o migrations\<timestamp>__<Name>.sql
2. Review the SQL script for destructive operations (DROP/ALTER) and locking risks.
3. Run the script in a staging environment that mirrors production; verify app behavior.
4. Backup production DB before applying changes.
5. Schedule a maintenance window for schema changes that lock tables.
6. Apply the script using the DB tooling your ops team prefers (mysql client, Aiven UI, CI job).
7. Monitor logs (Serilog MySQL Logs table) and health endpoints after deployment.

Warnings:
- Do NOT call EnsureCreated() on production databases — use migrations and reviewed SQL scripts.
- Keep MySqlServerVersion pinning in Program.cs in sync with production engine if dialect-sensitive features are used.

---

Full CI workflow (advanced GitHub Actions example with caching & secrets)

- Suggested file: .github/workflows/dotnet-ci.yml

name: .NET CI
on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]
  workflow_dispatch: {}

env:
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: 'true'

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        dotnet-version: [10.0.x]
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ matrix.dotnet-version }}

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ matrix.dotnet-version }}-${{ hashFiles('**/global.json') }}
          restore-keys: |
            nuget-${{ matrix.dotnet-version }}-

      - name: Restore dotnet tools
        run: |
          cd ScrumFlix
          dotnet tool restore

      - name: Restore solution
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore -c Release

      - name: Format check
        run: |
          dotnet tool install -g dotnet-format || true
          dotnet format --verify-no-changes || true

      - name: Run unit tests
        env:
          TMDB__APIKEY: ${{ secrets.TMDB_API_KEY }}
          ConnectionStrings__MySQLConnection: ${{ secrets.MYSQL_CONNECTION }}
        run: dotnet test --no-build --verbosity normal

Notes on secrets:
- Store sensitive values in repository or environment secrets (Settings → Secrets). Use names like MYSQL_CONNECTION, TMDB_API_KEY, SMTP_USER, SMTP_PASSWORD. CI should never write to production DB; tests may use a dedicated CI database or in-memory DB.

Integration test examples using WebApplicationFactory

- Recommended pattern: create an Integration test project that uses WebApplicationFactory<Program> to start the app in-memory and swap services (test DB, mocked external clients).

Example: tests\ScrumFlix.IntegrationTests\ScheduleIntegrationTests.cs

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

Notes:
- For controller-level integration tests that rely on EF, prefer a SQLite in-memory DB and apply migrations at test startup. Seed minimal data programmatically.
- Use mocked external HTTP clients (e.g., TMDb) by replacing IHttpClientFactory or named clients in ConfigureServices.

Templated DB deploy job (safe, manual-approval friendly)

- Use this pattern to apply reviewed SQL scripts to production. Keep this job protected behind required reviewers/environments.

name: DB Deploy
on:
  workflow_dispatch:
    inputs:
      script_path:
        description: 'Path to SQL script inside repo'
        required: true

jobs:
  apply-sql:
    runs-on: ubuntu-latest
    environment: production  # requires environment protection and reviewers
    steps:
      - uses: actions/checkout@v4
      - name: Setup mysql client
        run: sudo apt-get update && sudo apt-get install -y mysql-client

      - name: Apply SQL script
        env:
          MYSQL_HOST: ${{ secrets.PROD_DB_HOST }}
          MYSQL_USER: ${{ secrets.PROD_DB_USER }}
          MYSQL_PASSWORD: ${{ secrets.PROD_DB_PASSWORD }}
        run: |
          mysql -h "$MYSQL_HOST" -u "$MYSQL_USER" -p"$MYSQL_PASSWORD" < ${{ github.event.inputs.script_path }}

Safety and process notes:
- Protect this workflow with environments and required reviewers. Never store plaintext credentials in the repo.
- Prefer running the SQL script through your DB operator or provisioning platform (Aiven UI, managed CI job with limited credentials) rather than CI runner with broad DB privileges.

---

If more detail is desired (full runnable workflow files committed to .github/workflows/, step-by-step integration test seeding patterns, or an opinionated DB deploy pipeline template tied to your ops process), say which deliverable to generate next.