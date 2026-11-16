using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Santhi.Store.Data;

namespace Santhi.Store.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("ApiPerUserOrIp")]
    public class ProductsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ProductsApi
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Products
                .Select(p => new
                {
                    p.ProductId,
                    p.ProductName,
                    p.Description,
                    p.Price,
                    p.Category,
                    p.Stock,
                    p.ImageUrl
                })
                .ToListAsync();

            return Ok(products);
        }

        // GET: api/ProductsApi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products
                .Where(p => p.ProductId == id)
                .Select(p => new
                {
                    p.ProductId,
                    p.ProductName,
                    p.Description,
                    p.Price,
                    p.Category,
                    p.Stock,
                    p.ImageUrl
                })
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }
    }
}