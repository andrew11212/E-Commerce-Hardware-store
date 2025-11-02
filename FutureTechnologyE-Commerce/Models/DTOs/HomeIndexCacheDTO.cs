using System.Collections.Generic;

namespace FutureTechnologyE_Commerce.Models.DTOs
{
    /// <summary>
    /// Data Transfer Object for caching home page data to avoid circular references
    /// </summary>
    public class HomeIndexCacheDTO
    {
        public List<ProductCacheDTO> Products { get; set; } = new();
        public List<ProductCacheDTO> Accessories { get; set; } = new();
        public List<ProductCacheDTO> Laptops { get; set; } = new();
        public List<ReviewCacheDTO> TopReviews { get; set; } = new();
        public List<PromotionCacheDTO> Promotions { get; set; } = new();
        public string SearchString { get; set; } = "";
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 9;
        public int TotalCount { get; set; } = 0;
        public string Category { get; set; } = "";
        public List<string> CategoryOptions { get; set; } = new();
    }

    public class ProductCacheDTO
    {
        public int ProductID { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = "";
        public int StockQuantity { get; set; }
        public bool IsBestseller { get; set; }
        public string CategoryName { get; set; } = "";
        public string BrandName { get; set; } = "";
        public string? Processor { get; set; }
        public string? RAM { get; set; }
        public string? Storage { get; set; }
        public string? GraphicsCard { get; set; }
        public string? ScreenSize { get; set; }
        public string Discriminator { get; set; } = ""; // "Product" or "Laptop"
    }

    public class ReviewCacheDTO
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string UserName { get; set; } = "";
        public int Rating { get; set; }
        public string Comment { get; set; } = "";
        public DateTime ReviewDate { get; set; }
    }

    public class PromotionCacheDTO
    {
        public int PromotionId { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public ProductCacheDTO? Product { get; set; }
    }
}
