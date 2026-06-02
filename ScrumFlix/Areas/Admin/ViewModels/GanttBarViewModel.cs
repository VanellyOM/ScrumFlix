namespace ScrumFlix.Areas.Admin.ViewModels;



public class GanttBarViewModel
{
    public int ShiftId { get; set; }
    public string RoleName { get; set; } = "";
    public string RoleColor { get; set; } = "#D3D3D3";
    public string Label { get; set; } = "";
    public int LeftPx { get; set; }
    public int WidthPx { get; set; }
}