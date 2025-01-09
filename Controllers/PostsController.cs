using Connectify.Data;
using Connectify.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Connectify.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public PostsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
        )
        {
            _db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public ActionResult Index()
        {
            var posts = _db.Posts
               .Include(p => p.Comments)
               .ThenInclude(c => c.User)
               .Include(p => p.User)
               .Select(p => new
               {
                   p.Id,
                   p.Content,
                   p.PostedAt,
                   p.Media, // Include Media property
                   CommentCount = p.Comments.Count,
                   User = p.User // Explicitly include the User
               })
               .ToList();

            ViewBag.Posts = posts;

            return View();
        }




        public ActionResult Show(int id)
        {

            var currentUser = _userManager.GetUserAsync(User).Result; // Get the current logged-in user

            Post post = _db.Posts
                .Include(p => p.Comments) // Include Comments
                .ThenInclude(c => c.User) // Include User for each comment
                .Where(p => p.Id == id)
                .FirstOrDefault();

            if (post == null)
            {
                return NotFound();
            }

            ViewBag.Post = post;
            ViewBag.CurrentUserId = currentUser?.Id; // Pass the current user's ID to the view

            return View();
        }


        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync(Post post, IFormFile Media)
        {
            if (Media != null && Media.Length > 0)
            {
                var mediaPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", Media.FileName);

                using (var stream = new FileStream(mediaPath, FileMode.Create))
                {
                    await Media.CopyToAsync(stream);
                }

                post.Media = "/uploads/" + Media.FileName;
            }
            else
            {
                post.Media = null;
            }

            post.PostedAt = DateTime.Now;


            // Associate the post with the currently logged-in user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                post.UserId = currentUser.Id;
            }
            if (currentUser == null)
            {
                // Add an error message and redirect to login or another page
                TempData["ErrorMessage"] = "Cannot add post. Please log in first.";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
                // Redirect to the login page or desired action
            }

            try
            {
                _db.Posts.Add(post);
                _db.SaveChanges();

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return View();
            }
        }

        public IActionResult Edit(int id)
        {
            Post? post = _db.Posts.Find(id);

            ViewBag.Post = post;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EditAsync(int id, Post requestPost, IFormFile? Media, bool removeMedia)
        {
            Post? post = _db.Posts.Find(id);

            if (post == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }

            try
            {
                // Update content
                post.Content = requestPost.Content;

                // Handle media removal
                if (removeMedia && !string.IsNullOrEmpty(post.Media))
                {
                    var existingMediaPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", post.Media.TrimStart('/'));
                    if (System.IO.File.Exists(existingMediaPath))
                    {
                        System.IO.File.Delete(existingMediaPath);
                    }
                    post.Media = null;
                }

                // Handle new media upload
                if (Media != null && Media.Length > 0)
                {
                    // Save new file
                    var mediaPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", Media.FileName);

                    using (var stream = new FileStream(mediaPath, FileMode.Create))
                    {
                        await Media.CopyToAsync(stream);
                    }

                    // Update post's media
                    post.Media = "/uploads/" + Media.FileName;
                }

                // Update timestamp
                post.PostedAt = requestPost.PostedAt;

                _db.SaveChanges();

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return View("Edit", post);
            }
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            Post? post = _db.Posts.Find(id);

            if (post != null)
            {
                if (!string.IsNullOrEmpty(post.Media))
                {
                    var mediaPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", post.Media.TrimStart('/'));
                    if (System.IO.File.Exists(mediaPath))
                    {
                        System.IO.File.Delete(mediaPath);
                    }
                }

                _db.Posts.Remove(post);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
        }
    }
}
