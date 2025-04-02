using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Utility;
using FutureTechnologyE_Commerce.Repository.IRepository;
using FutureTechnologyE_Commerce.Models.ViewModels;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace FutureTechnologyE_Commerce.Controllers
{
	[Authorize(Roles = SD.Role_Admin)]
	public class LaptopsController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IWebHostEnvironment _webHostEnvironment;
		private readonly ILogger<LaptopsController> _logger;

		public LaptopsController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, ILogger<LaptopsController> logger)
		{
			_unitOfWork = unitOfWork;
			_webHostEnvironment = webHostEnvironment;
			_logger = logger;
		}

		// GET: Laptops
		public async Task<IActionResult> Index()
		{
			try
			{
				var laptops = await _unitOfWork.LaptopRepository.GetAllAsync();
				return View(laptops.ToList());
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Laptops Index action.");
				return View("Error");
			}
		}

		// GET: Laptops/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			try
			{
				if (id == null)
				{
					return NotFound();
				}

				var laptop = await _unitOfWork.LaptopRepository.GetAsync(l => l.ProductID == id, "Category", "Brand");
				if (laptop == null)
				{
					return NotFound();
				}

				return View(laptop);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Laptops Details action with id: {id}.", id);
				return View("Error");
			}
		}

		// GET: Laptops/Create
		public async Task<IActionResult> Create()
		{
			LaptopViewModel laptopVM = new()
			{
				Laptop = new Laptop(),
			};
			try
			{
				await PopulateDropdowns(laptopVM);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error populating dropdowns in Laptops Create action.");
				ModelState.AddModelError("", "Unable to load dropdown data. Please try again later.");
			}
			return View(laptopVM);
		}

		// POST: Laptops/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(LaptopViewModel laptopVM, IFormFile? file)
		{
			try
			{
				if (ModelState.IsValid)
				{
					if (file != null && file.Length > 0)
					{
						if (!IsValidImage(file))
						{
							ModelState.AddModelError("file", "Please upload a valid image file (e.g., .jpg, .png) under 5MB.");
							await PopulateDropdowns(laptopVM);
							return View(laptopVM);
						}

						string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
						string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products", fileName);

						using (var fileStream = new FileStream(imagePath, FileMode.Create))
						{
							await file.CopyToAsync(fileStream);
						}

						laptopVM.Laptop.ImageUrl = "/images/products/" + fileName;
					}

					await _unitOfWork.LaptopRepository.AddAsync(laptopVM.Laptop);
					await _unitOfWork.SaveAsync();
					TempData["Success"] = "Laptop created successfully.";
					return RedirectToAction(nameof(Index));
				}

				await PopulateDropdowns(laptopVM);
				return View(laptopVM);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Laptops Create POST action.");
				TempData["Error"] = "An error occurred while creating the laptop.";
				await PopulateDropdowns(laptopVM);
				return View(laptopVM);
			}
		}

		// GET: Laptops/Edit/5
		[HttpGet]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			LaptopViewModel laptopVM = null;
			try
			{
				var laptop = await _unitOfWork.LaptopRepository.GetAsync(l => l.ProductID == id, "Category", "Brand");
				if (laptop == null)
				{
					return NotFound();
				}

				laptopVM = new LaptopViewModel
				{
					Laptop = laptop,
				};

				await PopulateDropdowns(laptopVM);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Laptops Edit action with id: {id}.", id);
				if (laptopVM == null)
				{
					TempData["Error"] = "An error occurred while loading the laptop for editing.";
					return RedirectToAction(nameof(Index));
				}
				else
				{
					ModelState.AddModelError("", "Unable to load dropdown data. Please try again later.");
				}
			}

			return View(laptopVM);
		}

		// POST: Laptops/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, LaptopViewModel laptopVM, IFormFile? file)
		{
			try
			{
				if (id != laptopVM.Laptop.ProductID)
				{
					return NotFound();
				}

				if (ModelState.IsValid)
				{
					if (file != null && file.Length > 0)
					{
						if (!IsValidImage(file))
						{
							ModelState.AddModelError("file", "Please upload a valid image file (e.g., .jpg, .png) under 5MB.");
							await PopulateDropdowns(laptopVM);
							return View(laptopVM);
						}

						string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
						string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products", fileName);

						if (!string.IsNullOrEmpty(laptopVM.Laptop.ImageUrl))
						{
							string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, laptopVM.Laptop.ImageUrl.TrimStart('/'));
							if (System.IO.File.Exists(oldImagePath))
							{
								System.IO.File.Delete(oldImagePath);
							}
						}

						using (var fileStream = new FileStream(imagePath, FileMode.Create))
						{
							await file.CopyToAsync(fileStream);
						}

						laptopVM.Laptop.ImageUrl = "/images/products/" + fileName;
					}

					await _unitOfWork.LaptopRepository.UpdateAsync(laptopVM.Laptop);
					await _unitOfWork.SaveAsync();
					TempData["Success"] = "Laptop updated successfully.";
					return RedirectToAction(nameof(Index));
				}

				await PopulateDropdowns(laptopVM);
				return View(laptopVM);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Laptops Edit POST action with id: {id}.", id);
				TempData["Error"] = "An error occurred while updating the laptop.";
				await PopulateDropdowns(laptopVM);
				return View(laptopVM);
			}
		}

		// GET: Laptops/Delete/5
		public async Task<IActionResult> Delete(int? id)
		{
			try
			{
				if (id == null)
				{
					return NotFound();
				}

				var laptop = await _unitOfWork.LaptopRepository.GetAsync(l => l.ProductID == id, "Category", "Brand");
				if (laptop == null)
				{
					return NotFound();
				}

				return View(laptop);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Laptops Delete action with id: {id}.", id);
				return View("Error");
			}
		}

		// POST: Laptops/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			try
			{
				var laptop = await _unitOfWork.LaptopRepository.GetAsync(l => l.ProductID == id);
				if (laptop != null)
				{
					if (!string.IsNullOrEmpty(laptop.ImageUrl))
					{
						string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, laptop.ImageUrl.TrimStart('/'));
						if (System.IO.File.Exists(imagePath))
						{
							System.IO.File.Delete(imagePath);
						}
					}
					await _unitOfWork.LaptopRepository.RemoveAsync(laptop);
					await _unitOfWork.SaveAsync();
					TempData["Success"] = "Laptop deleted successfully.";
				}
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Laptops DeleteConfirmed action with id: {id}.", id);
				TempData["Error"] = "An error occurred while deleting the laptop.";
				return RedirectToAction(nameof(Index));
			}
		}

		// Helper method to populate dropdowns
		private async Task PopulateDropdowns(LaptopViewModel laptopVM)
		{
			laptopVM.CategoryList = (await _unitOfWork.CategoryRepository.GetAllAsync()).Select(c =>
				new SelectListItem { Text = c.Name, Value = c.CategoryID.ToString() }).ToList();
			laptopVM.BrandList = (await _unitOfWork.BrandRepository.GetAllAsync()).Select(b =>
				new SelectListItem { Text = b.Name, Value = b.BrandID.ToString() }).ToList();
		}

		// Helper method for file validation (example implementation)
		private bool IsValidImage(IFormFile file)
		{
			var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
			var maxSizeInBytes = 5 * 1024 * 1024; // 5MB

			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
			return allowedExtensions.Contains(extension) && file.Length <= maxSizeInBytes;
		}
	}
}