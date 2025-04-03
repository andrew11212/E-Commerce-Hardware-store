using System.ComponentModel.DataAnnotations;

namespace FutureTechnologyE_Commerce.Models.ViewModels
{
    public class ReturnRequestViewModel
    {
        public int OrderId { get; set; }
        
        [Required(ErrorMessage = "Please provide a reason for the return")]
        [MinLength(10, ErrorMessage = "Please provide more details about the return reason")]
        [MaxLength(500, ErrorMessage = "Return reason cannot exceed 500 characters")]
        public string Reason { get; set; }
    }
} 