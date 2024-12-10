using System.ComponentModel.DataAnnotations;

namespace Connectify.Models
{
    public class Group
    {
        [Key]
        public int Id { get; set; }
        public string GroupName { get; set; }

        // de adaugat user

        // un grup are o lista de mesaje
        public virtual ICollection<Message> Messages { get; set; }
    }
}
