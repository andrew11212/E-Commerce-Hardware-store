using FutureTechnologyE_Commerce.Models;
using System.Collections.Generic;

namespace FutureTechnologyE_Commerce.Models.ViewModels
{
    public class OrderDetailsViewModel
    {
        public OrderHeader OrderHeader { get; set; } = new OrderHeader();
        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public bool CanCancel => 
            OrderHeader.OrderStatus == Utility.SD.Status_Pending || 
            OrderHeader.OrderStatus == Utility.SD.Status_Approved;
        public bool CanReturn => OrderHeader.OrderStatus == Utility.SD.Status_Shipped;
    }
} 