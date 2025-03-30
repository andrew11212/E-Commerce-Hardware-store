using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Models.ViewModels;
using FutureTechnologyE_Commerce.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FutureTechnologyE_Commerce.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

         public IActionResult Index()
        {
            var viewModel = new HomeIndexViewModel
            {
                // Get all products, including laptops
                Products = _unitOfWork.ProductRepository.GetAll(null,includeProperties: "Category,Brand,ProductType").ToList(),

                // Get only Laptops.  This assumes you have a way to filter for laptops, such as by ProductType or Category.
                Laptops = _unitOfWork.LaptopRepository.GetAll(null,includeProperties: "Category,Brand,ProductType").ToList(),
            };
            return View(viewModel);
        }

        public IActionResult Details(int id)
        {
            var product = _unitOfWork.ProductRepository.Get(
         p => p.ProductID == id,
             "Category",  // Separate navigation properties
             "Brand",
            "ProductType"
            );

            if (product == null)
            {
                return NotFound();
            }

            // Get related products
            var relatedProducts = _unitOfWork.ProductRepository
                .GetAll(p => p.CategoryID == product.CategoryID &&
                             p.ProductID != product.ProductID,
                        includeProperties: "Category")
                .Take(4)
                .ToList();

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int productId, int quantity)
        {
            // Input validation
            if (quantity <= 0)
            {
                TempData["error"] = "Quantity must be at least 1";
                return RedirectToAction(nameof(Details), new { id = productId });
            }

            var product = _unitOfWork.ProductRepository.Get(p => p.ProductID == productId);
            if (product == null)
            {
                TempData["error"] = "Product not found";
                return RedirectToAction(nameof(Index));
            }

            // Authentication check
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["error"] = "Please login to add items to cart";
                return RedirectToAction("Login", "Account");
            }

            // Cart operations
            var cartItem = _unitOfWork.CartRepositery
                .Get(c => c.ApplicationUserId == userId && c.ProductId == productId);

            if (cartItem != null)
            {
                cartItem.Count += quantity;
                _unitOfWork.CartRepositery.Update(cartItem);
            }
            else
            {
                var newCartItem = new ShopingCart
                {
                    ApplicationUserId = userId,
                    ProductId = productId,
                    Count = quantity
                };
                _unitOfWork.CartRepositery.Add(newCartItem);
            }

            _unitOfWork.Save();
            TempData["success"] = "Item added to cart successfully";
            return RedirectToAction(nameof(Details), new { id = productId });
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}