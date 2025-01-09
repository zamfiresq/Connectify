using System.ComponentModel.DataAnnotations.Schema;

namespace Connectify.Models
{
    // tabela asociativa intre useri si grupuri
    public class UserGroup
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        public virtual ApplicationUser? User { get; set; }
        public string? UserId { get; set; }

        public virtual Group? Group { get; set; }
        public int? GroupId { get; set; }

        // bool pentru acceparea cererii de a intra in grup
        public bool IsAccepted { get; set; }

    }
}
