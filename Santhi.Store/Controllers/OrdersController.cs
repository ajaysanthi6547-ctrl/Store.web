
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Santhi.Store.Data;
using System.Security.Claims;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Santhi.Store.Controllers
{
    [Authorize(Roles = "Customer")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Display Logged-in Customer Orders
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // Display Individual Customer Order Details
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == id &&
                    o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // Customer Cancel Order

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == id &&
                    o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            if (order.OrderStatus != "Pending")
            {
                TempData["OrderError"] =
                    "Only pending orders can be cancelled.";

                return RedirectToAction("Details", new { id });
            }

            // Restore stock only if it was previously deducted
            if (order.StockDeducted)
            {
                foreach (var item in order.OrderItems)
                {
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p =>
                            p.ProductId == item.ProductId);

                    if (product != null)
                    {
                        product.Stock += item.Quantity;
                    }
                }

                // Prevent stock from being restored again
                order.StockDeducted = false;
            }

            order.OrderStatus = "Cancelled";

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            TempData["OrderMessage"] =
                "Your order was cancelled and product stock was restored successfully.";

            return RedirectToAction("Details", new { id });
        }


        [HttpGet]
        public async Task<IActionResult> DownloadInvoice(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get only the logged-in customer's order
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            // Generate PDF invoice
            var pdfBytes = Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header()
                        .Text("TRENDKART")
                        .FontSize(25)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    page.Content().Column(column =>
                    {
                        column.Spacing(12);

                        column.Item()
                            .Text("TAX INVOICE")
                            .FontSize(20)
                            .Bold();

                        column.Item().Text($"Invoice Number: TK-{order.OrderId}");

                        column.Item().Text(
                            $"Order Date: {order.OrderDate:dd-MM-yyyy}");

                        column.Item().Text(
                            $"Order Status: {order.OrderStatus}");

                        column.Item().PaddingTop(10)
                            .Text("Customer Details")
                            .Bold()
                            .FontSize(14);

                        column.Item().Text($"Name: {order.FullName}");
                        column.Item().Text($"Mobile: {order.MobileNumber}");
                        column.Item().Text($"Email: {order.Email}");
                        column.Item().Text($"Address: {order.Address}");
                        column.Item().Text($"City: {order.City}");
                        column.Item().Text($"Pincode: {order.Pincode}");

                        column.Item().PaddingTop(15)
                            .Text("Purchased Products")
                            .Bold()
                            .FontSize(14);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten2)
                                    .Padding(5).Text("Product").Bold();

                                header.Cell().Background(Colors.Grey.Lighten2)
                                    .Padding(5).Text("Price").Bold();

                                header.Cell().Background(Colors.Grey.Lighten2)
                                    .Padding(5).Text("Qty").Bold();

                                header.Cell().Background(Colors.Grey.Lighten2)
                                    .Padding(5).Text("Subtotal").Bold();
                            });

                            foreach (var item in order.OrderItems)
                            {
                                table.Cell().Padding(5).Text(item.ProductName);
                                table.Cell().Padding(5).Text(item.Price.ToString("N2"));
                                table.Cell().Padding(5).Text(item.Quantity.ToString());
                                table.Cell().Padding(5).Text(item.Subtotal.ToString("N2"));
                            }
                        });

                        column.Item().AlignRight()
                            .PaddingTop(15)
                            .Text($"Total Amount: Rs. {order.TotalAmount:N2}")
                            .Bold()
                            .FontSize(16)
                            .FontColor(Colors.Green.Darken2);

                        column.Item().PaddingTop(25)
                            .AlignCenter()
                            .Text("Thank you for shopping with TrendKart!");
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text("TrendKart - Online Shopping");
                });
            }).GeneratePdf();

            return File(
                pdfBytes,
                "application/pdf",
                $"TrendKart_Invoice_Order_{order.OrderId}.pdf");
        }


    }
}