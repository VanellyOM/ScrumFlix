namespace ScrumFlix.Areas.Admin.ViewModels;

// ── Gantt ───────────────────────────────────────────────────────────────────

public class GanttViewModel
{
    public int LocationId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int StartHour { get; set; }
    public int EndHour { get; set; }
    public int HourWidthPx { get; set; }
    public int TimelineLeftPx { get; set; }
    public List<GanttDayViewModel> Days { get; set; } = [];
    public bool IsEmpty { get; set; }
}