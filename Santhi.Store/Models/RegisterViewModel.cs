
using System.ComponentModel.DataAnnotations;

namespace Santhi.Store.Models
{
    public class RegisterViewModel
    {
        // Full Name validation
        [Required(ErrorMessage = "Please enter your name.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Name must be between 2 and 100 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;


        // Email validation
        [Required(ErrorMessage = "Please enter your email.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(256,
            ErrorMessage = "Email cannot exceed 256 characters.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;


        // Password validation
        [Required(ErrorMessage = "Please enter a password.")]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "Password must be between 6 and 100 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;


        // Confirm Password validation
        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare("Password",
            ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
