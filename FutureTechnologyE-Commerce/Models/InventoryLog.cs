using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FutureTechnologyE_Commerce.Models
{
    public class InventoryLog
    {
        [Key]
        public int LogId { get; set; }

        [Required]
        [ForeignKey("Inventory")]
        public int InventoryId { get; set; }

        [Required]
        public int PreviousStock { get; set; }

        [Required]
        public int NewStock { get; set; }

        [Required]
        public int ChangeQuantity { get; set; }

        [Required]
        public DateTime ChangeDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Change Type")]
        public string ChangeType { get; set; } = string.Empty; // "Addition", "Reduction", "Adjustment"

        public string? Notes { get; set; }

        [ValidateNever]
        public virtual Inventory Inventory { get; set; } = default!;
    }
} 