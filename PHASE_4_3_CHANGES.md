# ScrumFlix Phase 4.3 — Background-queue redesign (handoff)

Converts TMDb sync and database backup from synchronous-in-request to an
in-process `Channel`-based background queue, with the `/progressHub` SignalR
connection established on page load and completion reflected via HTMX partial
swaps. .NET 10 BCL only — no new NuGet packages.

## Files in this drop (paths are repo-relative; drop in over your tree)

### New
- `ScrumFlix/Services/BackgroundQueue/IBackgroundTaskQueue.cs`
- `ScrumFlix/Services/BackgroundQueue/BackgroundTaskQueue.cs`
- `ScrumFlix/Services/BackgroundQueue/QueuedHostedService.cs`
- `ScrumFlix/Areas/Admin/ViewModels/BackupResultPanelViewModel.cs`
- `ScrumFlix/Areas/Admin/Views/AdminHome/_TmdbSyncStatsPartial.cshtml`
- `ScrumFlix/Areas/Admin/Views/AdminBackup/_BackupResultPanelPartial.cshtml`
- `tests/ScrumFlix.Tests/BackgroundQueue/BackgroundTaskQueueTests.cs`
- `tests/ScrumFlix.Tests/BackgroundQueue/QueuedHostedServiceTests.cs`

### Modified
- `ScrumFlix/Program.cs` — singleton `IBackgroundTaskQueue` (capacity 10) +
  `AddHostedService<QueuedHostedService>()`, plus the using.
- `ScrumFlix/Areas/Admin/Controllers/AdminHomeController.cs` — `TmdbSyncRun`
  rewritten (3 paths); new `TmdbSyncStatsPartial` GET; new
  `BuildTmdbCoverageStatsAsync` helper; queue injected.
- `ScrumFlix/Areas/Admin/Controllers/AdminBackupController.cs` — `TriggerBackup`
  rewritten to enqueue; new `BackupResultPanel` GET; `DownloadBackup` unchanged;
  queue injected.
- `ScrumFlix/Areas/Admin/Views/AdminHome/TmdbSyncPage.cshtml` — coverage section
  extracted to `<partial>`; htmx loaded per-page.
- `ScrumFlix/Areas/Admin/Views/AdminBackup/Backup.cshtml` — `#backup-result-panel`
  placeholder added; htmx loaded per-page.
- `ScrumFlix/wwwroot/js/sf-progress.js` — added `sfProgress.connect()`
  (connect-on-load + `.join(opId, handlers)` dispatcher); `track()` kept.
- `ScrumFlix/wwwroot/js/sf-tmdb-sync.js` — connect on load, enqueue-style POST,
  HTMX swap of `#tmdb-coverage-stats`; `window.location.reload()` removed.
- `ScrumFlix/wwwroot/js/sf-backup.js` — connect on load, enqueue-style POST,
  HTMX swap of `#backup-result-panel`; iframe auto-download + download race
  logic removed.

## HTMX target / swap mapping
| Page | Target | Trigger | Endpoint |
|---|---|---|---|
| TmdbSyncPage | `#tmdb-coverage-stats` (outerHTML) | `ProgressUpdate` `isComplete` | `GET /Admin/AdminHome/TmdbSyncStatsPartial` |
| Backup | `#backup-result-panel` (outerHTML) | `ProgressUpdate` `isComplete` | `GET /Admin/AdminBackup/BackupResultPanel?operationId=...` |

## Key decisions (please review)

1. **Reporter token is `CancellationToken.None`, NOT `HttpContext.RequestAborted`,
   on the queued path.** Critical: the HTTP request returns before the work
   runs, so binding to `RequestAborted` would cancel the operation the instant
   the response was sent. User cancellation now flows solely through
   `ClientCancel → IProgressReporterFactory.Cancel → reporter.CancellationToken`.

2. **Single-movie TMDb sync stays synchronous** (no queue, no reporter) — it is
   sub-second and has no progress UI.

