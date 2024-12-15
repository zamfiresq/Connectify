using System.ComponentModel.DataAnnotations;

namespace Connectify.Models
{
    public class Group
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Your group must have a name!")]
        public string GroupName { get; set; }
        [Required(ErrorMessage = "Your group must have a description!")]
        public string Description { get; set; }

        // de adaugat user

        // un grup are o lista de mesaje
        public virtual ICollection<Message> Messages { get; set; }
    }
}
