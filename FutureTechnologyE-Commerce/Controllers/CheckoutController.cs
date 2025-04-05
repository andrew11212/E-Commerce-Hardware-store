using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepository;
using FutureTechnologyE_Commerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using FutureTechnologyE_Commerce.Models.ViewModels;
using System.Text.Json;

namespace FutureTechnologyE_Commerce.Controllers
{
	[Authorize]
	[EnableRateLimiting("fixed")]
	public class CheckoutController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<CheckoutController> _logger;

		[BindProperty]
		public CartViewModel CartVM { get; set; } = new CartViewModel();

		public CheckoutController(
			IUnitOfWork unitOfWork,
			ILogger<CheckoutController> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		public async Task<IActionResult> Index()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity!;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)!.Value;

			CartVM.CartList = await _unitOfWork.CartRepositery.GetAllAsync(c => c.ApplicationUserId == userId,
				includeProperties: "Product");

			if (!CartVM.CartList.Any())
			{
				return RedirectToAction("Index", "Cart");
			}

			CartVM.OrderHeader = new()
			{
				ApplicationUserId = userId,
				OrderTotal = Convert.ToDouble(CartVM.CartList.Sum(c => c.Count * c.Product.Price))
			};

			var user = await _unitOfWork.applciationUserRepository.GetAsync(u => u.Id == userId);
			if (user != null)
			{
				CartVM.OrderHeader.first_name = user.first_name;
				CartVM.OrderHeader.last_name = user.last_name;
				CartVM.OrderHeader.email = user.Email;
				CartVM.OrderHeader.phone_number = user.PhoneNumber ?? string.Empty;
				
				// If user has saved address information, use it
				if (!string.IsNullOrEmpty(user.street))
				{
					CartVM.OrderHeader.street = user.street;
					CartVM.OrderHeader.building = user.building;
					CartVM.OrderHeader.apartment = user.apartment;
					CartVM.OrderHeader.floor = user.floor;
					CartVM.OrderHeader.state = user.state;
					CartVM.OrderHeader.country = user.country;
				}
			}

			return View(CartVM);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Index(CartViewModel cartViewModel)
		{
			try
			{
				var claimsIdentity = (ClaimsIdentity)User.Identity!;
				var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)!.Value;

				cartViewModel.CartList = await _unitOfWork.CartRepositery.GetAllAsync(c => c.ApplicationUserId == userId,
					includeProperties: "Product");

				cartViewModel.OrderHeader.OrderDate = DateTime.Now;
				cartViewModel.OrderHeader.ApplicationUserId = userId;
				cartViewModel.OrderHeader.OrderTotal = Convert.ToDouble(cartViewModel.CartList.Sum(c => c.Count * c.Product.Price));

				// Validate model state
				if (!ModelState.IsValid)
				{
					return View(cartViewModel);
				}

				// Validate payment method
				if (string.IsNullOrEmpty(cartViewModel.SelectedPaymentMethod))
				{
					ModelState.AddModelError("SelectedPaymentMethod", "Please select a payment method");
					return View(cartViewModel);
				}

				// Set payment method based on selection
				cartViewModel.OrderHeader.PaymentMethod = cartViewModel.SelectedPaymentMethod;
				
				if (cartViewModel.SelectedPaymentMethod == SD.Payment_Method_COD)
				{
					// For COD, proceed to create the order
					using (var transaction = _unitOfWork.BeginTransaction())
					{
						try
						{
							// Set initial order status
							cartViewModel.OrderHeader.PaymentStatus = SD.Payment_Status_Pending;
							cartViewModel.OrderHeader.OrderStatus = SD.Status_Pending;
							
							// Create order header
							await _unitOfWork.OrderHeader.AddAsync(cartViewModel.OrderHeader);
							await _unitOfWork.SaveAsync();

							// Create order details for each cart item
							foreach (var cart in cartViewModel.CartList)
							{
								OrderDetail orderDetail = new()
								{
									ProductId = cart.ProductId,
									OrderId = cartViewModel.OrderHeader.Id,
									Price = Convert.ToDouble(cart.Product.Price),
									Count = cart.Count
								};
								await _unitOfWork.OrderDetail.AddAsync(orderDetail);
							}

							// Clear shopping cart
							await _unitOfWork.CartRepositery.RemoveRangeAsync(cartViewModel.CartList);
							await _unitOfWork.SaveAsync();

							// Commit the transaction for COD
							transaction.Commit();
							
							_logger.LogInformation("Cash on delivery order created successfully: {OrderId}", cartViewModel.OrderHeader.Id);
							return RedirectToAction("OrderConfirmation", "Checkout", new { id = cartViewModel.OrderHeader.Id });
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Error processing COD order");
							// Transaction will automatically roll back if we don't commit
							ModelState.AddModelError("", "Error processing your order. Please try again.");
							return View(cartViewModel);
						}
					}
				}
				else
				{
					// For online payment, store the checkout data in session
					// and redirect to payment without creating an order first
					
					// 1. Store the checkout information in session for retrieval after payment
					var checkoutData = new {
						UserInfo = new {
							UserId = userId,
							FirstName = cartViewModel.OrderHeader.first_name,
							LastName = cartViewModel.OrderHeader.last_name,
							Email = cartViewModel.OrderHeader.email,
							PhoneNumber = cartViewModel.OrderHeader.phone_number,
							Street = cartViewModel.OrderHeader.street,
							Building = cartViewModel.OrderHeader.building,
							Apartment = cartViewModel.OrderHeader.apartment,
							Floor = cartViewModel.OrderHeader.floor,
							State = cartViewModel.OrderHeader.state,
							Country = cartViewModel.OrderHeader.country
						},
						OrderTotal = cartViewModel.OrderHeader.OrderTotal,
						PaymentMethod = cartViewModel.SelectedPaymentMethod,
						CartItems = cartViewModel.CartList.Select(c => new {
							ProductId = c.ProductId,
							Count = c.Count,
							Price = c.Product.Price
						}).ToList()
					};
					
					// 2. Store checkout data in session
					HttpContext.Session.SetString("CheckoutData", JsonSerializer.Serialize(checkoutData));
					
					// 3. Redirect to payment initialization
					_logger.LogInformation("Online payment checkout data stored in session. Redirecting to payment.");
					return RedirectToAction("InitializePaymentOnly", "Payment");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error in checkout process");
				ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
				return View(cartViewModel);
			}
		}

