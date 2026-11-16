
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Santhi.Store.Data;
using Santhi.Store.Extensions;
using Santhi.Store.Models;

namespace Santhi.Store.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CheckoutController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Display Checkout Page
        [HttpGet]
        public IActionResult Index()
        {
            var cart = HttpContext.Session
                .GetObject<List<CartItem>>("Cart")
                ?? new List<CartItem>();

            if (!cart.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            ViewBag.Cart = cart;

            return View(new CheckoutViewModel());
        }

        // Save Customer Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var cart = HttpContext.Session
                .GetObject<List<CartItem>>("Cart")
                ?? new List<CartItem>();

            if (!cart.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Cart = cart;
                return View("Index", model);
            }

            // Validate cart quantities
            if (cart.Any(item => item.Quantity <= 0 ||
                                 item.Quantity > 1000))
            {
                TempData["CartError"] = "Invalid product quantity.";
                return RedirectToAction("Index", "Cart");
            }

            // Combine duplicate products in the cart
            var cartItems = cart
                .GroupBy(item => item.ProductId)
                .Select(group => new
                {
                    ProductId = group.Key,
                    Quantity = group.Sum(item => item.Quantity)
                })
                .ToList();

            // Get current product details from database
            var productIds = cartItems
                .Select(item => item.ProductId)
                .ToList();

            var products = await _context.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId);

            // Check whether all products exist
            if (products.Count != cartItems.Count)
            {
                TempData["CartError"] =
                    "One or more products are no longer available.";

                return RedirectToAction("Index", "Cart");
            }

            // Check available stock
            foreach (var item in cartItems)
            {
                var product = products[item.ProductId];

                if (product.Stock < item.Quantity)
                {
                    TempData["CartError"] =
                        $"{product.ProductName} does not have enough stock.";

                    return RedirectToAction("Index", "Cart");
                }
            }

            // Calculate total using database prices
            decimal total = cartItems.Sum(item =>
                products[item.ProductId].Price * item.Quantity);

            if (total <= 0)
            {
                TempData["CartError"] = "Invalid order amount.";
                return RedirectToAction("Index", "Cart");
            }

            // Begin database transaction
            using var transaction =
                await _context.Database.BeginTransactionAsync();

            // Deduct stock safely
            foreach (var item in cartItems)
            {
                int updatedRows = await _context.Products
                    .Where(p => p.ProductId == item.ProductId &&
                                p.Stock >= item.Quantity)
                    .ExecuteUpdateAsync(update => update
                        .SetProperty(
                            p => p.Stock,
                            p => p.Stock - item.Quantity));

                if (updatedRows == 0)
                {
                    await transaction.RollbackAsync();

                    TempData["CartError"] =
                        "Product stock changed. Please check your cart again.";

                    return RedirectToAction("Index", "Cart");
                }
            }

            // Create order
            var order = new Order
            {
                UserId = User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,

                FullName = model.FullName.Trim(),
                MobileNumber = model.MobileNumber.Trim(),
                Email = model.Email.Trim(),
                Address = model.Address.Trim(),
                City = model.City.Trim(),
                Pincode = model.Pincode.Trim(),

                TotalAmount = total,
                OrderDate = DateTime.Now,
                OrderStatus = "Pending",
                StockDeducted = true
            };

            _context.Orders.Add(order);

            // Create order items using database product details
            foreach (var item in cartItems)
            {
                var product = products[item.ProductId];

                var orderItem = new OrderItem
                {
                    Order = order,
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Price = product.Price,
                    Quantity = item.Quantity,
                    Subtotal = product.Price * item.Quantity
                };

                _context.OrderItems.Add(orderItem);
            }

            // Save order and order items
            await _context.SaveChangesAsync();

            // Commit stock and order changes together
            await transaction.CommitAsync();

            // Clear cart only after successful order
            HttpContext.Session.Remove("Cart");

            TempData["OrderMessage"] =
                $"Your order #{order.OrderId} has been placed successfully!";

            return RedirectToAction("Index", "Cart");
        }
    }
}
