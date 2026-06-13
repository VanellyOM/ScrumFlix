/*
 * File:      /ScrumFlix/Areas/Admin/ViewModels/BackupResultPanelViewModel.cs
 * Namespace: ScrumFlix.Areas.Admin.ViewModels
 * Purpose:   Model for the _BackupResultPanelPartial HTMX swap target rendered
 *            once a queued database backup completes (Phase 4.3).
 *
 *            Carries the operation id (used to build the DownloadBackup link)
 *            and the staged archive's file name when still available in cache.
 *
 * Phase: 4.3 — Background-queue redesign
 */

namespace ScrumFlix.Areas.Admin.ViewModels;

/// <summary>
/// View model for the "Backup ready — Download" panel.
/// </summary>
/// <param name="OperationId">
/// The completed backup's operation id. Used to build the
/// <c>DownloadBackup?operationId=...</c> link.
/// </param>
/// <param name="FileName">
/// The staged archive's file name, when still present in cache; <c>null</c> if
/// the cache entry has already expired (the download link still functions until
/// the TTL elapses).
/// </param>
public sealed record BackupResultPanelViewModel(string OperationId, string? FileName);
