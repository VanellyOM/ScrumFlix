using Microsoft.AspNetCore.Mvc.Rendering;

namespace ScrumFlix.Areas.Admin.ViewModels;

// ── Shifts ──────────────────────────────────────────────────────────────────
public class ShiftFormViewModel
{
    public int ShiftId { get; set; }   // 0 = new
    public DateTime StartTime { get; set; } = DateTime.Today.AddHours(8);
    public DateTime EndTime { get; set; } = DateTime.Today.AddHours(16);

    // RoleId and LocationId MUST be model-bound from the POST form data.
    // [BindNever] was previously here by mistake, causing FK constraint failures
    // because EF received RoleId=0 / LocationId=0 which don't exist in the DB.
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "Please select a role.")]
    public int RoleId { get; set; }

    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "Please select a location.")]
    public int LocationId { get; set; }

    public List<SelectListItem> Roles { get; set; } = [];
    public List<SelectListItem> Locations { get; set; } = [];
}