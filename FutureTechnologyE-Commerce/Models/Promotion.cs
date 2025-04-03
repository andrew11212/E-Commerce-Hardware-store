using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FutureTechnologyE_Commerce.Models
{
    public class Promotion
    {
        [Key]
        public int PromotionId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(255)]
        public string? Description { get; set; }
        
        public string? ImageUrl { get; set; } = string.Empty;
        
        [Required]
        public DateTime StartDate { get; set; } = DateTime.Now;
        
        [Required]
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(30);
        
        public bool IsActive { get; set; } = true;
        
        public int? ProductId { get; set; }
        
        public int DisplayOrder { get; set; } = 1;
        
        [ValidateNever]
        public virtual Product? Product { get; set; }
    }
} 