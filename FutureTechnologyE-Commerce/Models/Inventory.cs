using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FutureTechnologyE_Commerce.Models
{
    public class Inventory
    {
        [Key]
        public int InventoryId { get; set; }

        [Required]
        [ForeignKey("Product")]
        public int ProductId { get; set; }

        [Required]
        public int CurrentStock { get; set; }

        [Required]
        [Display(Name = "Low Stock Threshold")]
        public int LowStockThreshold { get; set; } = 10;

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        [Display(Name = "Last Restock Date")]
        public DateTime? LastRestockDate { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [ValidateNever]
        public virtual Product Product { get; set; } = default!;
    }
} 