
using System.ComponentModel.DataAnnotations;

namespace Santhi.Store.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        public string? UserId { get; set; }

        [Required(ErrorMessage = "Please enter your full name.")]
        [StringLength(100, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your mobile number.")]
        [RegularExpression(@"^(\+91[-\s]?)?[6-9]\d{9}$",
            ErrorMessage = "Enter a valid 10-digit Indian mobile number.")]
        public string MobileNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your email.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your address.")]
        [StringLength(250, MinimumLength = 5)]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your city.")]
        [StringLength(80, MinimumLength = 2)]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your pincode.")]
        [RegularExpression(@"^[1-9][0-9]{5}$",
            ErrorMessage = "Enter a valid 6-digit Indian pincode.")]
        public string Pincode { get; set; } = string.Empty;

        [Range(typeof(decimal), "0.01", "99999999",
            ErrorMessage = "Invalid order amount.")]
        public decimal TotalAmount { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [StringLength(30)]
        public string OrderStatus { get; set; } = "Pending";

        // Track whether stock was deducted for this order
        public bool StockDeducted { get; set; } = false;

        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
