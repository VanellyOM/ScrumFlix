using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace ScrumFlix.Models
{
    public class ConcessionItem
    {
        public int ConcessionItemId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ItemName { get; set; }

        public decimal Price { get; set; }

        public int QuantityInStock { get; set; }

        public int Minimum { get; set; }
        public int LocationId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}