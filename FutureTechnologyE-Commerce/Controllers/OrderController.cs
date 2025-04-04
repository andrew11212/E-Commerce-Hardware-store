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
using System.Text.RegularExpressions;

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ValidateOrder()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Get cart items
                var cartItems = (await _unitOfWork.CartRepositery.GetAllAsync(c => c.ApplicationUserId == userId, "Product")).ToList();
                if (!cartItems.Any())
                {
                    TempData["Error"] = "Your cart is empty";
                    return RedirectToAction("Index", "Cart");
                }

                // Set the price for each cart item
                foreach (var cart in cartItems)
                {
                    cart.price = Math.Round((double)cart.Product.Price, 2);
                }

                // Get user data
                var user = await _unitOfWork.applciationUserRepository.GetAsync(u => u.Id == userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found in Order/ValidateOrder for userId: {UserId}", userId);
                    return NotFound("User not found");
                }

                // Create and populate order header
                var orderHeader = new OrderHeader
                {
                    ApplicationUserId = userId,
                    PaymentMethod = Request.Form["PaymentMethod"],
                    PaymentStatus = SD.Payment_Status_Pending,
                    OrderStatus = SD.Status_Pending,
                    OrderDate = DateTime.UtcNow,
                    OrderTotal = Math.Round(cartItems.Sum(cart => cart.price * cart.Count), 2),
                    first_name = SanitizeInput(user.first_name),
                    last_name = SanitizeInput(user.last_name),
                    street = SanitizeInput(user.street),
                    building = SanitizeInput(user.building),
                    phone_number = SanitizePhoneNumber(user.PhoneNumber),
                    email = SanitizeInput(user.Email),
                    state = SanitizeInput(user.state),
                    floor = SanitizeInput(user.floor)
                };

                using (var transaction = _unitOfWork.BeginTransaction())
                {
                    // Check stock and update product quantities
                    foreach (var cart in cartItems)
                    {
                        // Instead of fetching the product again, use the one already loaded with the cart
                        var product = cart.Product;
                        if (product.StockQuantity < cart.Count)
                        {
                            TempData["Error"] = $"Item {product.Name ?? "unknown"} is out of stock";
                            return RedirectToAction("Index", "Cart");
                        }

                        // Get a clean reference to update the product stock
                        var productToUpdate = await _unitOfWork.ProductRepository.GetAsync(p => p.ProductID == cart.ProductId);
                        if (productToUpdate == null)
                        {
                            TempData["Error"] = $"Product not found for cart item {cart.Id}";
                            return RedirectToAction("Index", "Cart");
                        }

                        productToUpdate.StockQuantity -= cart.Count;
                        await _unitOfWork.ProductRepository.UpdateAsync(productToUpdate);
                    }

                    // Save order header
                    await _unitOfWork.OrderHeader.AddAsync(orderHeader);
                    await _unitOfWork.SaveAsync(); // Save to get the order ID

                    // Create order details
                    foreach (var cart in cartItems)
                    {
                        OrderDetail orderDetail = new()
                        {
                            OrderId = orderHeader.Id,
                            ProductId = cart.ProductId,
                            Price = cart.price,
                            Count = cart.Count
                        };
                        await _unitOfWork.OrderDetail.AddAsync(orderDetail);
                    }

                    // Remove cart items
                    foreach (var cartId in cartItems.Select(c => c.Id).ToList())
                    {
                        var cart = await _unitOfWork.CartRepositery.GetAsync(c => c.Id == cartId);
                        if (cart != null)
                        {
                            await _unitOfWork.CartRepositery.RemoveAsync(cart);
                        }
                    }
                    await _unitOfWork.SaveAsync();

                    // Commit the transaction
                    transaction.Commit();
                }

                // Redirect to payment or confirmation based on payment method
                if (orderHeader.PaymentMethod == SD.Payment_Method_COD)
                {
                    return RedirectToAction("Initialize", "Payment", 
                        new { orderId = orderHeader.Id, paymentMethod = SD.Payment_Method_COD });
                }
                else
                {
                    return RedirectToAction("Initialize", "Payment", 
                        new { orderId = orderHeader.Id, paymentMethod = SD.Payment_Method_Online });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Order/ValidateOrder for userId: {UserId}", 
                    User.FindFirstValue(ClaimTypes.NameIdentifier));
                TempData["Error"] = "Order validation failed. Please try again.";
                return RedirectToAction("Checkout", "Cart");
            }
        }

        // Helper methods
        private string SanitizeInput(string input) =>
            string.IsNullOrWhiteSpace(input) ? " " : Regex.Replace(input.Trim(), @"[<>&'""\\/]", "").Substring(0, Math.Min(255, input.Trim().Length));

        private string SanitizePhoneNumber(string phoneNumber) =>
            string.IsNullOrWhiteSpace(phoneNumber) ? " " :
            Regex.Replace(phoneNumber.Trim(), @"[^0-9+]", "").StartsWith("+") ?
            phoneNumber.Substring(0, Math.Min(20, phoneNumber.Length)) :
            ("+20" + phoneNumber).Substring(0, Math.Min(20, phoneNumber.Length + 3));

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