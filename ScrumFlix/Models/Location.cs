// This model represents a location and is used by EF Core to map the Location class to the database

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ScrumFlix.Models
{
    public class Location
    {
        public int LocationId { get; set; }

        [MaxLength(100)]
        public string LocationName { get; set; }

        [MaxLength(255)]
        public string? LocationAddress { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
