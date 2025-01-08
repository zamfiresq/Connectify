using Connectify.Data;
using Connectify.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Connectify.Controllers
{


    public class CommentsController : Controller
    {

        private readonly ApplicationDbContext _db;

        // PASUL 10: useri si roluri
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CommentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
        )
        {
            _db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }



        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> New(Comment comm)
        {
            // Set the CommentedAt property to the current date and time
            comm.CommentedAt = DateTime.Now;

            try
            {
                
                // Get the current logged-in user
                var currentUser = await _userManager.GetUserAsync(User);

                // If a user is logged in, set the user-related properties
                if (currentUser != null)
                {
                    comm.User = currentUser; // Set the full user object (optional, depending on your needs)
                    comm.UserId = currentUser.Id; // Store the UserId for reference (important for FK relationships)
                }

                // Add the comment to the database
                _db.Comments.Add(comm);
                await _db.SaveChangesAsync();

                // Redirect to the post's show page
                return Redirect("/Posts/Show/" + comm.PostId);
            }
            catch (Exception)
            {
                // If an error occurs, redirect back to the post's show page
                return Redirect("/Posts/Show/" + comm.PostId);
            }
        }


        [HttpPost]
        public IActionResult Delete(int id)
        {
            Comment comm = _db.Comments.Find(id);
            _db.Comments.Remove(comm);
            _db.SaveChanges();
            return Redirect("/Posts/Show/" + comm.PostId);
        }


        public IActionResult Edit(int id)
        {
            Comment comm = _db.Comments.Find(id);
            ViewBag.Comment = comm;
            return View();
        }

        [HttpPost]
        public IActionResult Edit(int id, Comment requestComment)
        {
            Comment comm = _db.Comments.Find(id);
            try
            {

                comm.Text = requestComment.Text;

                _db.SaveChanges();

                return Redirect("/Posts/Show/" + comm.PostId);
            }
            catch (Exception e)
            {
                return Redirect("/Posts/Show/" + comm.PostId);
            }

        }



    }
}
