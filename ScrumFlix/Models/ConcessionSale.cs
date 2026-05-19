using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ScrumFlix.Models
{
    public class ConcessionSale
    {
        public int ConcessionSaleId { get; set; }

        public int UserId { get; set; }

        [MaxLength(100)]
        public string CustomerEmail { get; set; }

        public DateTime TimeOfSale { get; set; }

        public decimal Total { get; set; }
        public int LocationId { get; set; }

        public User? User { get; set; }

        public List<ConcessionSaleItem> SaleItems { get; set; } = new();
    }
}