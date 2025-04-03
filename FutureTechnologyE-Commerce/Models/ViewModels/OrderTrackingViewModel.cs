using FutureTechnologyE_Commerce.Models;
using System;
using System.Collections.Generic;

namespace FutureTechnologyE_Commerce.Models.ViewModels
{
    public class OrderTrackingViewModel
    {
        public OrderHeader OrderHeader { get; set; }
        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public List<OrderStatusEvent> StatusHistory { get; set; } = new List<OrderStatusEvent>();
        public string CurrentStatus => OrderHeader?.OrderStatus;
        public DateTime? EstimatedDeliveryDate => 
            OrderHeader?.OrderStatus == Utility.SD.Status_Shipped ? 
            OrderHeader.ShippingDate.AddDays(3) : null;
    }

    public class OrderStatusEvent
    {
        public string Status { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
    }
} 