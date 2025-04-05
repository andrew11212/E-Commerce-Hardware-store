using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepository;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Utility
{
    /// <summary>
    /// Utility class for monitoring payment system health and order processing
    /// </summary>
    public class PaymentHealthMonitor
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentHealthMonitor> _logger;

        public PaymentHealthMonitor(
            IUnitOfWork unitOfWork,
            ILogger<PaymentHealthMonitor> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Gets payment system health metrics
        /// </summary>
        public async Task<PaymentHealthMetrics> GetHealthMetricsAsync()
        {
            try
            {
                var metrics = new PaymentHealthMetrics();
                var lastDay = DateTime.UtcNow.AddDays(-1);

                // Get all orders from the last 24 hours
                var recentOrders = await _unitOfWork.OrderHeader.GetAllAsync(
                    o => o.OrderDate >= lastDay);

                if (recentOrders.Any())
                {
                    metrics.TotalOrderCount = recentOrders.Count();
                    metrics.TotalOrderValue = recentOrders.Sum(o => o.OrderTotal);
                    
                    // Successful orders
                    var successfulOrders = recentOrders.Where(o => 
                        o.OrderStatus == SD.Status_Shipped || 
                        o.OrderStatus == SD.Status_Approved || 
                        o.OrderStatus == SD.Status_Delivered);
                    
                    metrics.SuccessfulOrderCount = successfulOrders.Count();
                    metrics.SuccessfulOrderValue = successfulOrders.Sum(o => o.OrderTotal);
                    
                    // Failed payment orders
                    var failedPaymentOrders = recentOrders.Where(o => 
                        o.PaymentStatus == SD.Payment_Status_Rejected);
                    
                    metrics.FailedPaymentCount = failedPaymentOrders.Count();
                    metrics.FailedPaymentValue = failedPaymentOrders.Sum(o => o.OrderTotal);
                    
                    // Pending payment orders
                    var pendingPaymentOrders = recentOrders.Where(o => 
                        o.PaymentStatus == SD.Payment_Status_Pending);
                    
                    metrics.PendingPaymentCount = pendingPaymentOrders.Count();
                    metrics.PendingPaymentValue = pendingPaymentOrders.Sum(o => o.OrderTotal);
                    
                    // Payment methods distribution
                    metrics.OnlinePaymentCount = recentOrders.Count(o => o.PaymentMethod == SD.Payment_Method_Online);
                    metrics.CashOnDeliveryCount = recentOrders.Count(o => o.PaymentMethod == SD.Payment_Method_COD);
                    
                    // Calculate conversion rate
                    if (metrics.TotalOrderCount > 0)
                    {
                        metrics.PaymentConversionRate = (double)metrics.SuccessfulOrderCount / metrics.TotalOrderCount * 100;
                    }
                }

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment health metrics");
                return new PaymentHealthMetrics
                {
                    Error = ex.Message,
                    IsError = true
                };
            }
        }
    }

    /// <summary>
    /// Metrics for payment system health
    /// </summary>
    public class PaymentHealthMetrics
    {
        // Total orders
        public int TotalOrderCount { get; set; }
        public double TotalOrderValue { get; set; }
        
        // Successful orders
        public int SuccessfulOrderCount { get; set; }
        public double SuccessfulOrderValue { get; set; }
        
        // Failed payment orders
        public int FailedPaymentCount { get; set; }
        public double FailedPaymentValue { get; set; }
        
        // Pending payment orders
        public int PendingPaymentCount { get; set; }
        public double PendingPaymentValue { get; set; }
        
        // Payment methods distribution
        public int OnlinePaymentCount { get; set; }
        public int CashOnDeliveryCount { get; set; }
        
        // Conversion metrics
        public double PaymentConversionRate { get; set; }
        
        // Error handling
        public bool IsError { get; set; }
        public string Error { get; set; } = string.Empty;
        
        // Timestamp
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
} 