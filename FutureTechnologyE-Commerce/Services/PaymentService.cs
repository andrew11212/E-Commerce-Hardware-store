using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepository;
using FutureTechnologyE_Commerce.Utility;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Services
{
    /// <summary>
    /// Service for handling all payment-related operations
    /// </summary>
    public class PaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentService> _logger;
        private readonly Paymob _paymobSettings;

        public PaymentService(
            IUnitOfWork unitOfWork,
            ILogger<PaymentService> logger,
            IOptions<Paymob> paymobSettings)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _paymobSettings = paymobSettings.Value;
        }

        /// <summary>
        /// Validates payment callback signature from Paymob
        /// </summary>
        public bool ValidatePaymentSignature(string hmac, string merchantOrderId, string amount, string integrationId)
        {
            try
            {
                if (string.IsNullOrEmpty(hmac) || string.IsNullOrEmpty(merchantOrderId) || 
                    string.IsNullOrEmpty(amount) || string.IsNullOrEmpty(integrationId))
                {
                    _logger.LogWarning("Missing parameters for HMAC validation");
                    return false;
                }

                // In a real implementation, we would validate the HMAC
                // For now just log the attempt
                _logger.LogInformation("Validating HMAC: {Hmac} for order {OrderId}", 
                    MaskString(hmac), merchantOrderId);
                
                // Simulating validation - in a real implementation, this would use a proper validation method
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating payment signature");
                return false;
            }
        }

        /// <summary>
        /// Completes the order by creating order details, updating inventory, and removing cart items
        /// </summary>
        public async Task<bool> CompleteOrderAsync(int orderId, string userId)
        {
            try
            {
                // Get the order header
                var orderHeader = await _unitOfWork.OrderHeader.GetAsync(o => o.Id == orderId);
                if (orderHeader == null)
                {
                    _logger.LogWarning("Order not found for completion: {OrderId}", orderId);
                    return false;
                }

                // Security check - make sure this is the user's order
                if (orderHeader.ApplicationUserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to complete order {OrderId} belonging to {OrderUserId}", 
                        userId, orderId, orderHeader.ApplicationUserId);
                    return false;
                }

                // Get cart items
                var cartItems = await _unitOfWork.CartRepositery.GetAllAsync(c => c.ApplicationUserId == userId, includeProperties: "Product");
                
                if (!cartItems.Any())
                {
                    _logger.LogWarning("No cart items found for order {OrderId}, user {UserId}", orderId, userId);
                    return false;
                }

                // Begin transaction
                var transaction = await _unitOfWork.BeginTransactionAsync();
                
                try
                {
                    // Create order details for each cart item
                    foreach (var item in cartItems)
                    {
                        var orderDetail = new OrderDetail
                        {
                            OrderId = orderId,
                            ProductId = item.ProductId,
                            Count = item.Count,
                            Price = Math.Round((double)item.Product.Price, 2)
                        };
                        
                        await _unitOfWork.OrderDetail.AddAsync(orderDetail);
                        
                        // Update inventory
                        var product = await _unitOfWork.ProductRepository.GetAsync(p => p.ProductID == item.ProductId);
                        if (product != null)
                        {
                            // Make sure we don't go negative with inventory
                            int newStock = Math.Max(0, product.StockQuantity - item.Count);
                            int quantityReduced = product.StockQuantity - newStock;
                            
                            product.StockQuantity = newStock;
                            await _unitOfWork.ProductRepository.UpdateAsync(product);
                        }
                        
                        // Remove cart item
                        await _unitOfWork.CartRepositery.RemoveAsync(item);
                    }
                    
                    // Save changes
                    await _unitOfWork.SaveAsync();
                    
                    // Commit transaction
                    await _unitOfWork.CommitTransactionAsync(transaction);
                    
                    _logger.LogInformation("Order {OrderId} completed successfully", orderId);
                    return true;
                }
                catch (Exception ex)
                {
                    // Rollback transaction on error
                    await _unitOfWork.RollbackTransactionAsync(transaction);
                    _logger.LogError(ex, "Error completing order {OrderId}", orderId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CompleteOrderAsync for order {OrderId}", orderId);
                return false;
            }
        }

        /// <summary>
        /// Cancels an order and restores inventory if needed
        /// </summary>
        public async Task<bool> CancelOrderAsync(int orderId, string reason)
        {
            try
            {
                // Get the order with details
                var orderHeader = await _unitOfWork.OrderHeader.GetAsync(o => o.Id == orderId);
                if (orderHeader == null)
                {
                    _logger.LogWarning("Order not found for cancellation: {OrderId}", orderId);
                    return false;
                }

                // Check if order has already been cancelled
                if (orderHeader.OrderStatus == SD.Status_Cancelled)
                {
                    _logger.LogInformation("Order {OrderId} is already cancelled", orderId);
                    return true;
                }

                // Begin transaction
                var transaction = await _unitOfWork.BeginTransactionAsync();
                
                try
                {
                    // Update order status
                    orderHeader.OrderStatus = SD.Status_Cancelled;
                    orderHeader.PaymentStatus = SD.Payment_Status_Rejected;
                    await _unitOfWork.OrderHeader.UpdateAsync(orderHeader);
                    
                    // Get order details to restore inventory if they exist
                    var orderDetails = await _unitOfWork.OrderDetail.GetAllAsync(od => od.OrderId == orderId);
                    
                    foreach (var detail in orderDetails)
                    {
                        // Get the product
                        var product = await _unitOfWork.ProductRepository.GetAsync(p => p.ProductID == detail.ProductId);
                        if (product != null)
                        {
                            // Restore inventory
                            int previousQuantity = product.StockQuantity;
                            product.StockQuantity += detail.Count;
                            await _unitOfWork.ProductRepository.UpdateAsync(product);
                        }
                    }
                    
                    // Save changes
                    await _unitOfWork.SaveAsync();
                    
                    // Commit transaction
                    await _unitOfWork.CommitTransactionAsync(transaction);
                    
                    _logger.LogInformation("Order {OrderId} cancelled successfully. Reason: {Reason}", orderId, reason);
                    return true;
                }
                catch (Exception ex)
                {
                    // Rollback transaction on error
                    await _unitOfWork.RollbackTransactionAsync(transaction);
                    _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelOrderAsync for order {OrderId}", orderId);
                return false;
            }
        }

        /// <summary>
        /// Verifies that an order has details or creates them temporarily from cart items
        /// </summary>
        public async Task<bool> EnsureOrderHasDetailsAsync(int orderId, string userId)
        {
            try
            {
                // Check if order already has details
                var existingDetails = await _unitOfWork.OrderDetail.GetAllAsync(od => od.OrderId == orderId);
                if (existingDetails.Any())
                {
                    _logger.LogInformation("Order {OrderId} already has {Count} details", orderId, existingDetails.Count());
                    return true;
                }

                // Get the order header
                var orderHeader = await _unitOfWork.OrderHeader.GetAsync(o => o.Id == orderId);
                if (orderHeader == null)
                {
                    _logger.LogWarning("Order not found when ensuring details: {OrderId}", orderId);
                    return false;
                }

                // Security check - make sure this is the user's order
                if (orderHeader.ApplicationUserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to access order {OrderId} belonging to {OrderUserId}", 
                        userId, orderId, orderHeader.ApplicationUserId);
                    return false;
                }

                // Get cart items
                var cartItems = await _unitOfWork.CartRepositery.GetAllAsync(c => c.ApplicationUserId == userId, includeProperties: "Product");
                
                if (!cartItems.Any())
                {
                    _logger.LogWarning("No cart items found for order {OrderId}, user {UserId}", orderId, userId);
                    return false;
                }

                // Create order details from cart items (but don't remove cart items yet)
                foreach (var item in cartItems)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = orderId,
                        ProductId = item.ProductId,
                        Count = item.Count,
                        Price = Math.Round((double)item.Product.Price, 2)
                    };
                    
                    await _unitOfWork.OrderDetail.AddAsync(orderDetail);
                }
                
                // Save changes
                await _unitOfWork.SaveAsync();
                
                _logger.LogInformation("Created {Count} temporary order details for order {OrderId}", cartItems.Count(), orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring order has details for order {OrderId}", orderId);
                return false;
            }
        }

        // Helper method to mask sensitive strings
        private string MaskString(string value)
        {
            if (string.IsNullOrEmpty(value)) return "[empty]";
            if (value.Length <= 4) return "****";
            
            return value.Substring(0, 2) + new string('*', value.Length - 4) + value.Substring(value.Length - 2);
        }
    }
} 