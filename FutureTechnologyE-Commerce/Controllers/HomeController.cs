using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace FutureTechnologyE_Commerce.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly IUnitOfWork unitOfWork;

		public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
		{
			_logger = logger;
			this.unitOfWork = unitOfWork;
		}

		public IActionResult Index()
		{
			var productList = unitOfWork.ProductRepository.GetAll(default, "Category").ToList();
			return View(productList);
		}
		public IActionResult Details(int id)
		{
			ShopingCart cart = new()
			{
				Product = unitOfWork.ProductRepository.Get(p => p.ProductID == id, "Category"),
				Count = 1,
				ProductId = id

			};
			return View(cart);
		}
		[Authorize]
		[HttpPost]
		public IActionResult Details([Bind("ProductId", "Count")] ShopingCart shopingCart)
		{
			if (!ModelState.IsValid)
			{
				// If model state is invalid, return the same view to show validation errors
				return View(shopingCart);
			}

			var claimsIdentity = User.Identity as ClaimsIdentity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			if (userId == null)
			{
				return Unauthorized();
			}

			shopingCart.ApplicationUserId = userId;

			var cartFromdb = unitOfWork.CartRepositery
				.Get(s => s.ApplicationUserId == userId && s.ProductId == shopingCart.ProductId);

			if (cartFromdb != null)
			{
				cartFromdb.Count += shopingCart.Count;
				unitOfWork.CartRepositery.Update(cartFromdb); // Update the existing entity, not shopingCart
			}
			else
			{
				unitOfWork.CartRepositery.Add(shopingCart);
			}

			unitOfWork.Save();

			return RedirectToAction("Index");
		}


		public IActionResult Privacy()
		{
			return View();
		}

		
	}


}
