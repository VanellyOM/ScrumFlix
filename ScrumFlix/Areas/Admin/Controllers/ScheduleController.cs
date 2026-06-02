using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using ScrumFlix.Hubs;


namespace ScrumFlix.Areas.Admin.Controllers;

/// <summary>
/// Handles all schedule management: shifts, the visual Gantt panel,
/// and employee schedule assignments.
///
/// HTMX pattern used throughout:
///   - GET  Index          → full page (first load)
///   - All other actions   → return PartialView so HTMX can swap
///                           just the affected section
///
/// SignalR pattern:
///   - After every mutating action, broadcast to the affected
///     location group so other connected clients auto-refresh.
///   - Client JS calls htmx.ajax() on receiving the event.
///
/// Role guard: Manager or above (RoleId <= 2). Applied per-action via
/// RoleGuard(2) from StaffControllerBase. [Authorize] is not used —
/// ScrumFlix uses session-based auth, not ASP.NET Core Identity.
/// </summary>
[Area("Admin")]
public class ScheduleController : StaffControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<ScheduleHub> _hub;
    private readonly ILogger<ScheduleController> _logger;

    public ScheduleController(
        AppDbContext db,
        IHubContext<ScheduleHub> hub,
        ILogger<ScheduleController> logger)
    {
        _db = db;
        _hub = hub;
        _logger = logger;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the UserId from the authenticated claims principal.
    /// Replaces the WinForms Session.UserId static.
    /// </summary>
    //private int CurrentUserId =>
    //    int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
    //        ?? throw new InvalidOperationException("UserId claim missing."));

    /// <summary>
    /// Writes a record to AuditLog. Call after db.SaveChanges() so
    /// ObjectId is populated, then call SaveChanges() again.
    /// </summary>
    private void Audit(
        string actionType,
        string tableName,
        int? objectId,
        string description,
        string? oldValues = null,
        string? newValues = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = CurrentUserId ?? 0,
            ActionType = actionType,
            TableName = tableName,
            ObjectId = objectId,
            ActionTime = DateTime.Now,
            Description = description,
            OldValues = oldValues,
            NewValues = newValues
        });
    }

    /// <summary>
    /// Broadcasts a SignalR event to all clients watching the given location
    /// and fires-and-forgets (awaited in async actions, ignored on failure).
    /// </summary>
    private async Task BroadcastShiftsUpdated(int locationId) =>
        await _hub.Clients
            .Group(ScheduleHub.LocationGroup(locationId))
            .SendAsync("ShiftsUpdated", locationId);

    private async Task BroadcastAssignmentsUpdated(int locationId) =>
        await _hub.Clients
            .Group(ScheduleHub.LocationGroup(locationId))
            .SendAsync("AssignmentsUpdated", locationId);

    // ── Combo / select-list builders ────────────────────────────────────────

    private List<SelectListItem> GetRoleSelectList(int? selectedId = null) =>
        _db.Roles
            .OrderBy(r => r.RoleName)
            .Select(r => new SelectListItem
            {
                Value = r.RoleId.ToString(),
                Text = r.RoleName ?? "",
                Selected = r.RoleId == selectedId
            })
            .ToList();

    private List<SelectListItem> GetActiveLocationSelectList(int? selectedId = null) =>
        _db.Locations
            .Where(l => l.IsActive)
            .OrderBy(l => l.LocationName)
            .Select(l => new SelectListItem
            {
                Value = l.LocationId.ToString(),
                Text = l.LocationName ?? "",
                Selected = l.LocationId == selectedId
            })
            .ToList();

    private List<SelectListItem> GetEmployeeSelectList(int? selectedUserId = null) =>
        _db.Users
            .Include(u => u.Employee)
            .Include(u => u.Role)
            .OrderBy(u => u.Employee!.LastName)
            .ThenBy(u => u.Employee!.FirstName)
            .Select(u => new SelectListItem
            {
                Value = u.UserId.ToString(),
                Text = (u.Employee!.FirstName ?? "") + " " + (u.Employee.LastName ?? "")
                           + " (" + (u.Role != null ? (u.Role.RoleName ?? "") : "") + ")",
                Selected = u.UserId == selectedUserId
            })
            .ToList();

    private List<SelectListItem> GetShowtimeSelectList(int? selectedId = null)
    {
        var items = _db.Showtimes
            .Include(s => s.Movie)
            .Where(s => s.IsActive)
            .OrderBy(s => s.StartTime)
            .Select(s => new SelectListItem
            {
                Value = s.ShowtimeId.ToString(),
                Text = (s.Movie!.Title ?? "") + " — " + s.StartTime.ToString("MM/dd/yyyy hh:mm tt"),
                Selected = s.ShowtimeId == selectedId
            })
            .ToList();

        // "None" sentinel — matches WinForms ShowtimeComboItem { ShowtimeId = null }
        items.Insert(0, new SelectListItem
        {
            Value = "",
            Text = "None",
            Selected = selectedId == null
        });

        return items;
    }

    /// <summary>
    /// Dynamically builds the month select list from actual shift data,
    /// replacing the hardcoded "May 2026" in the original WinForms form.
    /// </summary>
    private List<SelectListItem> GetShiftMonthSelectList(int? selectedYear = null, int? selectedMonth = null)
    {
        var months = _db.Shifts
            .Select(s => new { s.StartTime.Year, s.StartTime.Month })
            .Distinct()
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.Month)
            .ToList();

        return months
            .Select(m => new SelectListItem
            {
                Value = $"{m.Year}-{m.Month:D2}",
                Text = new DateTime(m.Year, m.Month, 1).ToString("MMMM yyyy"),
                Selected = m.Year == selectedYear && m.Month == selectedMonth
            })
            .ToList();
    }

    // ── Full page ────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /Schedule
    /// Full-page load. Subsequent interactions use partials via HTMX.
    /// </summary>
    [HttpGet]
    public IActionResult Index(int? locationId, int? year, int? month)
    {
        if (RoleGuard(2) is { } redirect) return redirect;
        // Default to first active location if none provided
        locationId ??= _db.Locations
            .Where(l => l.IsActive)
            .OrderBy(l => l.LocationName)
            .Select(l => (int?)l.LocationId)
            .FirstOrDefault();

        // Default to current month
        year ??= DateTime.Today.Year;
        month ??= DateTime.Today.Month;

        var vm = BuildIndexViewModel(locationId, year.Value, month.Value);
        return View(vm);
    }

    // ── Shifts grid ─────────────────────────────────────────────────────────

    /// <summary>
    /// GET /Schedule/ShiftsGrid
    /// Returns the shifts table partial. Called by HTMX and by the
    /// client-side SignalR listener after a remote "ShiftsUpdated" event.
    /// </summary>
    [HttpGet]
    public IActionResult ShiftsGrid()
    {
        if (RoleGuard(2) is { } redirect) return redirect;
        var shifts = GetShiftRows();
        return PartialView("_ShiftsGrid", shifts);
    }

    /// <summary>
    /// GET /Schedule/GetShift/{id}
    /// Returns the shift edit form partial pre-populated with the selected
    /// shift's values. Fired by hx-get on a grid row click.
    /// Fixes the original WinForms gridShifts_CellClick pattern.
    /// </summary>
    [HttpGet]
    public IActionResult GetShift(int id)
    {
        if (RoleGuard(2) is { } redirect) return redirect;
        var shift = _db.Shifts.FirstOrDefault(s => s.ShiftId == id);
        if (shift == null) return NotFound();

        var vm = new ShiftFormViewModel
        {
            ShiftId = shift.ShiftId,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            Roles = GetRoleSelectList(shift.RoleId),
            Locations = GetActiveLocationSelectList(shift.LocationId)
        };

        return PartialView("_ShiftForm", vm);
    }

    /// <summary>
    /// POST /Schedule/AddShift
    /// Creates a new shift. Returns the refreshed shifts grid partial
    /// (and triggers a SignalR broadcast to other connected clients).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddShift(ShiftFormViewModel form)
    {
        if (RoleGuard(2) is { } redirect) return redirect;
        if (!ModelState.IsValid)
            return PartialView("_ShiftForm", RepopulateShiftForm(form));

        if (form.EndTime <= form.StartTime)
        {
            ModelState.AddModelError(nameof(form.EndTime), "End time must be after start time.");
            return PartialView("_ShiftForm", RepopulateShiftForm(form));
        }

        var shift = new Shift
        {
            StartTime = form.StartTime,
            EndTime = form.EndTime,
            RoleId = form.RoleId,
            LocationId = form.LocationId
        };

        _db.Shifts.Add(shift);
        await _db.SaveChangesAsync();

        Audit("ADD_SHIFT", "Shifts", shift.ShiftId,
            "Added shift",
            newValues: $"StartTime={shift.StartTime}, EndTime={shift.EndTime}, RoleId={shift.RoleId}, LocationId={shift.LocationId}");

        await _db.SaveChangesAsync();

        _logger.LogInformation("Shift {ShiftId} added by User {UserId}", shift.ShiftId, CurrentUserId);

        await BroadcastShiftsUpdated(shift.LocationId);

        return PartialView("_ShiftsGrid", GetShiftRows());
    }

    /// <summary>
    /// POST /Schedule/UpdateShift
    /// Updates an existing shift. Returns the refreshed shifts grid partial.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateShift(ShiftFormViewModel form)
    {
        if (RoleGuard(2) is { } redirect) return redirect;
        if (!ModelState.IsValid)
            return PartialView("_ShiftForm", RepopulateShiftForm(form));

        if (form.EndTime <= form.StartTime)
        {
            ModelState.AddModelError(nameof(form.EndTime), "End time must be after start time.");
            return PartialView("_ShiftForm", RepopulateShiftForm(form));
        }

        var shift = await _db.Shifts.FirstOrDefaultAsync(s => s.ShiftId == form.ShiftId);
        if (shift == null) return NotFound();

        var oldValues = $"StartTime={shift.StartTime}, EndTime={shift.EndTime}, RoleId={shift.RoleId}, LocationId={shift.LocationId}";
        var oldLocationId = shift.LocationId;

        shift.StartTime = form.StartTime;
        shift.EndTime = form.EndTime;
        shift.RoleId = form.RoleId;
        shift.LocationId = form.LocationId;

        Audit("UPDATE_SHIFT", "Shifts", shift.ShiftId,
            "Updated shift",
            oldValues: oldValues,
            newValues: $"StartTime={shift.StartTime}, EndTime={shift.EndTime}, RoleId={shift.RoleId}, LocationId={shift.LocationId}");

        await _db.SaveChangesAsync();

        _logger.LogInformation("Shift {ShiftId} updated by User {UserId}", shift.ShiftId, CurrentUserId);

        // Broadcast to both old and new location in case location changed
        await BroadcastShiftsUpdated(shift.LocationId);
        if (oldLocationId != shift.LocationId)
            await BroadcastShiftsUpdated(oldLocationId);

        return PartialView("_ShiftsGrid", GetShiftRows());
    }

    /// <summary>
    /// POST /Schedule/DeleteShift
    /// Deletes a shift. Guards against deletion if assignments exist —
    /// mirrors the original WinForms check. Returns the refreshed grid.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteShift(int shiftId)
    {
        if (RoleGuard(2) is { } redirect) return redirect;
        var shift = await _db.Shifts.FirstOrDefaultAsync(s => s.ShiftId == shiftId);
        if (shift == null) return NotFound();

        bool hasAssignments = await _db.ScheduleAssignments
            .AnyAsync(a => a.ShiftId == shiftId);

        if (hasAssignments)
        {
            ModelState.AddModelError("",
                "This shift has schedule assignments. Delete those assignments before deleting the shift.");
            return PartialView("_ShiftsGrid", GetShiftRows());
        }

        Audit("DELETE_SHIFT", "Shifts", shift.ShiftId,
            "Deleted shift",
            oldValues: $"StartTime={shift.StartTime}, EndTime={shift.EndTime}, RoleId={shift.RoleId}, LocationId={shift.LocationId}");

        var locationId = shift.LocationId;

        _db.Shifts.Remove(shift);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Shift {ShiftId} deleted by User {UserId}", shiftId, CurrentUserId);

        await BroadcastShiftsUpdated(locationId);

        return PartialView("_ShiftsGrid", GetShiftRows());
    }

    // ── Visual schedule (Gantt) ─────────────────────────────────────────────

    /// <summary>
    /// GET /Schedule/ScheduleVisual?locationId=1&amp;year=2026&amp;month=5
    /// Returns the Gantt panel partial. Called by HTMX on location/month
    /// combo change (hx-trigger="change") and by the SignalR listener.
    ///
    /// Month list is now dynamic (from actual shift data) — fixes the
    /// hardcoded "May 2026" limitation in the original WinForms form.
    /// </summary>
    [HttpGet]
    public IActionResult ScheduleVisual(int locationId, int year, int month)
    {
        if (RoleGuard(2) is { } redirect) return redirect;
        var vm = BuildGanttViewModel(locationId, year, month);
        return PartialView("_ScheduleVisual", vm);
    }

    // ── Assignments grid ────────────────────────────────────────────────────

    /// <summary>
    /// GET /Schedule/AssignmentsGrid?locationId=1
    /// Returns the assignments table partial.
    /// </summary>
    [HttpGet]
    public IActionResult AssignmentsGrid(int? locationId)
    {
        if (RoleGuard(2) is { } redirect) return redirect;
        var rows = GetAssignmentRows(locationId);
        return PartialView("_AssignmentsGrid", rows);
    }

    /// <summary>
    /// GET /Schedule/GetAssignment/{id}
    /// Returns the assignment edit form partial pre-populated.
    /// Fixes the unwired gridScheduleAssignments_CellClick bug in the original.
    /// </summary>
    [HttpGet]
    public IActionResult GetAssignment(int id)
    {
        if (RoleGuard(2) is { } redirect) return redirect;
        var assignment = _db.ScheduleAssignments
            .FirstOrDefault(a => a.AssignmentId == id);

        if (assignment == null) return NotFound();

        var vm = new AssignmentFormViewModel
        {
            AssignmentId = assignment.AssignmentId,
            AssignmentName = assignment.AssignmentName,
            UserId = assignment.UserId,
            ShiftId = assignment.ShiftId,
            ShowtimeId = assignment.ShowtimeId,
            Employees = GetEmployeeSelectList(assignment.UserId),
            Showtimes = GetShowtimeSelectList(assignment.ShowtimeId)
        };

        return PartialView("_AssignmentForm", vm);
    }

    /// <summary>
    /// POST /Schedule/AddAssignment
    /// Creates a new schedule assignment with role-match and overlap
    /// validation — exact business rules from the original WinForms form.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAssignment(AssignmentFormViewModel form)
    {
        if (RoleGuard(2) is { } redirect) return redirect;
        if (!ModelState.IsValid)
            return PartialView("_AssignmentForm", RepopulateAssignmentForm(form));

        var (user, shift, error) = await ValidateAssignment(
            form.UserId, form.ShiftId, excludeAssignmentId: null);

        if (error != null)
        {
            ModelState.AddModelError("", error);
            return PartialView("_AssignmentForm", RepopulateAssignmentForm(form));
        }

        var assignment = new ScheduleAssignment
        {
            AssignmentName = (form.AssignmentName?.Trim()) ?? "",
            UserId = user!.UserId,
            ShiftId = shift!.ShiftId,
            ShowtimeId = form.ShowtimeId == 0 ? null : form.ShowtimeId
        };

        _db.ScheduleAssignments.Add(assignment);
        await _db.SaveChangesAsync();

        Audit("ADD_SCHEDULE_ASSIGNMENT", "ScheduleAssignments", assignment.AssignmentId,
            $"Added schedule assignment '{assignment.AssignmentName}'",
            newValues: $"UserId={assignment.UserId}, ShiftId={assignment.ShiftId}, ShowtimeId={assignment.ShowtimeId}");

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Assignment {AssignmentId} added by User {UserId}",
            assignment.AssignmentId, CurrentUserId);

        await BroadcastAssignmentsUpdated(shift!.LocationId);

        return PartialView("_AssignmentsGrid", GetAssignmentRows(shift.LocationId));
    }

    /// <summary>
    /// POST /Schedule/UpdateAssignment
    /// Updates an existing assignment with role-match and overlap validation.
    /// Overlap check excludes the current assignment (same fix as original).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAssignment(AssignmentFormViewModel form)
    {
        if (RoleGuard(2) is { } redirect) return redirect;
        if (!ModelState.IsValid)
            return PartialView("_AssignmentForm", RepopulateAssignmentForm(form));

        var assignment = await _db.ScheduleAssignments
            .FirstOrDefaultAsync(a => a.AssignmentId == form.AssignmentId);

        if (assignment == null) return NotFound();

        var (user, shift, error) = await ValidateAssignment(
            form.UserId, form.ShiftId, excludeAssignmentId: assignment.AssignmentId);

        if (error != null)
        {
            ModelState.AddModelError("", error);
            return PartialView("_AssignmentForm", RepopulateAssignmentForm(form));
        }

        var oldValues = $"AssignmentName={assignment.AssignmentName}, UserId={assignment.UserId}, ShiftId={assignment.ShiftId}, ShowtimeId={assignment.ShowtimeId}";
        var oldLocationId = (await _db.Shifts.FindAsync(assignment.ShiftId))?.LocationId ?? 0;

        assignment.AssignmentName = (form.AssignmentName?.Trim()) ?? "";
        assignment.UserId = user!.UserId;
        assignment.ShiftId = shift!.ShiftId;
        assignment.ShowtimeId = form.ShowtimeId == 0 ? null : form.ShowtimeId;

        Audit("UPDATE_SCHEDULE_ASSIGNMENT", "ScheduleAssignments", assignment.AssignmentId,
            $"Updated schedule assignment '{form.AssignmentName}'",
            oldValues: oldValues,
            newValues: $"AssignmentName={assignment.AssignmentName}, UserId={assignment.UserId}, ShiftId={assignment.ShiftId}, ShowtimeId={assignment.ShowtimeId}");

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Assignment {AssignmentId} updated by User {UserId}",
            assignment.AssignmentId, CurrentUserId);

        await BroadcastAssignmentsUpdated(shift!.LocationId);
        if (oldLocationId != shift.LocationId)
            await BroadcastAssignmentsUpdated(oldLocationId);

        return PartialView("_AssignmentsGrid", GetAssignmentRows(shift.LocationId));
    }

    /// <summary>
    /// POST /Schedule/DeleteAssignment
    /// Deletes a schedule assignment. Returns the refreshed assignments grid.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAssignment(int assignmentId)
    {
        if (RoleGuard(2) is { } redirect) return redirect;
        var assignment = await _db.ScheduleAssignments
            .Include(a => a.Shift)
            .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);

        if (assignment == null) return NotFound();

        var locationId = assignment.Shift?.LocationId ?? 0;

        Audit("DELETE_SCHEDULE_ASSIGNMENT", "ScheduleAssignments", assignment.AssignmentId,
            $"Deleted schedule assignment '{assignment.AssignmentName}'",
            oldValues: $"AssignmentName={assignment.AssignmentName}, UserId={assignment.UserId}, ShiftId={assignment.ShiftId}, ShowtimeId={assignment.ShowtimeId}");

        _db.ScheduleAssignments.Remove(assignment);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Assignment {AssignmentId} deleted by User {UserId}",
            assignmentId, CurrentUserId);

        await BroadcastAssignmentsUpdated(locationId);

        return PartialView("_AssignmentsGrid", GetAssignmentRows(locationId));
    }

    // ── Private query helpers ────────────────────────────────────────────────

    private List<ShiftRowViewModel> GetShiftRows() =>
        _db.Shifts
            .Include(s => s.Role)
            .Include(s => s.Location)
            .OrderBy(s => s.Location!.LocationName)
            .ThenBy(s => s.StartTime)
            .Select(s => new ShiftRowViewModel
            {
                ShiftId = s.ShiftId,
                LocationName = s.Location!.LocationName ?? "",
                RoleName = s.Role!.RoleName ?? "",
                StartTime = s.StartTime,
                EndTime = s.EndTime
            })
            .ToList();

    private List<AssignmentRowViewModel> GetAssignmentRows(int? locationId) =>
        _db.ScheduleAssignments
            .Include(a => a.User).ThenInclude(u => u!.Employee)
            .Include(a => a.Shift).ThenInclude(s => s!.Role)
            .Include(a => a.Shift).ThenInclude(s => s!.Location)
            .Include(a => a.Showtime).ThenInclude(s => s!.Movie)
            .Where(a => locationId == null || a.Shift!.LocationId == locationId)
            .OrderBy(a => a.Shift!.StartTime)
            .Select(a => new AssignmentRowViewModel
            {
                AssignmentId = a.AssignmentId,
                AssignmentName = a.AssignmentName ?? "",
                EmployeeName = a.User!.Employee!.FullName ?? "",
                RoleName = a.Shift!.Role!.RoleName ?? "",
                LocationName = a.Shift.Location!.LocationName ?? "",
                StartTime = a.Shift.StartTime,
                EndTime = a.Shift.EndTime,
                ShowtimeTitle = a.Showtime != null ? (a.Showtime.Movie!.Title ?? "None") : "None"
            })
            .ToList();

    private GanttViewModel BuildGanttViewModel(int locationId, int year, int month)
    {
        var startMonth = new DateTime(year, month, 1);
        var endMonth = startMonth.AddMonths(1);

        const int startHour = 8;
        const int endHour = 24;
        const int hourWidthPx = 70;
        const int timelineLeftPx = 170;

        var shifts = _db.Shifts
            .Include(s => s.Role)
            .Include(s => s.Location)
            .Where(s =>
                s.LocationId == locationId &&
                s.StartTime >= startMonth &&
                s.StartTime < endMonth)
            .OrderBy(s => s.StartTime)
            .ToList();

        var dayGroups = shifts
            .GroupBy(s => s.StartTime.Date)
            .Select(g => new GanttDayViewModel
            {
                Date = g.Key,
                Bars = g.OrderBy(s => s.Role!.RoleName).ThenBy(s => s.StartTime)
                        .Select(s => new GanttBarViewModel
                        {
                            ShiftId = s.ShiftId,
                            RoleName = s.Role?.RoleName ?? "",
                            RoleColor = GetRoleColor(s.Role?.RoleName),
                            Label = $"{s.StartTime:h:mm tt} – {s.EndTime:h:mm tt}",
                            // CSS left/width in pixels, matching the WinForms pixel math
                            LeftPx = timelineLeftPx + (int)((s.StartTime - s.StartTime.Date.AddHours(startHour)).TotalHours * hourWidthPx),
                            WidthPx = Math.Max(60, (int)((s.EndTime - s.StartTime).TotalHours * hourWidthPx))
                        })
                        .ToList()
            })
            .ToList();

        return new GanttViewModel
        {
            LocationId = locationId,
            Year = year,
            Month = month,
            StartHour = startHour,
            EndHour = endHour,
            HourWidthPx = hourWidthPx,
            TimelineLeftPx = timelineLeftPx,
            Days = dayGroups,
            IsEmpty = !shifts.Any()
        };
    }

    private ScheduleIndexViewModel BuildIndexViewModel(int? locationId, int year, int month)
    {
        return new ScheduleIndexViewModel
        {
            // Shifts section
            Shifts = GetShiftRows(),
            ShiftForm = new ShiftFormViewModel
            {
                Roles = GetRoleSelectList(),
                Locations = GetActiveLocationSelectList()
            },

            // Gantt section
            Gantt = BuildGanttViewModel(locationId ?? 0, year, month),
            MonthList = GetShiftMonthSelectList(year, month),
            LocationList = GetActiveLocationSelectList(locationId),

            // Assignments section
            Assignments = GetAssignmentRows(locationId),
            AssignmentForm = new AssignmentFormViewModel
            {
                Employees = GetEmployeeSelectList(),
                Showtimes = GetShowtimeSelectList()
            }
        };
    }

    /// <summary>
    /// Shared business-rule validation for both AddAssignment and UpdateAssignment.
    /// Returns the hydrated User + Shift on success, or an error string on failure.
    /// </summary>
    private async Task<(User? user, Shift? shift, string? error)> ValidateAssignment(
        int userId, int shiftId, int? excludeAssignmentId)
    {
        var user = await _db.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        var shift = await _db.Shifts
            .FirstOrDefaultAsync(s => s.ShiftId == shiftId);

        if (user == null || user.Employee == null)
            return (null, null, "Employee not found.");

        if (shift == null)
            return (null, null, "Shift not found.");

        // Rule 1: role must match (mirrors WinForms check exactly)
        if (user.RoleId != shift.RoleId)
            return (null, null, "This employee's role does not match the selected shift's role.");

        // Rule 2: no overlapping assignments for this user (excludes self on update)
        var overlapQuery = _db.ScheduleAssignments
            .Include(a => a.Shift)
            .Where(a =>
                a.UserId == userId &&
                shift.StartTime < a.Shift!.EndTime &&
                a.Shift.StartTime < shift.EndTime);

        if (excludeAssignmentId.HasValue)
            overlapQuery = overlapQuery.Where(a => a.AssignmentId != excludeAssignmentId.Value);

        bool hasOverlap = await overlapQuery.AnyAsync();

        if (hasOverlap)
            return (null, null, "This employee already has an overlapping schedule assignment.");

        return (user, shift, null);
    }

    // ── Form repopulation (on validation failure) ───────────────────────────

    private ShiftFormViewModel RepopulateShiftForm(ShiftFormViewModel form)
    {
        form.Roles = GetRoleSelectList(form.RoleId);
        form.Locations = GetActiveLocationSelectList(form.LocationId);
        return form;
    }

    private AssignmentFormViewModel RepopulateAssignmentForm(AssignmentFormViewModel form)
    {
        form.Employees = GetEmployeeSelectList(form.UserId);
        form.Showtimes = GetShowtimeSelectList(form.ShowtimeId);
        return form;
    }

    // ── Role color (mirrors WinForms GetRoleColor exactly) ──────────────────

    private static string GetRoleColor(string? roleName) => roleName switch
    {
        "Admin" => "#F08080", // LightCoral
        "Manager" => "#F0E68C", // Khaki
        "Employee" => "#87CEEB", // LightSkyBlue
        _ => "#D3D3D3"  // LightGray
    };
}
