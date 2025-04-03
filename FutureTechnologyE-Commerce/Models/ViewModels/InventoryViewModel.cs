using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FutureTechnologyE_Commerce.Models.ViewModels
{
    public class InventoryViewModel
    {
        public Inventory Inventory { get; set; } = default!;
        
        [ValidateNever]
        public Product Product { get; set; } = default!;

        [ValidateNever]
        public IEnumerable<InventoryLog>? InventoryLogs { get; set; }

        [ValidateNever]
        public IEnumerable<SelectListItem>? ProductList { get; set; }
    }

    public class BulkInventoryUpdateViewModel
    {
        public List<InventoryItemViewModel> Items { get; set; } = new List<InventoryItemViewModel>();
    }

    public class InventoryItemViewModel
    {
        public int InventoryId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int NewStock { get; set; }
        public int LowStockThreshold { get; set; }
        public string? Notes { get; set; }
        public bool IsLowStock => CurrentStock <= LowStockThreshold;
    }

    public class LowStockViewModel
    {
        public List<InventoryItemViewModel> LowStockItems { get; set; } = new List<InventoryItemViewModel>();
    }
} 