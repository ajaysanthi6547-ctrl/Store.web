
using System.ComponentModel.DataAnnotations;

namespace Santhi.Store.Models
{
    public class ProductReview
    {
        public int ProductReviewId { get; set; }

        // Product relationship
        public int ProductId { get; set; }

        public Product Product { get; set; } = null!;

        // Customer relationship
        [Required]
        public string UserId { get; set; } = string.Empty;

        // Star Rating
        [Range(1, 5, ErrorMessage = "Please select 1 to 5 stars.")]
        public int Rating { get; set; }

        // Customer Comment
        [Required(ErrorMessage = "Please enter your review.")]
        [StringLength(1000, MinimumLength = 5,
            ErrorMessage = "Review must be between 5 and 1000 characters.")]
        public string Comment { get; set; } = string.Empty;

        // Review Date
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
