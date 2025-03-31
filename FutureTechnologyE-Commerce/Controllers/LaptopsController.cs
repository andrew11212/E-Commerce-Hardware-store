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
		public IActionResult Index()
		{
			try
			{
				var laptops = _unitOfWork.LaptopRepository.GetAll(includeProperties: "Category,Brand,ProductType");
				return View(laptops);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Laptops Index action.");
				return View("Error"); // Return an error view
			}
		}

		// GET: Laptops/Details/5
		public IActionResult Details(int? id)
		{
			try
			{
				if (id == null)
				{
					return NotFound();
				}

				var laptop = _unitOfWork.LaptopRepository.Get(l => l.ProductID == id, includeProperties: "Brand,Category,ProductType");
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
		public IActionResult Create()
		{
			try
			{
				LaptopViewModel LabtobVM = new()
				{
					Laptop = new Laptop(),
					CategoryList = _unitOfWork.CategoryRepository.GetAll().Select(c =>
						new SelectListItem { Text = c.Name, Value = c.CategoryID.ToString() }).ToList(),
					BrandList = _unitOfWork.BrandRepository.GetAll().Select(b =>
						new SelectListItem { Text = b.Name, Value = b.BrandID.ToString() }).ToList(),
					ProductTypeList = _unitOfWork.ProductTypeRepository.GetAll().Select(pt =>
						new SelectListItem { Text = pt.Name, Value = pt.ProductTypeID.ToString() }).ToList()
				};
				return View(LabtobVM);
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
		public async Task<IActionResult> Create(LaptopViewModel laptopVm, IFormFile? file)
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

						laptopVm.Laptop.ImageUrl = "/images/products/" + fileName;
					}

					_unitOfWork.LaptopRepository.Add(laptopVm.Laptop);
					_unitOfWork.Save();

					return RedirectToAction(nameof(Index));
				}

				laptopVm.CategoryList = _unitOfWork.CategoryRepository.GetAll().Select(c =>
						new SelectListItem { Text = c.Name, Value = c.CategoryID.ToString() }).ToList();
				laptopVm.BrandList = _unitOfWork.BrandRepository.GetAll().Select(b =>
					new SelectListItem { Text = b.Name, Value = b.BrandID.ToString() }).ToList();
				laptopVm.ProductTypeList = _unitOfWork.ProductTypeRepository.GetAll().Select(pt =>
					new SelectListItem { Text = pt.Name, Value = pt.ProductTypeID.ToString() }).ToList();

				return View(laptopVm); // Return the ViewModel, not just Laptop
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Laptops Create POST action.");
				return View("Error");
			}
		}

		// GET: Laptops/Edit/5
		public IActionResult Edit(int? id)
		{
			try
			{
				if (id == null)
				{
					return NotFound();
				}

				var laptop = _unitOfWork.LaptopRepository.Get(l => l.ProductID == id);
				if (laptop == null)
				{
					return NotFound();
				}
				LaptopViewModel LabtobVM = new()
				{
					Laptop = laptop,
					CategoryList = _unitOfWork.CategoryRepository.GetAll().Select(c =>
						new SelectListItem { Text = c.Name, Value = c.CategoryID.ToString() }).ToList(),
					BrandList = _unitOfWork.BrandRepository.GetAll().Select(b =>
						new SelectListItem { Text = b.Name, Value = b.BrandID.ToString() }).ToList(),
					ProductTypeList = _unitOfWork.ProductTypeRepository.GetAll().Select(pt =>
						new SelectListItem { Text = pt.Name, Value = pt.ProductTypeID.ToString() }).ToList()
				};

				return View(LabtobVM);
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
		public async Task<IActionResult> Edit(int id, LaptopViewModel laptopVm, IFormFile file)
		{
			try
			{
				if (id != laptopVm.Laptop.ProductID)
				{
					return NotFound();
				}

				if (ModelState.IsValid)
				{
					if (file != null && file.Length > 0)
					{
						string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
						string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products", fileName);

						if (!string.IsNullOrEmpty(laptopVm.Laptop.ImageUrl))
						{
							string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, laptopVm.Laptop.ImageUrl.TrimStart('/'));
							if (System.IO.File.Exists(oldImagePath))
							{
								System.IO.File.Delete(oldImagePath);
							}
						}

						using (var fileStream = new FileStream(imagePath, FileMode.Create))
						{
							await file.CopyToAsync(fileStream);
						}

						laptopVm.Laptop.ImageUrl = "/images/products/" + fileName;
					}
					_unitOfWork.LaptopRepository.Update(laptopVm.Laptop);
					_unitOfWork.Save();
					return RedirectToAction(nameof(Index));
				}

				laptopVm.CategoryList = _unitOfWork.CategoryRepository.GetAll().Select(c =>
						new SelectListItem { Text = c.Name, Value = c.CategoryID.ToString() }).ToList();
				laptopVm.BrandList = _unitOfWork.BrandRepository.GetAll().Select(b =>
					new SelectListItem { Text = b.Name, Value = b.BrandID.ToString() }).ToList();
				laptopVm.ProductTypeList = _unitOfWork.ProductTypeRepository.GetAll().Select(pt =>
					new SelectListItem { Text = pt.Name, Value = pt.ProductTypeID.ToString() }).ToList();

				return View(laptopVm);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in Laptops Edit POST action with id: {id}.", id);
				return View("Error");
			}
		}

		// GET: Laptops/Delete/5
		public IActionResult Delete(int? id)
		{
			try
			{
				if (id == null)
				{
					return NotFound();
				}

				var laptop = _unitOfWork.LaptopRepository.Get(l => l.ProductID == id, includeProperties: "Brand,Category,ProductType");
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
		public IActionResult DeleteConfirmed(int id)
		{
			try
			{
				var laptop = _unitOfWork.LaptopRepository.Get(l => l.ProductID == id);
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
					_unitOfWork.LaptopRepository.Remove(laptop);
					_unitOfWork.Save();
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