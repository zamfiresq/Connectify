using Microsoft.AspNetCore.Mvc;
using Connectify.Models;
using System.Linq;
using Connectify.Data;
using Microsoft.EntityFrameworkCore;

namespace Connectify.Controllers
{
    public class ApplicationUserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ApplicationUserController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return PartialView("UserSearchResults", new List<ApplicationUser>());

            var users = _context.Users
                .Where(u => u.FirstName.Contains(query) || u.LastName.Contains(query))
                .OrderBy(u => u.FirstName)
                .Take(10)
                .ToList();

            return PartialView("UserSearchResults", users);
        }

        public IActionResult Show(string id)
        {
            // Find user by id
            var user = _context.Users
                .Include(u=>u.Posts)
                    .ThenInclude(p => p.Comments)
                .Where(u => u.Id == id)
                .FirstOrDefault();

            // Check if user exists
            if (user == null)
            {
                return NotFound(); // User not found
            }

            // Return the view with the user data
            return View(user);
        }
    }
}

