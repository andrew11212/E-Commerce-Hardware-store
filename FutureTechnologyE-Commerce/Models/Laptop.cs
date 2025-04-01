using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FutureTechnologyE_Commerce.Models
{
    public class Laptop : Product // Assuming Product class exists and has its own validation
    {
        //[Required(ErrorMessage = "Processor information is required.")]
        [StringLength(100, ErrorMessage = "Processor name cannot exceed 100 characters.")]
        [Display(Name = "Processor")]
        public string Processor { get; set; }

        //[Required(ErrorMessage = "RAM information is required.")]
        [StringLength(50, ErrorMessage = "RAM description cannot exceed 50 characters.")]
        [Display(Name = "RAM")]
        public string RAM { get; set; }

        //[Required(ErrorMessage = "Storage information is required.")]
        [StringLength(50, ErrorMessage = "Storage description cannot exceed 50 characters.")]
        [Display(Name = "Storage")]
        public string Storage { get; set; } 

        [Column(TypeName = "decimal(4,1)")] // Specifies DB type (4 total digits, 1 decimal place)
        [Range(10.0, 21.0, ErrorMessage = "Screen size must be between 10.0 and 21.0 inches.")] // Sensible range for laptops
        [Display(Name = "Screen Size (inches)")]
        public decimal ScreenSize { get; set; }

        //[Required(ErrorMessage = "Graphics card information is required.")]
        [StringLength(100, ErrorMessage = "Graphics card name cannot exceed 100 characters.")]
        [Display(Name = "Graphics Card")]
        public string GraphicsCard { get; set; } // e.g., "Intel Iris Xe", "NVIDIA GeForce RTX 4060"
    }
}