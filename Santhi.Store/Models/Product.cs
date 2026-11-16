
using System.ComponentModel.DataAnnotations;

namespace Santhi.Store.Models
{
    public class Product
    {
        public int ProductId { get; set; }

        // Product Name
        [Required(ErrorMessage = "Please enter product name.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Product name must be between 2 and 100 characters.")]
        public string ProductName { get; set; } = string.Empty;


        // Product Description
        [StringLength(1000,
            ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string Description { get; set; } = string.Empty;


        // Product Price
        [Required(ErrorMessage = "Please enter product price.")]
        [Range(typeof(decimal), "0.01", "9999999.99",
            ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }


        // Product Image
        public string ImageUrl { get; set; } = string.Empty;


        // Product Category
        [Required(ErrorMessage = "Please select a category.")]
        [StringLength(50,
            ErrorMessage = "Category cannot exceed 50 characters.")]
        public string Category { get; set; } = string.Empty;


        // Available Stock
        [Range(0, 100000,
            ErrorMessage = "Stock must be between 0 and 100000.")]
        public int Stock { get; set; }

        // Customer Reviews
        public ICollection<ProductReview> Reviews { get; set; }
            = new List<ProductReview>();
    }
}
