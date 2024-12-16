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

                return Redirect("/Posts/Show/" + comm.PostId);
            }
            catch (Exception e)
            {
                return Redirect("/Posts/Show/" + comm.PostId);
            }

        }



    }
}
