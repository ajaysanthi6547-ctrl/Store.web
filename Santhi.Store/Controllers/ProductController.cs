
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Santhi.Store.Data;
using Santhi.Store.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Santhi.Store.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IMemoryCache _memoryCache;

        private const string ProductCacheKey = "Products_All";
        private const long MaxImageSize = 5 * 1024 * 1024;

        private static readonly string[] AllowedExtensions =
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public ProductController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            IMemoryCache memoryCache)
        {
            _context = context;
            _environment = environment;
            _memoryCache = memoryCache;
        }


        // Display products (Everyone can access)
        [AllowAnonymous]
        public async Task<IActionResult> Index(string? category)
        {
            var allProducts = await _memoryCache.GetOrCreateAsync(
                ProductCacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromMinutes(5);

                    return await _context.Products
                        .AsNoTracking()
                        .ToListAsync();
                });

            var products = allProducts.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                products = products.Where(p =>
                    string.Equals(
                        p.Category?.Trim(),
                        category.Trim(),
                        StringComparison.OrdinalIgnoreCase
                    ));
            }

            var productList = products.ToList();

            // Load ratings for displayed products
            var productIds = productList
                .Select(p => p.ProductId)
                .ToList();

            var ratingSummary = await _context.ProductReviews
                .AsNoTracking()
                .Where(r => productIds.Contains(r.ProductId))
                .GroupBy(r => r.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    AverageRating = g.Average(r => r.Rating),
                    ReviewCount = g.Count()
                })
                .ToListAsync();

            ViewBag.RatingSummary = ratingSummary
                .ToDictionary(r => r.ProductId);

            ViewBag.Categories = allProducts
                .Where(p => !string.IsNullOrEmpty(p.Category))
                .Select(p => p.Category)
                .Distinct()
                .ToList();

            ViewBag.SelectedCategory = category;

            return View(productList);
        }

        // GET: Product/Create (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Product/Create (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Product product, IFormFile? imageFile)
        {
            ModelState.Remove(nameof(Product.ImageUrl));

            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError(
                    "ImageUrl",
                    "Please select a product image.");
            }
            else if (imageFile.Length > MaxImageSize)
            {
                ModelState.AddModelError(
                    "ImageUrl",
                    "Image size must be below 5 MB.");
            }
            else
            {
                var extension = Path.GetExtension(
                    imageFile.FileName).ToLowerInvariant();

                if (!AllowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "ImageUrl",
                        "Only JPG, PNG and WEBP images are allowed.");
                }
                else
                {
                    bool validImage =
                        await HasValidImageSignatureAsync(
                            imageFile, extension);

                    if (!validImage)
                    {
                        ModelState.AddModelError(
                            "ImageUrl",
                            "Invalid image file. Please upload a valid image.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return View(product);
            }

            var folderPath = Path.Combine(
                _environment.WebRootPath, "images");

            Directory.CreateDirectory(folderPath);

            var fileName = Guid.NewGuid().ToString("N")
                + Path.GetExtension(imageFile!.FileName)
                    .ToLowerInvariant();

            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(
                filePath, FileMode.CreateNew))
            {
                await imageFile.CopyToAsync(stream);
            }

            product.ImageUrl = "/images/" + fileName;

            _context.Products.Add(product);

            await _context.SaveChangesAsync();

            // Clear cached products
            _memoryCache.Remove(ProductCacheKey);

            TempData["SuccessMessage"] =
                "Product added successfully!";

            return RedirectToAction("Index");
        }

        // Verify image file signature
        private static async Task<bool> HasValidImageSignatureAsync(
            IFormFile file, string extension)
        {
            byte[] header = new byte[12];

            using (var stream = file.OpenReadStream())
            {
                int bytesRead = await stream.ReadAsync(
                    header, 0, header.Length);

                if (extension == ".jpg" || extension == ".jpeg")
                {
                    return bytesRead >= 3 &&
                           header[0] == 0xFF &&
                           header[1] == 0xD8 &&
                           header[2] == 0xFF;
                }

                if (extension == ".png")
                {
                    byte[] pngSignature =
                    {
                        0x89, 0x50, 0x4E, 0x47,
                        0x0D, 0x0A, 0x1A, 0x0A
                    };

                    return bytesRead >= 8 &&
                           header.Take(8).SequenceEqual(
                               pngSignature);
                }

                if (extension == ".webp")
                {
                    return bytesRead >= 12 &&
                           header[0] == 'R' &&
                           header[1] == 'I' &&
                           header[2] == 'F' &&
                           header[3] == 'F' &&
                           header[8] == 'W' &&
                           header[9] == 'E' &&
                           header[10] == 'B' &&
                           header[11] == 'P';
                }
            }

            return false;
        }

        // GET: Product/Edit/5 (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }


        // POST: Product/Edit/5 (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product)
        {
            ModelState.Remove(nameof(Product.ImageUrl));

            if (!ModelState.IsValid)
            {
                return View(product);
            }

            var existingProduct = await _context.Products
                .FindAsync(product.ProductId);

            if (existingProduct == null)
            {
                return NotFound();
            }

            existingProduct.ProductName = product.ProductName;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.Category = product.Category;
            existingProduct.Stock = product.Stock;

            await _context.SaveChangesAsync();

            // Clear product list cache
            _memoryCache.Remove(ProductCacheKey);

            // Clear individual product details cache
            _memoryCache.Remove(
                $"ProductDetails_{existingProduct.ProductId}");

            TempData["SuccessMessage"] =
                "Product updated successfully!";

            return RedirectToAction("Index");
        }



        // GET: Product/Details/5
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            string cacheKey = $"ProductDetails_{id}";

            var product = await _memoryCache.GetOrCreateAsync(
                cacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromMinutes(5);

                    return await _context.Products
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.ProductId == id);
                });

            if (product == null)
            {
                return NotFound();
            }

            // Load customer reviews
            var reviews = await _context.ProductReviews
                .AsNoTracking()
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Reviews = reviews;

            ViewBag.AverageRating = reviews.Any()
                ? reviews.Average(r => r.Rating)
                : 0;

            return View(product);
        }


        // POST: Product/Delete (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            // Clear product list cache
            _memoryCache.Remove(ProductCacheKey);

            // Clear individual product details cache
            _memoryCache.Remove($"ProductDetails_{id}");

            // Clear cached products
            _memoryCache.Remove(ProductCacheKey);

            TempData["SuccessMessage"] =
                "Product deleted successfully!";

            return RedirectToAction("Index");
        }


        // POST: Submit Product Review (Customer only)
        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(
            int productId, int rating, string? comment)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // Check whether product exists
            var productExists = await _context.Products
                .AnyAsync(p => p.ProductId == productId);

            if (!productExists)
            {
                return NotFound();
            }

            // Validate rating and comment
            if (rating < 1 || rating > 5)
            {
                TempData["ReviewError"] =
                    "Please select a rating between 1 and 5 stars.";

                return RedirectToAction(
                    nameof(Details), new { id = productId });
            }

            if (string.IsNullOrWhiteSpace(comment) ||
                comment.Trim().Length < 5 ||
                comment.Trim().Length > 1000)
            {
                TempData["ReviewError"] =
                    "Review must contain between 5 and 1000 characters.";

                return RedirectToAction(
                    nameof(Details), new { id = productId });
            }

            // Check whether customer purchased and received this product
            var hasPurchased = await _context.Orders
                .AnyAsync(o =>
                    o.UserId == userId &&
                    o.OrderStatus == "Delivered" &&
                    o.OrderItems.Any(item =>
                        item.ProductId == productId));

            if (!hasPurchased)
            {
                TempData["ReviewError"] =
                    "You can review a product only after it has been delivered.";

                return RedirectToAction(
                    nameof(Details), new { id = productId });
            }

            // Prevent duplicate reviews
            var alreadyReviewed = await _context.ProductReviews
                .AnyAsync(r =>
                    r.ProductId == productId &&
                    r.UserId == userId);

            if (alreadyReviewed)
            {
                TempData["ReviewError"] =
                    "You have already reviewed this product.";

                return RedirectToAction(
                    nameof(Details), new { id = productId });
            }

            // Save review
            var review = new ProductReview
            {
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                Comment = comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductReviews.Add(review);

            await _context.SaveChangesAsync();

            TempData["ReviewMessage"] =
                "Thank you! Your review was submitted successfully.";

            return RedirectToAction(
                nameof(Details), new { id = productId });
        }

    }
}
