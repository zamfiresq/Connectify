using System.ComponentModel.DataAnnotations;

namespace Connectify.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage ="Your post must have a conent!")]
        public string Content { get; set; }
        public DateTime PostedAt { get; set; }

        // o postare are o lista de comentarii
        public virtual ICollection<Comment> Comments { get; set; }

        // de adaugat user
    }
}