3. **Non-AJAX (JS-disabled) full-catalog TMDb sync degrades gracefully** to the
   old synchronous inline run returning the dashboard view. Backup was already
   effectively JS-required in Phase 4.2 (its form posts via fetch and the action
   returns JSON); that is unchanged — a JS-disabled backup POST enqueues and
   returns `{operationId}` JSON with no client to watch it.

4. **Backup completion now shows a manual "Download .zip" button** (swapped-in
   partial) instead of an auto-triggered iframe download. This removes the
   fragile download-vs-fetch teardown races entirely. The 5-minute cache TTL is
   unchanged, so the button is live for several minutes.

5. **`IMemoryCache` is resolved from the per-item scope** in the backup work
   item. It is a singleton, so the scope returns the same shared instance as the
   root container — documented inline.

6. **Captured singletons are localized** (`var reporterFactory = _reporterFactory;`)
   so the work-item closure does not hold the (short-lived) controller instance.

## Out of scope / called out separately
- **`/tmdbSyncHub` + `TmdbProgressHub.cs` retirement** (pending from Phase 4.1):
  left mapped and untouched, as instructed. Nothing in the 4.3 code path uses it.
- **Pre-existing latent issue (NOT fixed here):** the HTMX library script is not
  loaded anywhere globally — `_AdminLayout` does not load it, and the comment in
  `sf-schedule.js` pointing at `_Layout.cshtml` is stale. The Schedule page's
  `hx-get` / `htmx.ajax` calls are therefore likely broken on the standalone
  admin layout. This drop loads htmx per-page (jsdelivr) only for the two pages
  it touches. Recommend either loading htmx globally in `_AdminLayout` or adding
  it per-page to the Schedule views in a follow-up.

## CSP
No change required. `script-src` already permits `cdn.jsdelivr.net` (htmx loads
from there). `htmx.ajax` calls hit same-origin `/Admin/...` (connect-src `'self'`)
and the `/progressHub` WebSocket is same-origin (already working pre-4.3 for the
Schedule and Phase 4.2 backup hubs).

## Somee.com / app-pool recycle caveat (accepted, documented)
The queue is in-process. Queued/in-flight items are lost on an app-pool recycle —
not a regression (the old synchronous operation died with its recycled request
just the same). No persistence added; out of scope.

## Verification status — READ THIS
- **Static review done:** all C# brace/paren-balanced; all three JS files pass
  `node --check`; service/method signatures cross-checked against the existing
  interfaces (`SyncGenresAsync(ct)`, `SyncAllMoviesAsync(forceAll, progress, ct)`,
  `GenerateAsync(options, reporter, ct)`, `IAuditService.LogAsync`,
  `IEmailService.SendWithPdfAttachmentAsync`).
- **NOT verified — no build/test run:** this sandbox has no .NET SDK and NuGet
  is blocked (403). I did **not** run `dotnet build` or `dotnet test`, so I am
  **not** claiming a green build. Please run locally:
  ```
  dotnet build
  dotnet test tests/ScrumFlix.Tests
  ```
- **Possible build tweak:** the new tests use `Microsoft.Extensions.DependencyInjection`
  (`ServiceCollection`/`BuildServiceProvider`), `Microsoft.Extensions.Hosting`
  (`BackgroundService.StartAsync/StopAsync`), and
  `Microsoft.Extensions.Logging.Abstractions` (`NullLogger<T>`). These should
  flow transitively via the `ScrumFlix` project reference (the web shared
  framework). If the test project fails to resolve them, add to
  `tests/ScrumFlix.Tests/ScrumFlix.Tests.csproj`:
  `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.

## Manual smoke checklist (after deploying)
- TMDb sync / backup trigger returns near-instantly with `{operationId}`.
- `/progressHub` connects on page load (Network tab shows the negotiate on load,
  not on submit) and receives real % updates without disconnecting.
- On completion the relevant panel swaps without a full reload.
- Cancel stops a running sync and a running backup mid-operation.
- The backup `.zip` downloads via the swapped-in Download button.
