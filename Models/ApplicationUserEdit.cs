using System.ComponentModel.DataAnnotations;

namespace Connectify.Models
{
    public class ApplicationUserEdit
    {
        public string Id { get; set; } // Keep the ID for identifying the user
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        public bool IsPrivate { get; set; }
    }
}
