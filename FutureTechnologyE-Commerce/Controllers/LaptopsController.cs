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
using Microsoft.Extensions.Logging; // Add logging
using System.Linq; // Make sure this is included

namespace FutureTechnologyE_Commerce.Controllers
{
	[Authorize(Roles = SD.Role_Admin)]
	public class LaptopsController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IWebHostEnvironment _webHostEnvironment;
		private readonly ILogger<LaptopsController> _logger; // Add logger

		public LaptopsController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, ILogger<LaptopsController> logger) // Inject logger
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
				var laptops = await _unitOfWork.LaptopRepository.GetAllAsync(includeProperties: "Category,Brand,ProductType");
				return View(laptops.ToList());
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Laptops Index action.");
				return View("Error"); // Return an error view
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

				var laptop = await _unitOfWork.LaptopRepository.GetAsync(l => l.ProductID == id, includeProperties: "Brand,Category,ProductType");
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
			try
			{
				LaptopViewModel laptopVM = new()
				{
					Laptop = new Laptop(),
					CategoryList = (await _unitOfWork.CategoryRepository.GetAllAsync()).Select(c =>
						new SelectListItem { Text = c.Name, Value = c.CategoryID.ToString() }).ToList(),
					BrandList = (await _unitOfWork.BrandRepository.GetAllAsync()).Select(b =>
						new SelectListItem { Text = b.Name, Value = b.BrandID.ToString() }).ToList(),
					ProductTypeList = (await _unitOfWork.ProductTypeRepository.GetAllAsync()).Select(pt =>
						new SelectListItem { Text = pt.Name, Value = pt.ProductTypeID.ToString() }).ToList()
				};
				return View(laptopVM);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Laptops Create action.");
				return View("Error");
			}
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

					return RedirectToAction(nameof(Index));
				}

				laptopVM.CategoryList = (await _unitOfWork.CategoryRepository.GetAllAsync()).Select(c =>
						new SelectListItem { Text = c.Name, Value = c.CategoryID.ToString() }).ToList();
				laptopVM.BrandList = (await _unitOfWork.BrandRepository.GetAllAsync()).Select(b =>
					new SelectListItem { Text = b.Name, Value = b.BrandID.ToString() }).ToList();
				laptopVM.ProductTypeList = (await _unitOfWork.ProductTypeRepository.GetAllAsync()).Select(pt =>
					new SelectListItem { Text = pt.Name, Value = pt.ProductTypeID.ToString() }).ToList();

				return View(laptopVM); // Return the ViewModel, not just Laptop
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Laptops Create POST action.");
				return View("Error");
			}
		}

		// GET: Laptops/Edit/5
		public async Task<IActionResult> Edit(int? id)
		{
			try
			{
				if (id == null)
				{
					return NotFound();
				}

				var laptop = await _unitOfWork.LaptopRepository.GetAsync(l => l.ProductID == id);
				if (laptop == null)
				{
					return NotFound();
				}
				LaptopViewModel laptopVM = new()
				{
					Laptop = laptop,
					CategoryList = (await _unitOfWork.CategoryRepository.GetAllAsync()).Select(c =>
						new SelectListItem { Text = c.Name, Value = c.CategoryID.ToString() }).ToList(),
					BrandList = (await _unitOfWork.BrandRepository.GetAllAsync()).Select(b =>
						new SelectListItem { Text = b.Name, Value = b.BrandID.ToString() }).ToList(),
					ProductTypeList = (await _unitOfWork.ProductTypeRepository.GetAllAsync()).Select(pt =>
						new SelectListItem { Text = pt.Name, Value = pt.ProductTypeID.ToString() }).ToList()
				};

				return View(laptopVM);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Laptops Edit action with id: {id}.", id);
				return View("Error");
			}
		}

		// POST: Laptops/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, LaptopViewModel laptopVM, IFormFile file)
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
					return RedirectToAction(nameof(Index));
				}

				laptopVM.CategoryList = (await _unitOfWork.CategoryRepository.GetAllAsync()).Select(c =>
						new SelectListItem { Text = c.Name, Value = c.CategoryID.ToString() }).ToList();
				laptopVM.BrandList = (await _unitOfWork.BrandRepository.GetAllAsync()).Select(b =>
					new SelectListItem { Text = b.Name, Value = b.BrandID.ToString() }).ToList();
				laptopVM.ProductTypeList = (await _unitOfWork.ProductTypeRepository.GetAllAsync()).Select(pt =>
					new SelectListItem { Text = pt.Name, Value = pt.ProductTypeID.ToString() }).ToList();

				return View(laptopVM);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Laptops Edit POST action with id: {id}.", id);
				return View("Error");
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

				var laptop = await _unitOfWork.LaptopRepository.GetAsync(l => l.ProductID == id, includeProperties: "Brand,Category,ProductType");
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
				}
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Laptops DeleteConfirmed action with id: {id}.", id);
				return View("Error");
			}
		}
	}
}