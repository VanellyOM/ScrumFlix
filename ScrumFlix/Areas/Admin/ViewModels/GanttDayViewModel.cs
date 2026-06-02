namespace ScrumFlix.Areas.Admin.ViewModels;

// ── Gantt Day ───────────────────────────────────────────────────────────────

public class GanttDayViewModel
{
    public DateTime Date { get; set; }
    public List<GanttBarViewModel> Bars { get; set; } = [];
}
