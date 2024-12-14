using Connectify.Data;
using Connectify.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Connectify.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public PostsController(ApplicationDbContext context)
        {
            _db = context;
        }

        public ActionResult Index()
        {
            var posts = _db.Posts.Include("Comments").Select(p => new
            {
                p.Id,
                p.Content,
                p.PostedAt,
                p.Media, // Include Media property
                CommentCount = p.Comments.Count
            }).ToList();

            ViewBag.Posts = posts;
            return View();
        }

        public ActionResult Show(int id)
        {
            Post post = _db.Posts.Include("Comments")
                .Where(p => p.Id == id).First();

            var commentCount = post.Comments.Count;

            ViewBag.Post = post;
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
