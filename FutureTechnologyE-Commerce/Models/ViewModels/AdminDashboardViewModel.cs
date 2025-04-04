using System;
using System.Collections.Generic;

namespace FutureTechnologyE_Commerce.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Sales overview data
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; }
        
        // Time period filters
        public DateTime StartDate { get; set; } = DateTime.Now.AddDays(-30);
        public DateTime EndDate { get; set; } = DateTime.Now;
        
        // Recent orders
        public List<OrderHeader> RecentOrders { get; set; } = new List<OrderHeader>();
        
        // Sales by period chart data
        public List<ChartDataPoint> SalesByPeriod { get; set; } = new List<ChartDataPoint>();
        
        // Top selling products
        public List<TopSellingProduct> TopSellingProducts { get; set; } = new List<TopSellingProduct>();
        
        // Low stock items
        public List<InventoryItemViewModel> LowStockItems { get; set; } = new List<InventoryItemViewModel>();
        
        // Order status summary
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
    }

    public class ChartDataPoint
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public class TopSellingProduct
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public int CurrentStock { get; set; }
    }
} 