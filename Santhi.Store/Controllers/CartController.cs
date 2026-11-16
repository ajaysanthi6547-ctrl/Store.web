
using Microsoft.AspNetCore.Mvc;
using Santhi.Store.Data;
using Santhi.Store.Extensions;
using Santhi.Store.Models;

namespace Santhi.Store.Controllers
{
    public class CartController : Controller
    {
        private const string CartSessionKey = "Cart";

        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Display Cart
        public IActionResult Index()
        {
            var cart = HttpContext.Session
                .GetObject<List<CartItem>>(CartSessionKey)
                ?? new List<CartItem>();

            return View(cart);
        }


       

            // Add Product to Cart
            [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int id)
        {
            // Check whether the visitor is logged in
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["RegistrationRequired"] =
                    "Please register or log in before adding products to your cart.";

                return RedirectToAction("Register", "Account");
            }

            var product = _context.Products
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            var cart = HttpContext.Session
                .GetObject<List<CartItem>>(CartSessionKey)
                ?? new List<CartItem>();



            // Check available stock
            if (product.Stock <= 0)
            {
                TempData["CartError"] =
                    "Sorry, this product is out of stock.";

                return RedirectToAction("Index");
            }

            var existingItem = cart.FirstOrDefault(
                x => x.ProductId == id);

            if (existingItem != null)
            {
                if (existingItem.Quantity >= product.Stock)
                {
                    TempData["CartError"] =
                        "Sorry, only " + product.Stock +
                        " items are available in stock.";

                    return RedirectToAction("Index");
                }

                existingItem.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    Quantity = 1
                });
            }

            HttpContext.Session.SetObject(CartSessionKey, cart);

            return RedirectToAction("Index", "Cart");
        }


        // Increase Quantity
        public IActionResult IncreaseQuantity(int id)
        {
            var cart = HttpContext.Session
                .GetObject<List<CartItem>>(CartSessionKey)
                ?? new List<CartItem>();

            var item = cart.FirstOrDefault(x => x.ProductId == id);

            if (item != null)
            {
                var product = _context.Products
                    .FirstOrDefault(p => p.ProductId == id);

                if (product == null)
                {
                    cart.Remove(item);
                    TempData["CartError"] =
                        "Product is no longer available.";
                }
                else if (item.Quantity >= product.Stock)
                {
                    TempData["CartError"] =
                        "Only " + product.Stock +
                        " items are available in stock.";
                }
                else
                {
                    item.Quantity++;
                }
            }

            HttpContext.Session.SetObject(CartSessionKey, cart);

            return RedirectToAction("Index");
        }

        // Decrease Quantity
        public IActionResult DecreaseQuantity(int id)
        {
            var cart = HttpContext.Session
                .GetObject<List<CartItem>>(CartSessionKey)
                ?? new List<CartItem>();

            var item = cart.FirstOrDefault(x => x.ProductId == id);

            if (item != null)
            {
                item.Quantity--;

                if (item.Quantity <= 0)
                {
                    cart.Remove(item);
                }
            }

            HttpContext.Session.SetObject(CartSessionKey, cart);

            return RedirectToAction("Index");
        }

        // Remove Product
        public IActionResult RemoveFromCart(int id)
        {
            var cart = HttpContext.Session
                .GetObject<List<CartItem>>(CartSessionKey)
                ?? new List<CartItem>();

            var item = cart.FirstOrDefault(x => x.ProductId == id);

            if (item != null)
            {
                cart.Remove(item);
            }

            HttpContext.Session.SetObject(CartSessionKey, cart);

            return RedirectToAction("Index");
        }
    }
}
