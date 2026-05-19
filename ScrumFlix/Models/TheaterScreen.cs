// This model represents a screen and is used by EF Core to map the TheaterScreen class to the database

using System.ComponentModel.DataAnnotations;

namespace ScrumFlix.Models;

public class TheaterScreen
{
    public int TheaterScreenId { get; set; }

    [Required, MaxLength(100)]
    public string ScreenName { get; set; } = "";
    public int LocationId { get; set; }
    public Location? Location { get; set; }
    public int Capacity { get; set; } = 50;
    public bool IsActive { get; set; } = true;
    public string ScreenDisplay
    {
        get { return $"{ScreenName} ({Location?.LocationName})"; }
    }
    public string GetLocation
    {
        get { return $"{Location?.LocationName}"; }
    }
    public int GetCapacity
    {
        get { return Capacity; }
    }
}