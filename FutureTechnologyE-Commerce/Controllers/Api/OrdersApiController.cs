using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepository;
using FutureTechnologyE_Commerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Controllers.Api
{
    [Route("api/orders")]
    [ApiController]
    [Authorize]
    public class OrdersApiController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrdersApiController> _logger;

        public OrdersApiController(
            IUnitOfWork unitOfWork,
            ILogger<OrdersApiController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Completes an order after payment verification
        /// </summary>
        [HttpPost("complete")]
        public async Task<IActionResult> CompleteOrder([FromBody] OrderCompletionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid request data", errors = ModelState });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                // Get the order
                var orderHeader = await _unitOfWork.OrderHeader.GetAsync(o => o.Id == request.OrderId);
                if (orderHeader == null)
                {
                    return NotFound(new { success = false, message = $"Order with ID {request.OrderId} not found" });
                }

                // Verify the user can access this order (or is an admin)
                if (orderHeader.ApplicationUserId != userId && !User.IsInRole(SD.Role_Admin))
                {
                    return Forbid();
                }

                // Update order status
                orderHeader.PaymentStatus = request.PaymentSuccessful ? SD.Payment_Status_Approved : SD.Payment_Status_Rejected;
                orderHeader.OrderStatus = request.PaymentSuccessful ? SD.Status_Approved : SD.Status_Cancelled;
                
                if (!string.IsNullOrEmpty(request.TransactionId))
                {
                    orderHeader.TransactionId = request.TransactionId;
                }
                
                orderHeader.PaymentDate = DateTime.UtcNow;

                // If payment failed, restore inventory
                if (!request.PaymentSuccessful)
                {
                    var orderDetails = await _unitOfWork.OrderDetail.GetAllAsync(od => od.OrderId == request.OrderId, "Product");
                    foreach (var item in orderDetails)
                    {
                        if (item.Product != null)
                        {
                            item.Product.StockQuantity += item.Count;
                            await _unitOfWork.ProductRepository.UpdateAsync(item.Product);
                        }
                    }
                }

                await _unitOfWork.OrderHeader.UpdateAsync(orderHeader);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Order {OrderId} completed with status: {PaymentSuccessful}", 
                    request.OrderId, request.PaymentSuccessful);

                return Ok(new 
                { 
                    success = true, 
                    message = "Order updated successfully",
                    orderId = orderHeader.Id,
                    status = orderHeader.OrderStatus
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing order {OrderId}", request.OrderId);
                return StatusCode((int)HttpStatusCode.InternalServerError, 
                    new { success = false, message = "An error occurred while completing the order" });
            }
        }
    }

    public class OrderCompletionRequest
    {
        public int OrderId { get; set; }
        public bool PaymentSuccessful { get; set; }
        public string? TransactionId { get; set; }
    }
} 