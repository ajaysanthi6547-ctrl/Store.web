
using System.ComponentModel.DataAnnotations;

namespace Santhi.Store.Models
{
    public class WishlistItem
    {
        [Key]
        public int WishlistItemId { get; set; }

        public int ProductId { get; set; }

        public Product Product { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
