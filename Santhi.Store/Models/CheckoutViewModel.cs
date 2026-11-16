using System.ComponentModel.DataAnnotations;

namespace Santhi.Store.Models
{
    public class CheckoutViewModel
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string MobileNumber { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Enter a valid 6-digit pincode.")]
        public string Pincode { get; set; } = string.Empty;
    }
}