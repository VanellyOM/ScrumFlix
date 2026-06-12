using Microsoft.AspNetCore.Mvc.Rendering;

namespace ScrumFlix.Areas.Admin.ViewModels;

public class AssignmentFormViewModel
{
    public int AssignmentId { get; set; }   // 0 = new

    /// <summary>FK to AssignmentAreas — replaces the raw AssignmentName string (Phase 3).</summary>
    [Display(Name = "Assignment Area")]
    [Range(1, int.MaxValue, ErrorMessage = "Select an assignment area.")]
    public int AssignmentAreaId { get; set; }

    public int UserId { get; set; }
    public int ShiftId { get; set; }
    public int? ShowtimeId { get; set; }

    public List<SelectListItem> Areas { get; set; } = [];
    public List<SelectListItem> Employees { get; set; } = [];

    /// <summary>Shift options. Phase 3 fix — the form previously reused the
    /// Employees list as a placeholder for the shift dropdown.</summary>
    public List<SelectListItem> Shifts { get; set; } = [];

    public List<SelectListItem> Showtimes { get; set; } = [];
}