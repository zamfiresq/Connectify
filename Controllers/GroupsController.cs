using Connectify.Data;
using Connectify.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Connectify.Controllers
{
    public class GroupsController : Controller
    {
        private readonly ApplicationDbContext dbc;

        // PASUL 10: useri si roluri
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;


        public GroupsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
        )
        {
            dbc = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        //index - afisarea tuturor grupurilor
        public IActionResult Index()
        {
            var groups = dbc.Groups;

            ViewBag.Groups = groups;

            return View();
        }

        // show - afisarea unui grup dupa id cu mesajele asociate
        public ActionResult Show(int id)
        {
            Group group = dbc.Groups.Include(g => g.Messages)
                .Where(g => g.Id == id)
                .FirstOrDefault();

            if (group == null)
            {
                return NotFound();
            }

            ViewBag.Group = group;
            return View(group);
        }

        // New - formular pentru crearea unui grup
        public IActionResult New()
        {
            return View();
        }

        [HttpPost]
        public IActionResult New(Group group)
        {
            try
            {
                group.GroupName = group.GroupName;
                group.Description = group.Description;

                dbc.Groups.Add(group);
                dbc.SaveChanges();

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return View();
            }
        }

        // edit
        // httpget default
        public IActionResult Edit(int id)
        {
            Group? group = dbc.Groups.Find(id);

            if (group == null)
            {
                return NotFound();
            }

            ViewBag.Group = group;
            return View(group);
        }

        // formularul de editare
        [HttpPost]
        public IActionResult Edit(int id, Group requestGroup)
        {
            Group? group = dbc.Groups.Find(id);

            if (group == null)
            {
                return NotFound();
            }

            try
            {
                group.GroupName = requestGroup.GroupName;
                group.Description = requestGroup.Description;
                TempData["message"] = "Group updated successfully!";
                dbc.SaveChanges();

                return RedirectToAction("Index");
            }
            catch (Exception) // cand nu se poate face update
            {
                TempData["message"] = "Group not found!";
                return RedirectToAction("Edit", new { id = group.Id });
            }
        }

        // delete - stergerea unui grup
        [HttpPost]
        public ActionResult Delete(int id)
        {
            Group? group = dbc.Groups.Find(id);

            if (group != null)
            {
                dbc.Groups.Remove(group);
                dbc.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
        }
    }
}
