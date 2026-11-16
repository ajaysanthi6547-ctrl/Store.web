
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Santhi.Store.Data;
using Santhi.Store.Models;
using System.Security.Claims;

namespace Santhi.Store.Controllers
{
    [Authorize(Roles = "Customer")]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Display customer wishlist
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var wishlist = await _context.WishlistItems
                .Include(w => w.Product)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedAt)
                .ToListAsync();

            return View(wishlist);
        }


        // Add product to wishlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var productExists = await _context.Products
                .AnyAsync(p => p.ProductId == productId);

            if (!productExists)
            {
                return NotFound();
            }

            var alreadyAdded = await _context.WishlistItems
                .AnyAsync(w =>
                    w.ProductId == productId &&
                    w.UserId == userId);

            if (!alreadyAdded)
            {
                var wishlistItem = new WishlistItem
                {
                    ProductId = productId,
                    UserId = userId!,
                    AddedAt = DateTime.UtcNow
                };

                _context.WishlistItems.Add(wishlistItem);
                await _context.SaveChangesAsync();

                TempData["WishlistMessage"] =
                    "Product added to your wishlist!";
            }
            else
            {
                TempData["WishlistMessage"] =
                    "This product is already in your wishlist.";
            }

            return RedirectToAction("Index", "Product");
        }


        // Remove product from wishlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var item = await _context.WishlistItems
                .FirstOrDefaultAsync(w =>
                    w.WishlistItemId == id &&
                    w.UserId == userId);

            if (item == null)
            {
                return NotFound();
            }

            _context.WishlistItems.Remove(item);

            await _context.SaveChangesAsync();

            TempData["WishlistMessage"] =
                "Product removed from your wishlist.";

            return RedirectToAction(nameof(Index));
        }


        // Move Wishlist Product to Cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveToCart(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var wishlistItem = await _context.WishlistItems
                .FirstOrDefaultAsync(w =>
                    w.WishlistItemId == id &&
                    w.UserId == userId);

            if (wishlistItem == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.ProductId == wishlistItem.ProductId);

            if (product == null)
            {
                return NotFound();
            }

            if (product.Stock <= 0)
            {
                TempData["WishlistMessage"] =
                    "Sorry, this product is out of stock.";

                return RedirectToAction(nameof(Index));
            }

            // Redirect to the existing Cart AddToCart action
            return RedirectToAction(
                "AddToCart",
                "Cart",
                new { id = product.ProductId });
        }

    }
}
