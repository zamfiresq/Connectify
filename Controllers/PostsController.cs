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


        public IActionResult Index()
        {
            var posts = _db.Posts;

            ViewBag.Posts = posts;

            return View();
        }


        public ActionResult Show(int id)
        {

            Post post=_db.Posts.Include("Comments")
                .Where(p => p.Id == id).First();

            ViewBag.Post = post;
            return View();
        }


        public IActionResult Create()
        {
            return View();


        }

        

        [HttpPost]
        public IActionResult Create(Post post)
        {
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
        public ActionResult Edit(int id, Post requestPost)
        {
            Post? post = _db.Posts.Find(id);

            try
            {
                post.Content = requestPost.Content;
                post.PostedAt = requestPost.PostedAt;

                _db.SaveChanges();

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return RedirectToAction("Edit", post.Id);
            }
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            Post? post = _db.Posts.Find(id);

            if (post != null)
            {
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
