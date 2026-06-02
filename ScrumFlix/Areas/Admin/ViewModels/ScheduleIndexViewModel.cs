using Microsoft.AspNetCore.Mvc.Rendering;

namespace ScrumFlix.Areas.Admin.ViewModels;


// ── Index (full page) ───────────────────────────────────────────────────────

public class ScheduleIndexViewModel
{
    public List<ShiftRowViewModel> Shifts { get; set; } = [];
    public ShiftFormViewModel ShiftForm { get; set; } = new();
    public GanttViewModel Gantt { get; set; } = new();
    public List<SelectListItem> MonthList { get; set; } = [];
    public List<SelectListItem> LocationList { get; set; } = [];
    public List<AssignmentRowViewModel> Assignments { get; set; } = [];
    public AssignmentFormViewModel AssignmentForm { get; set; } = new();
}