
using Microsoft.AspNetCore.Mvc;
using Santhi.Store.Data;
using Santhi.Store.Models;
using System.Diagnostics;

namespace Santhi.Store.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var products = _context.Products.ToList();

            return View(products);
        }

        public IActionResult Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return RedirectToAction("Index");
            }

            searchTerm = searchTerm.Trim();

            var products = _context.Products
                .Where(p =>
                    p.ProductName.Contains(searchTerm) ||
                    p.Category.Contains(searchTerm) ||
                    (p.Description != null &&
                     p.Description.Contains(searchTerm)))
                .ToList();

            return View("Index", products);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
