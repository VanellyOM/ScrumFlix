namespace ScrumFlix.Areas.Admin.ViewModels;

// ── Shifts ──────────────────────────────────────────────────────────────────

public class ShiftRowViewModel
{
    public int ShiftId { get; set; }
    public string LocationName { get; set; } = "";
    public string RoleName { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}