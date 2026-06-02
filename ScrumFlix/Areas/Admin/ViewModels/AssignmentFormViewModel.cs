using Microsoft.AspNetCore.Mvc.Rendering;

namespace ScrumFlix.Areas.Admin.ViewModels;

public class AssignmentFormViewModel
{
    public int AssignmentId { get; set; }   // 0 = new
    public string AssignmentName { get; set; } = "";
    public int UserId { get; set; }
    public int ShiftId { get; set; }
    public int? ShowtimeId { get; set; }

    public List<SelectListItem> Employees { get; set; } = [];
    public List<SelectListItem> Showtimes { get; set; } = [];
}