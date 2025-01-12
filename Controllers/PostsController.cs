using Connectify.Data;
using Connectify.Models;
using Microsoft.AspNetCore.Authorization;
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
            var currentUser = _userManager.GetUserAsync(User).Result; // userul curent
            var currentUserId = currentUser?.Id;
            var isAdmin = User.IsInRole("Admin");

            ViewBag.CurrentUserId = currentUserId;

            // aducem postarile filtrate in functie de userul curent
            var posts = _db.Posts
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .Include(p => p.User)
                .Where(p =>
                    isAdmin || // adminul vede toate postarile
                    (currentUser == null && (p.User == null || !p.User.IsPrivate)) || // cei neautentificati vad doar postarile publice
                    (currentUser != null && (p.User == null || !p.User.IsPrivate || p.User.Id == currentUserId)) // cei autentificati vad postarile publice si pe cele ale lor
                )
                .Select(p => new
                {
                    p.Id,
                    p.Content,
                    p.PostedAt,
                    p.Media, // proprietatea media
                    CommentCount = p.Comments.Count,
                    User = p.User // include userul
                })
                .ToList();

            ViewBag.Posts = posts;

            return View();
        }


        // metoda show
        public ActionResult Show(int id)
        {

            var currentUser = _userManager.GetUserAsync(User).Result; 

            Post post = _db.Posts
                .Include(p => p.Comments) // includem comentariile
                .ThenInclude(c => c.User) // includem userul care a scris comentariul
                .Where(p => p.Id == id)
                .FirstOrDefault();

            if (post == null)
            {
                return NotFound();
            }

            ViewBag.Post = post;
            ViewBag.CurrentUserId = currentUser?.Id; // pasam id-ul userului curent

            return View();
        }


        // metoda create - new post
        public IActionResult Create()
        {
            return View();
        }


        // CreateAsync - metoda care adauga un post in baza de date
        [Authorize(Roles = "User,Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync(Post post, IFormFile Media)
        {
            if (post == null)
            {
                TempData["ErrorMessage"] = "Invalid post data.";
                return RedirectToAction("Index");
            }

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

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                post.UserId = currentUser.Id;
            }
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Cannot add post. Please log in first.";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
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




        // edit
        [Authorize(Roles = "User,Admin")]
        public IActionResult Edit(int id)
        {
            Post? post = _db.Posts.Find(id);

            ViewBag.Post = post;
            return View();
        }

        [Authorize(Roles = "User,Admin")]
        [HttpPost]
        public async Task<IActionResult> EditAsync(int id, Post requestPost, IFormFile? Media, bool removeMedia)
        {
            Post? post = _db.Posts.Find(id);

            if (post == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }

            // verificam daca userul curent este cel care a creat postarea sau admin
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // postarea are un user asociat 
            if (post.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid(); // forbidden
            }

            try
            {
                // actualizare content
                post.Content = requestPost.Content;

                // stergere media
                if (removeMedia && !string.IsNullOrEmpty(post.Media))
                {
                    var existingMediaPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", post.Media.TrimStart('/'));
                    if (System.IO.File.Exists(existingMediaPath))
                    {
                        System.IO.File.Delete(existingMediaPath);
                    }
                    post.Media = null;
                }

                // dam upload la media noua
                if (Media != null && Media.Length > 0)
                {
                    // salvare fisier
                    var mediaPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", Media.FileName);

                    using (var stream = new FileStream(mediaPath, FileMode.Create))
                    {
                        await Media.CopyToAsync(stream);
                    }

                    // dam update la post
                    post.Media = "/uploads/" + Media.FileName;
                }

                // timestamp
                post.PostedAt = requestPost.PostedAt;

                _db.SaveChanges();

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return View("Edit", post);
            }
        }


        // metoda delete
        [Authorize(Roles = "User,Admin")]
        [HttpPost]
        public ActionResult Delete(int id)
        {
            var post = _db.Posts
            .Include(p => p.Comments) // includem comentariile
            .FirstOrDefault(p => p.Id == id);

            var currentUser = _userManager.GetUserAsync(User).Result; 
            ViewBag.CurrentUserId = currentUser?.Id;
            ViewBag.post = post;

            if (post != null)
            {
                if (post.Comments != null && post.Comments.Any())
                {
                    _db.Comments.RemoveRange(post.Comments);
                } 

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
