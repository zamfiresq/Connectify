using Connectify.Data;
using Connectify.Models;
using Microsoft.AspNetCore.Mvc;

namespace Connectify.Controllers
{


    public class CommentsController : Controller
    {

        private readonly ApplicationDbContext _db;
        public CommentsController(ApplicationDbContext context)
        {
            _db = context;
        }



        [HttpPost]
        public IActionResult New(Comment comm)
        {
            comm.CommentedAt = DateTime.Now;

            try
            {
                _db.Comments.Add(comm);
                _db.SaveChanges();
                return Redirect("/Posts/Show/" + comm.PostId);
            }

            catch (Exception)
            {
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

                return Redirect("/Comments/Show/" + comm.PostId);
            }
            catch (Exception e)
            {
                return Redirect("/Comments/Show/" + comm.PostId);
            }

        }

    }
}
