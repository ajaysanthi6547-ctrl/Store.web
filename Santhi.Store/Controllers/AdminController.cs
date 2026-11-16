
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Santhi.Store.Data;


namespace Santhi.Store.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Admin Dashboard
        public async Task<IActionResult> Index()
        {
            ViewBag.LowStockCount = await _context.Products
                .CountAsync(p => p.Stock > 0 && p.Stock < 10);

            return View();
        }

        // Sales Report Dashboard
        public async Task<IActionResult> SalesReport()
        {
            // Exclude cancelled orders from sales
            var validOrders = _context.Orders
                .Where(o => o.OrderStatus != "Cancelled");

            // Total Sales
            ViewBag.TotalSales = await validOrders
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // Total Orders
            ViewBag.TotalOrders = await validOrders.CountAsync();

            // Total Customers
            var customers = await _userManager
                .GetUsersInRoleAsync("Customer");

            ViewBag.TotalCustomers = customers.Count;

            // Best-Selling Products
            ViewBag.BestSellingProducts = await _context.Orders
                .Where(o => o.OrderStatus != "Cancelled")
                .SelectMany(o => o.OrderItems)
                .GroupBy(i => new { i.ProductId, i.ProductName })
                .Select(g => new
                {
                    ProductName = g.Key.ProductName,
                    TotalQuantity = g.Sum(i => i.Quantity),
                    TotalRevenue = g.Sum(i => i.Subtotal)
                })
                .OrderByDescending(p => p.TotalQuantity)
                .Take(5)
                .ToListAsync();

            // Monthly Sales for the Last 12 Months
            var today = DateTime.Today;

            var startDate = new DateTime(today.Year, today.Month, 1)
                .AddMonths(-11);

            var orderData = await validOrders
                .Where(o => o.OrderDate >= startDate)
                .Select(o => new
                {
                    o.OrderDate,
                    o.TotalAmount
                })
                .ToListAsync();

            ViewBag.MonthlySales = Enumerable.Range(0, 12)
                .Select(i =>
                {
                    var month = startDate.AddMonths(i);

                    return new
                    {
                        Month = month.ToString("MMM yyyy"),
                        TotalSales = orderData
                            .Where(o =>
                                o.OrderDate.Year == month.Year &&
                                o.OrderDate.Month == month.Month)
                            .Sum(o => o.TotalAmount)
                    };
                })
                .ToList();

            return View();
        }

        // Inventory Management
        public async Task<IActionResult> Inventory()
        {
            var products = await _context.Products
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            return View(products);
        }

        // Update Product Stock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int id, int newStock)
        {
            if (newStock < 0)
            {
                return BadRequest("Stock cannot be negative.");
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            product.Stock = newStock;

            await _context.SaveChangesAsync();

            TempData["StockMessage"] =
                "Product stock updated successfully!";

            return RedirectToAction("Inventory");


        }

        // Display All Customer Orders
        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // Display Individual Order Details
        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // Update Customer Order Status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(
            int id, string status)
        {
            var allowedStatuses = new[]
            {
                "Pending",
                "Processing",
                "Shipped",
                "Delivered",
                "Cancelled"
            };

            if (!allowedStatuses.Contains(status))
            {
                return BadRequest("Invalid order status.");
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            order.OrderStatus = status;

            await _context.SaveChangesAsync();

            TempData["OrderMessage"] =
                "Order status updated successfully!";

            return RedirectToAction("OrderDetails", new { id });
        }

        
        // Customer Management with Search
        public async Task<IActionResult> Customers(string searchTerm)
        {
            var customers = await _userManager.GetUsersInRoleAsync("Customer");

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                customers = customers
                    .Where(c =>
                        (c.UserName ?? "").Contains(
                            searchTerm,
                            StringComparison.OrdinalIgnoreCase) ||
                        (c.Email ?? "").Contains(
                            searchTerm,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ViewBag.SearchTerm = searchTerm;

            return View(customers);
        }

        // View Customer Details
        public async Task<IActionResult> CustomerDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var customer = await _userManager.FindByIdAsync(id);

            if (customer == null ||
                !await _userManager.IsInRoleAsync(customer, "Customer"))
            {
                return NotFound();
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.CustomerOrders = orders;

            return View(customer);
        }
        // Deactivate Customer Account
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateCustomer(string id)
        {
            var customer = await _userManager.FindByIdAsync(id);

            if (customer == null ||
                !await _userManager.IsInRoleAsync(customer, "Customer"))
            {
                return NotFound();
            }

            customer.LockoutEnabled = true;
            customer.LockoutEnd = DateTimeOffset.MaxValue;

            var result = await _userManager.UpdateAsync(customer);

            if (!result.Succeeded)
            {
                TempData["CustomerMessage"] =
                    "Unable to deactivate customer account.";
            }
            else
            {
                TempData["CustomerMessage"] =
                    "Customer account deactivated successfully!";
            }

            return RedirectToAction("Customers");
        }

        // Activate Customer Account
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateCustomer(string id)
        {
            var customer = await _userManager.FindByIdAsync(id);

            if (customer == null ||
                !await _userManager.IsInRoleAsync(customer, "Customer"))
            {
                return NotFound();
            }

            customer.LockoutEnd = null;

            var result = await _userManager.UpdateAsync(customer);

            if (!result.Succeeded)
            {
                TempData["CustomerMessage"] =
                    "Unable to activate customer account.";
            }
            else
            {
                TempData["CustomerMessage"] =
                    "Customer account activated successfully!";
            }

            return RedirectToAction("Customers");
        }
    }
}
