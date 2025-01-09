using Microsoft.AspNetCore.Mvc;
using Connectify.Models;
using System.Linq;
using Connectify.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Connectify.Controllers
{
    public class ApplicationUserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ApplicationUserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)

        {
            _context = context;
            _userManager = userManager;
        }





        [Authorize] // Ensure the user is logged in
        public IActionResult Edit(string id)
        {
            // Get the currently logged-in user's ID
            var currentUserId = _userManager.GetUserId(User);

            // Find the user by id
            var user = _context.Users.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Check if the logged-in user is an admin or the owner of the profile
            var isAdmin = User.IsInRole("Admin"); // Check if the logged-in user is an admin
            if (user.Id != currentUserId && !isAdmin)
            {
                return Forbid(); // Return 403 Forbidden if unauthorized
            }

            // Map user to a ViewModel
            var editModel = new ApplicationUserEdit
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Bio = user.Bio,
                IsPrivate = user.IsPrivate
            };

            return View(editModel);
        }

        [HttpPost]
        [Authorize] // Ensure the user is logged in
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ApplicationUserEdit model)
        {
            // Get the currently logged-in user's ID
            var currentUserId = _userManager.GetUserId(User);

            // Find the user by id
            var user = _context.Users.FirstOrDefault(u => u.Id == model.Id);

            if (user == null)
            {
                return NotFound();
            }

            // Check if the logged-in user is an admin or the owner of the profile
            var isAdmin = User.IsInRole("Admin");
            if (user.Id != currentUserId && !isAdmin)
            {
                return Forbid(); // Return 403 Forbidden if unauthorized
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Update the user's fields
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Bio = model.Bio;
            user.IsPrivate = model.IsPrivate;

            // Save changes to the database
            _context.SaveChanges();

            return RedirectToAction("Show", new { id = user.Id });
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

