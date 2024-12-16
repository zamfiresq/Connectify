using System.ComponentModel.DataAnnotations.Schema;

namespace Connectify.Models
{
    public class FollowRequest
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public bool IsAccepted { get; set; } // daca cererea a fost acceptata sau nu

        // PASUL 6: useri si roluri
        public virtual ApplicationUser? Sender { get; set; }
        public string? SenderId { get; set; }

        public virtual ApplicationUser? Receiver { get; set; }
        public string? ReceiverId { get; set; }
    }
}
