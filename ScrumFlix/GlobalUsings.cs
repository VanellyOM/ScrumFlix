/*
 * File: /ScrumFlix/GlobalUsings.cs
 * Description: Global using directives for ScrumFlix.
 *
 * POLICY: A namespace earns a global using when it appears as a per-file using
 * in 3 or more files across different layers AND its types are ambient to the
 * whole codebase — i.e. any developer writing new ScrumFlix code would
 * reasonably expect them in scope without an explicit import.
 *
 * Library-specific namespaces (Serilog, MailKit, QRCoder, TMDbLib, MiniExcel,
 * etc.) stay as per-file usings in the files that actually use them so the
 * dependency is visible at the call site.
 */

// ── BCL ───────────────────────────────────────────────────────────────────────
global using System;
global using System.Collections.Generic;
global using System.ComponentModel.DataAnnotations;
global using System.ComponentModel.DataAnnotations.Schema;
global using System.IO;
global using System.Linq;
global using System.Threading.Tasks;

// ── ASP.NET Core ──────────────────────────────────────────────────────────────
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.Filters;

// ── Microsoft Extensions ──────────────────────────────────────────────────────
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Logging;

// ── ScrumFlix application namespaces ─────────────────────────────────────────
global using ScrumFlix.Areas.Admin.ViewModels;
global using ScrumFlix.Controllers;
global using ScrumFlix.Data;
global using ScrumFlix.Domain;
global using ScrumFlix.Models;
global using ScrumFlix.Services;
global using ScrumFlix.ViewModels;
