using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Models.ViewModels;
using FutureTechnologyE_Commerce.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

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

		public async Task<IActionResult> Index( string searchString = "")
		{
			var query = _unitOfWork.ProductRepository.GetQueryable(includeProperties: "Category,Brand,ProductType");

			if (!string.IsNullOrEmpty(searchString))
			{
				searchString = searchString.Trim().ToLower();
				query = query.Where(p => p.Name.ToLower().Contains(searchString) ||
										 (p.Brand != null && p.Brand.Name.ToLower().Contains(searchString)));
			}


			var viewModel = new HomeIndexViewModel
			{
				Products = (await _unitOfWork.ProductRepository.GetAllAsync(c=>c.IsBestseller, includeProperties: "Category,Brand,ProductType")), // Use the paginated list
				SearchString = searchString,
				Laptops = (await _unitOfWork.LaptopRepository.GetAllAsync(null, includeProperties: "Category,Brand,ProductType"))
					.Take(5)
					.ToList()
			};

			return View(viewModel);
		}

		public async Task<IActionResult> Details(int id)
		{
			var product = await _unitOfWork.ProductRepository.GetAsync(
				p => p.ProductID == id,
				"Category",
				"Brand",
				"ProductType"
			);

			if (product == null)
			{
				return NotFound();
			}

			var relatedProducts = (await _unitOfWork.ProductRepository
				.GetAllAsync(p => p.CategoryID == product.CategoryID &&
									 p.ProductID != product.ProductID,
									 includeProperties: "Category"))
				.Take(4)
				.ToList();

			ViewBag.RelatedProducts = relatedProducts;

			return View(product);
		}

		public async Task<IActionResult> GetAllProducts(int pageNumber = 1, string searchString = "")
		{
			var query = _unitOfWork.ProductRepository.GetQueryable(includeProperties: "Category,Brand,ProductType");

			if (!string.IsNullOrEmpty(searchString))
			{
				searchString = searchString.Trim().ToLower();
				query = query.Where(c => c.Brand.Name.ToLower().Contains(searchString) || c.Name.ToLower().Contains(searchString));
			}

			int pageSize = 4;
			int totalCount = await query.CountAsync();
			var products = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var viewModel = new HomeIndexViewModel
			{
				SearchString = searchString,
				Products = products,
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalCount = totalCount,
			};
			return View(viewModel);
		}

		public async Task<IActionResult> GetAllLaptops(int pageNumber = 1, string searchString = "")
		{
			var query = _unitOfWork.LaptopRepository.GetQueryable(includeProperties: "Category,Brand,ProductType");

			if (!string.IsNullOrEmpty(searchString))
			{
				searchString = searchString.Trim().ToLower();
				query = query.Where(c => c.Brand.Name.ToLower().Contains(searchString) || c.Name.ToLower().Contains(searchString));
			}

			int pageSize = 4;
			int totalCount = await query.CountAsync();
			var laptops = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var viewModel = new HomeIndexViewModel
			{
				SearchString = searchString,
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalCount = totalCount,
				Laptops = laptops,
			};
			return View(viewModel);
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[AllowAnonymous]
		public IActionResult Error()
		{
			return View();
		}
	}
}