		public async Task<IActionResult> OrderConfirmation(int id)
		{
			var orderHeader = await _unitOfWork.OrderHeader.GetAsync(o => o.Id == id);
			if (orderHeader == null)
			{
				return NotFound();
			}

			// For COD orders, update status to show approval
			if (orderHeader.PaymentMethod == SD.Payment_Method_COD)
			{
				orderHeader.OrderStatus = SD.Status_Approved;
				await _unitOfWork.SaveAsync();
			}

			// Create OrderDetailsViewModel to match what the view expects
			var orderDetails = await _unitOfWork.OrderDetail.GetAllAsync(d => d.OrderId == id, includeProperties: "Product");
			
			var orderVM = new OrderDetailsViewModel
			{
				OrderHeader = orderHeader,
				OrderDetails = orderDetails.ToList()
			};

			_logger.LogInformation("Order {OrderId} confirmed", id);
			return View(orderVM);
		}
		
		[HttpGet]
		public async Task<IActionResult> CreateOrderAfterPayment(string transactionId, int paymobOrderId)
		{
			try
			{
				// 1. Retrieve checkout data from session
				var checkoutDataJson = HttpContext.Session.GetString("CheckoutData");
				if (string.IsNullOrEmpty(checkoutDataJson))
				{
					_logger.LogError("Checkout data not found in session");
					return RedirectToAction("Error", "Home", new { message = "Checkout information not found. Please try again." });
				}
				
				// 2. Deserialize checkout data
				var checkoutData = JsonSerializer.Deserialize<JsonElement>(checkoutDataJson);
				var userId = checkoutData.GetProperty("UserInfo").GetProperty("UserId").GetString();
				
				// 3. Begin transaction for creating order
				using (var transaction = await _unitOfWork.BeginTransactionAsync())
				{
					try
					{
						// 4. Create order header
						var orderHeader = new OrderHeader
						{
							ApplicationUserId = userId,
							OrderDate = DateTime.Now,
							OrderTotal = checkoutData.GetProperty("OrderTotal").GetDouble(),
							PaymentMethod = checkoutData.GetProperty("PaymentMethod").GetString(),
							PaymentStatus = SD.Payment_Status_Approved,
							OrderStatus = SD.Status_Approved,
							PaymentDate = DateTime.Now,
							PaymobOrderId = paymobOrderId,
							TransactionId = transactionId,
							
							// Address information
							first_name = checkoutData.GetProperty("UserInfo").GetProperty("FirstName").GetString(),
							last_name = checkoutData.GetProperty("UserInfo").GetProperty("LastName").GetString(),
							email = checkoutData.GetProperty("UserInfo").GetProperty("Email").GetString(),
							phone_number = checkoutData.GetProperty("UserInfo").GetProperty("PhoneNumber").GetString(),
							street = checkoutData.GetProperty("UserInfo").GetProperty("Street").GetString(),
							building = checkoutData.GetProperty("UserInfo").GetProperty("Building").GetString(),
							apartment = checkoutData.GetProperty("UserInfo").GetProperty("Apartment").GetString(),
							floor = checkoutData.GetProperty("UserInfo").GetProperty("Floor").GetString(),
							state = checkoutData.GetProperty("UserInfo").GetProperty("State").GetString(),
							country = checkoutData.GetProperty("UserInfo").GetProperty("Country").GetString()
						};
						
						await _unitOfWork.OrderHeader.AddAsync(orderHeader);
						await _unitOfWork.SaveAsync();
						
						// 5. Create order details for each cart item
						var cartItems = checkoutData.GetProperty("CartItems");
						foreach (var item in cartItems.EnumerateArray())
						{
							var orderDetail = new OrderDetail
							{
								OrderId = orderHeader.Id,
								ProductId = item.GetProperty("ProductId").GetInt32(),
								Count = item.GetProperty("Count").GetInt32(),
								Price = item.GetProperty("Price").GetDouble()
							};
							
							await _unitOfWork.OrderDetail.AddAsync(orderDetail);
						}
						
						// 6. Get cart items and remove them
						var userCartItems = await _unitOfWork.CartRepositery.GetAllAsync(c => c.ApplicationUserId == userId);
						await _unitOfWork.CartRepositery.RemoveRangeAsync(userCartItems);
						await _unitOfWork.SaveAsync();
						
						// 7. Commit the transaction
						await _unitOfWork.CommitTransactionAsync(transaction);
						
						// 8. Clear checkout data from session
						HttpContext.Session.Remove("CheckoutData");
						
						_logger.LogInformation("Order successfully created after payment: {OrderId}", orderHeader.Id);
						return RedirectToAction("OrderConfirmation", new { id = orderHeader.Id });
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error creating order after payment");
						await _unitOfWork.RollbackTransactionAsync(transaction);
						return RedirectToAction("Error", "Home", new { message = "Failed to create order after payment. Please contact support." });
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error in CreateOrderAfterPayment");
				return RedirectToAction("Error", "Home", new { message = "An unexpected error occurred. Please contact support." });
			}
		}
	}
}