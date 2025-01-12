using System.ComponentModel.DataAnnotations;

namespace Connectify.Models
{
    public class Group
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Your group must have a name!")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Group name must be between 3 and 100 characters.")]
        public string GroupName { get; set; }
        [Required(ErrorMessage = "Your group must have a description!")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Description must be between 5 and 500 characters.")]
        public string Description { get; set; }

        // PASUL 6: useri si roluri
        public virtual ApplicationUser? User { get; set; }
        public string? UserId { get; set; }

        // un grup are o lista de mesaje
        public virtual ICollection<Message>? Messages { get; set; } 

        // un grup are o lista de useri
        public virtual ICollection<UserGroup>? UserGroups { get; set; }
    }
}
