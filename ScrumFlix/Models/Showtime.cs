// This model represents a showtime and is used by EF Core to map the Showtime class to the database

using System.ComponentModel.DataAnnotations;
using System;

namespace ScrumFlix.Models;

public class Showtime
{
    public int ShowtimeId { get; set; }
    public int MovieId { get; set; }
    public Movie? Movie { get; set; }
    public int TheaterScreenId { get; set; }
    public TheaterScreen? TheaterScreen { get; set; }
    public DateTime StartTime { get; set; }

    public int Capacity { get; set; } = 50;

    [Range(0, 99999999.99)]
    public decimal PricePerTicket { get; set; } = 0.00m;

    public bool IsActive { get; set; } = true;

    public bool SellTicket(int amount)
    {
        if (Capacity - amount < 0)
        {
            return false; // not enough tickets
        }

        Capacity -= amount;
        return true;
    }

}
