using FutureTechnologyE_Commerce.Repository.IRepository;
using FutureTechnologyE_Commerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Controllers
{
    [Authorize(Roles = SD.Role_Admin)]
    [Route("api/[controller]")]
    [ApiController]
    public class DiagnosticsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DiagnosticsController> _logger;
        private readonly PaymentHealthMonitor _healthMonitor;

        public DiagnosticsController(
            IUnitOfWork unitOfWork,
            ILogger<DiagnosticsController> logger,
            PaymentHealthMonitor healthMonitor)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _healthMonitor = healthMonitor;
        }

        /// <summary>
        /// Get system health metrics related to payment processing
        /// </summary>
        [HttpGet("payment-health")]
        public async Task<IActionResult> GetPaymentHealth()
        {
            try
            {
                var metrics = await _healthMonitor.GetHealthMetricsAsync();
                if (metrics.IsError)
                {
                    return StatusCode(500, new { error = metrics.Error });
                }
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPaymentHealth");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get statistics on abandoned orders
        /// </summary>
        [HttpGet("abandoned-orders")]
        public async Task<IActionResult> GetAbandonedOrders()
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddHours(-1);
                
                // Find orders that are pending for more than 1 hour
                var abandonedOrders = await _unitOfWork.OrderHeader.GetAllAsync(
                    o => o.OrderStatus == SD.Status_Pending && 
                         o.OrderDate < cutoffTime);
                
                // Count orders with no details (truly abandoned)
                int abandonedCount = 0;
                double abandonedValue = 0;
                
                foreach (var order in abandonedOrders)
                {
                    // Check if this order has order details
                    var hasOrderDetails = (await _unitOfWork.OrderDetail.GetAllAsync(od => od.OrderId == order.Id)).Any();
                    
                    // If no order details, this is an abandoned order
                    if (!hasOrderDetails)
                    {
                        abandonedCount++;
                        abandonedValue += order.OrderTotal;
                    }
                }
                
                return Ok(new { 
                    TotalPendingOrders = abandonedOrders.Count(),
                    AbandonedOrders = abandonedCount,
                    AbandonedValue = abandonedValue
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAbandonedOrders");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get recent payment errors
        /// </summary>
        [HttpGet("payment-errors")]
        public async Task<IActionResult> GetPaymentErrors()
        {
            try
            {
                var lastWeek = DateTime.UtcNow.AddDays(-7);
                
                // Find orders with rejected payments in the last week
                var rejectedPayments = await _unitOfWork.OrderHeader.GetAllAsync(
                    o => o.PaymentStatus == SD.Payment_Status_Rejected && 
                         o.OrderDate >= lastWeek,
                    "ApplicationUser");
                
                // Group by day
                var dailyStats = rejectedPayments
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new {
                        Date = g.Key,
                        Count = g.Count(),
                        TotalValue = g.Sum(o => o.OrderTotal)
                    })
                    .OrderBy(x => x.Date)
                    .ToList();
                
                // Group by payment method
                var methodStats = rejectedPayments
                    .GroupBy(o => o.PaymentMethod ?? "Unknown")
                    .Select(g => new {
                        Method = g.Key,
                        Count = g.Count(),
                        TotalValue = g.Sum(o => o.OrderTotal)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();
                
                return Ok(new { 
                    TotalRejectedCount = rejectedPayments.Count(),
                    TotalRejectedValue = rejectedPayments.Sum(o => o.OrderTotal),
                    DailyBreakdown = dailyStats,
                    PaymentMethodBreakdown = methodStats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPaymentErrors");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
} 