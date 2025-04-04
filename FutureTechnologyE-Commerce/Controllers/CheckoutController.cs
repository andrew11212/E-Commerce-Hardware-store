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

			// Set payment method and status based on selection
			cartViewModel.OrderHeader.PaymentMethod = cartViewModel.SelectedPaymentMethod;
			
			if (cartViewModel.SelectedPaymentMethod == SD.Payment_Method_COD)
			{
				cartViewModel.OrderHeader.PaymentStatus = SD.Payment_Status_Pending;
				cartViewModel.OrderHeader.OrderStatus = SD.Status_Pending;
			}
			else
			{
				cartViewModel.OrderHeader.PaymentStatus = SD.Payment_Status_Pending;
				cartViewModel.OrderHeader.OrderStatus = SD.Status_Pending;
			}

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
			await _unitOfWork.SaveAsync();

			// Clear shopping cart
			await _unitOfWork.CartRepositery.RemoveRangeAsync(cartViewModel.CartList);
			await _unitOfWork.SaveAsync();

			// Redirect based on payment method
			if (cartViewModel.SelectedPaymentMethod == SD.Payment_Method_COD)
			{
				return RedirectToAction("OrderConfirmation", "Checkout", new { id = cartViewModel.OrderHeader.Id });
			}
			else
			{
				// Redirect to payment page for online payment
				return RedirectToAction("PaymentInit", "Payment", new { orderId = cartViewModel.OrderHeader.Id });
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

			_logger.LogInformation("Order {OrderId} confirmed", id);
			return View(id);
		}
	}
}