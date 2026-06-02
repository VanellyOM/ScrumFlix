namespace ScrumFlix.Areas.Admin.ViewModels;


public class AssignmentRowViewModel
{
    public int AssignmentId { get; set; }
    public string AssignmentName { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public string RoleName { get; set; } = "";
    public string LocationName { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string ShowtimeTitle { get; set; } = "None";
}
