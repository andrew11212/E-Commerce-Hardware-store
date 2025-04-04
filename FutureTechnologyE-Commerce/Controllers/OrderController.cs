using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Models.ViewModels;
using FutureTechnologyE_Commerce.Repository.IRepository;
using FutureTechnologyE_Commerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Controllers
{
    [Authorize]
    [EnableRateLimiting("fixed")]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            IUnitOfWork unitOfWork,
            ILogger<OrderController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // Display all orders for the current user
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = GetValidatedUserId();
                if (userId == null) return Unauthorized();

                var orderHeaders = (await _unitOfWork.OrderHeader.GetAllAsync(
                    o => o.ApplicationUserId == userId,
                    includeProperties: "ApplicationUser"))
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                return View(orderHeaders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order history");
                TempData["Error"] = "Failed to load order history. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        // View details of a single order
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userId = GetValidatedUserId();
                if (userId == null) return Unauthorized();

                // Get the order header
                var orderHeader = await _unitOfWork.OrderHeader.GetAsync(
                    o => o.Id == id && o.ApplicationUserId == userId);

                if (orderHeader == null)
                {
                    TempData["Error"] = "Order not found";
                    return RedirectToAction(nameof(Index));
                }

                // Get the order details with Product and Brand information
                var orderDetails = await _unitOfWork.OrderDetail.GetAllAsync(
                    od => od.OrderId == id, 
                    includeProperties: "Product,Product.Brand");

                var orderDetailsVM = new OrderDetailsViewModel
                {
                    OrderHeader = orderHeader,
                    OrderDetails = orderDetails.ToList()
                };

                return View(orderDetailsVM);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order details for orderId: {OrderId}", id);
                TempData["Error"] = "Failed to load order details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Track an order by its ID
        public async Task<IActionResult> Track(int id)
        {
            try
            {
                var userId = GetValidatedUserId();
                if (userId == null) return Unauthorized();

                // Get the order header
                var orderHeader = await _unitOfWork.OrderHeader.GetAsync(
                    o => o.Id == id && o.ApplicationUserId == userId);

                if (orderHeader == null)
                {
                    TempData["Error"] = "Order not found";
                    return RedirectToAction(nameof(Index));
                }

                // Get the order details with Product and Brand information
                var orderDetails = await _unitOfWork.OrderDetail.GetAllAsync(
                    od => od.OrderId == id, 
                    includeProperties: "Product,Product.Brand");

                var trackingVM = new OrderTrackingViewModel
                {
                    OrderHeader = orderHeader,
                    OrderDetails = orderDetails.ToList(),
                    StatusHistory = GenerateStatusHistory(orderHeader)
                };

                return View(trackingVM);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking order for orderId: {OrderId}", id);
                TempData["Error"] = "Failed to track order. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Cancel an order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var userId = GetValidatedUserId();
                if (userId == null) return Unauthorized();

                // Get the order header
                var orderHeader = await _unitOfWork.OrderHeader.GetAsync(
                    o => o.Id == id && o.ApplicationUserId == userId);

                if (orderHeader == null)
                {
                    TempData["Error"] = "Order not found";
                    return RedirectToAction(nameof(Index));
                }

                // Can only cancel if order is pending or approved
                if (orderHeader.OrderStatus != SD.Status_Pending && 
                    orderHeader.OrderStatus != SD.Status_Approved)
                {
                    TempData["Error"] = "Cannot cancel this order. It has already been processed or shipped.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Restore product stock
                var orderDetails = await _unitOfWork.OrderDetail.GetAllAsync(
                    od => od.OrderId == id, 
                    includeProperties: "Product,Product.Brand");

                foreach (var item in orderDetails)
                {
                    var product = item.Product;
                    product.StockQuantity += item.Count;
                    await _unitOfWork.ProductRepository.UpdateAsync(product);
                }

                // Update order status
                orderHeader.OrderStatus = SD.Status_Cancelled;
                orderHeader.PaymentStatus = SD.Payment_Status_Rejected;
                await _unitOfWork.OrderHeader.UpdateAsync(orderHeader);
                await _unitOfWork.SaveAsync();

                TempData["Success"] = "Order cancelled successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order for orderId: {OrderId}", id);
                TempData["Error"] = "Failed to cancel order. Please try again.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // Request a return/refund
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestReturn(ReturnRequestViewModel returnRequest)
        {
            try
            {
                var userId = GetValidatedUserId();
                if (userId == null) return Unauthorized();

                // Get the order header
                var orderHeader = await _unitOfWork.OrderHeader.GetAsync(
                    o => o.Id == returnRequest.OrderId && o.ApplicationUserId == userId);

                if (orderHeader == null)
                {
                    TempData["Error"] = "Order not found";
                    return RedirectToAction(nameof(Index));
                }

                // Can only request return for shipped or delivered orders
                if (orderHeader.OrderStatus != SD.Status_Shipped)
                {
                    TempData["Error"] = "Cannot request return for this order. It must be delivered first.";
                    return RedirectToAction(nameof(Details), new { id = returnRequest.OrderId });
                }

                // Create a return request
                var returnRequestRecord = new ReturnRequest
                {
                    OrderId = returnRequest.OrderId,
                    Reason = returnRequest.Reason,
                    RequestDate = DateTime.UtcNow,
                    Status = "Pending"
                };

                // Add return request to database - assuming you have a ReturnRequest repository
                //await _unitOfWork.ReturnRequest.AddAsync(returnRequestRecord);
                //await _unitOfWork.SaveAsync();

                // For now, just update the order status directly since we don't have a ReturnRequest model yet
                orderHeader.OrderStatus = "Return Requested";
                await _unitOfWork.OrderHeader.UpdateAsync(orderHeader);
                await _unitOfWork.SaveAsync();

                TempData["Success"] = "Return request submitted successfully.";
                return RedirectToAction(nameof(Details), new { id = returnRequest.OrderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting return for orderId: {OrderId}", returnRequest.OrderId);
                TempData["Error"] = "Failed to submit return request. Please try again.";
                return RedirectToAction(nameof(Details), new { id = returnRequest.OrderId });
            }
        }

        #region Helper Methods
        private string? GetValidatedUserId()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrEmpty(userId) ? null : userId;
        }

        private List<OrderStatusEvent> GenerateStatusHistory(OrderHeader orderHeader)
        {
            var history = new List<OrderStatusEvent>();
            
            // Add order creation
            history.Add(new OrderStatusEvent
            {
                Status = "Order Placed",
                Date = orderHeader.OrderDate,
                Description = "Your order has been successfully placed."
            });

            // Add payment confirmation if payment is approved
            if (orderHeader.PaymentStatus == SD.Payment_Status_Approved)
            {
                history.Add(new OrderStatusEvent
                {
                    Status = "Payment Confirmed",
                    Date = orderHeader.PaymentDate ?? orderHeader.OrderDate.AddMinutes(5),
                    Description = "Your payment has been successfully processed."
                });
            }

            // Add processing status if applicable
            if (orderHeader.OrderStatus == SD.Status_Processing ||
                orderHeader.OrderStatus == SD.Status_Shipped)
            {
                history.Add(new OrderStatusEvent
                {
                    Status = "Processing",
                    Date = orderHeader.OrderDate.AddHours(1),
                    Description = "Your order is being prepared for shipping."
                });
            }

            // Add shipped status if applicable
            if (orderHeader.OrderStatus == SD.Status_Shipped)
            {
                history.Add(new OrderStatusEvent
                {
                    Status = "Shipped",
                    Date = orderHeader.ShippingDate,
                    Description = "Your order has been shipped and is on its way to you."
                });
            }

            // Add cancelled status if applicable
            if (orderHeader.OrderStatus == SD.Status_Cancelled)
            {
                history.Add(new OrderStatusEvent
                {
                    Status = "Cancelled",
                    Date = DateTime.UtcNow,
                    Description = "Your order has been cancelled."
                });
            }

            return history.OrderByDescending(h => h.Date).ToList();
        }
        #endregion
    }
} 