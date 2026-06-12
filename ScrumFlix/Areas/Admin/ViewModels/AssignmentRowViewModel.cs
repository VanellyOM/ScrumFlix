namespace ScrumFlix.Areas.Admin.ViewModels;


public class AssignmentRowViewModel
{
    public int AssignmentId { get; set; }
    /// <summary>Display name from AssignmentArea.AreaName (Phase 3 — replaces AssignmentName).</summary>
    public string AreaName { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public string RoleName { get; set; } = "";
    public string LocationName { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string ShowtimeTitle { get; set; } = "None";
}
