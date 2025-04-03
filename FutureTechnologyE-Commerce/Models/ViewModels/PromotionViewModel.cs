using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FutureTechnologyE_Commerce.Models.ViewModels
{
    public class PromotionViewModel
    {
        public Promotion Promotion { get; set; } = new Promotion();
        
        [ValidateNever]
        public IEnumerable<SelectListItem>? ProductList { get; set; }
    }
} 