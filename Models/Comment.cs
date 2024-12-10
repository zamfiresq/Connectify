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
        public int PostId { get; set; }
        public Post Post { get; set; }

        // de adaugat user

    }
}
