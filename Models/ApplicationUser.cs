using Microsoft.AspNetCore.Identity;
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

        public ICollection<Post> Posts { get; set; } // A user can have many posts


    }
}
