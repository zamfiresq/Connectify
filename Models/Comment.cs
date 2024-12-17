using System.ComponentModel.DataAnnotations;

namespace Connectify.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Your comment can't be empty!")]
        public string Text { get; set; }
        public DateTime CommentedAt { get; set; }

        // FK 
        public int? PostId { get; set; }
        public Post? Post { get; set; }

        // PASUL 6: useri si roluri
        public virtual ApplicationUser? User { get; set; }
        public string? UserId { get; set; }

    }
}
