using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

// PASUL 1: useri si roluri
namespace Connectify.Models
{

    // campurile pe care le are un user pe pagina sa de profil
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Bio { get; set; }
        //public string ProfilePicture { get; set; }
        public bool IsPrivate { get; set; } // daca profilul este privat sau nu

        // un user poate avea mai multe postari
        public ICollection<Post>? Posts { get; set; }

        // un user poate avea mai multe comentarii
        public ICollection<Comment>? Comments { get; set; }

        // un user poate trimite mai multe mesaje 
        public ICollection<Message>? Messages { get; set; }

        // un user poate face parte din mai multe grupuri
        public ICollection<UserGroup>? UserGroups { get; set; }

        // un user poate trimite si primi mai multe cereri de follow
        // public ICollection<FollowRequest>? SentFollowRequests { get; set; }
        // public ICollection<FollowRequest>? ReceivedFollowRequests { get; set; }


        // pentru selectarea rolurilor dintr-un dropdown
        [NotMapped]
        public IEnumerable<SelectListItem>? Roles { get; set; } 




    }
}
