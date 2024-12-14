using Connectify.Data;
using Connectify.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Connectify.Controllers
{
    public class GroupsController : Controller
    {

        private readonly ApplicationDbContext dbc;

        // applicationuser + identity role

        // constructor 
        public GroupsController(ApplicationDbContext dbc)
        {
            this.dbc = dbc;
        }


        // index - afisarea tuturor grupurilor
        public IActionResult Index()
        {
            var grup = dbc.Groups;
            ViewBag.Groups = grup;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
            }
            return View();
        }


        // show - afisarea unui grup specific dupa id
        public IActionResult Show(int id)
        {
            // orice user care face parte din niste grupuri poate sa le vada
            // conditie de pus pt roluri
            var group = dbc.Groups.Include("Messages")
                        .Where(g => g.Id == id)
                        .First();
            return View(group);
        }

        // show - pentru mesajele unui grup
        [HttpPost]
        public IActionResult Show(int id, [FromForm] Message mesaj)
        {
            mesaj.SentAt = DateTime.Now;

            try
            {
                dbc.Messages.Add(mesaj);
                dbc.SaveChanges();
                return Redirect("/Groups/Show/" + mesaj.GroupId);
            }

            catch (Exception ex)
            {
                Group grp = dbc.Groups.Include("Comments")
                            .Where(grp => grp.Id == mesaj.GroupId)
                            .First();

                return View(grp);
            }
        }

        // formular pentru crearea grupului
        public IActionResult New(Group grup)
        {
            if (ModelState.IsValid)
            {
                dbc.Groups.Add(grup);
                dbc.SaveChanges();
                TempData["message"] = "Group created successfully!";
                return RedirectToAction("Index");
            }
            return View(grup);
        }


        // editarea unui grup
        public IActionResult Edit(int id)
        {
            Group grup = dbc.Groups.Find(id);
            return View(grup);
        }

        [HttpPost]
        public IActionResult Edit(int id, Group requestGroup)
        {
            Group grup = dbc.Groups.Find(id);
            if (ModelState.IsValid)
            {
                grup.GroupName = requestGroup.GroupName;
                grup.Description = requestGroup.Description;
                dbc.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                return View(requestGroup);
            }
        }

        // stergerea unui grup
        [HttpPost]
        public IActionResult Delete(int id)
        {
            Group grup = dbc.Groups.Find(id);
            dbc.Groups.Remove(grup);
            dbc.SaveChanges();
            TempData["message"] = "Group deleted successfully!";
            return RedirectToAction("Index");
        }
    }
}
