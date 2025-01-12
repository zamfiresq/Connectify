using System.ComponentModel.DataAnnotations;

namespace Connectify.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Your message must have a content!")]
        [StringLength(500, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 500 characters.")]
        public string TextMessage { get; set; }
        public DateTime SentAt { get; set; }

        // FK
        public int GroupId { get; set; }
        public Group? Group { get; set; }

        // PASUL 6: useri si roluri
        public virtual ApplicationUser? User { get; set; }
        public string? UserId { get; set; }

    }
}
