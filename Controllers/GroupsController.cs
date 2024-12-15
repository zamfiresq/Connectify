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
            var group = dbc.Groups.Include("Messages")
                .Where(g => g.Id == id)
                .FirstOrDefault();

            return View(group);
        }


        // show - pentru mesajele unui grup
        [HttpPost]
        public IActionResult Show([FromForm] Message mesaj)
        {
            mesaj.SentAt = DateTime.Now;

           if (ModelState.IsValid)
            {
                dbc.Messages.Add(mesaj);
                dbc.SaveChanges();
                TempData["message"] = "Message sent successfully!";
                return Redirect("/Groups/Show/" + mesaj.GroupId);
            }
            else
            {
                var group = dbc.Groups.Include("Messages")
                    .Where(g => g.Id == mesaj.GroupId)
                    .FirstOrDefault();

                return View(group);
            }
        }

        // formular pentru crearea grupului
        public IActionResult New()
        {
           Group newGroup = new Group();

            return View(newGroup);
        }

        // se adauga un grup nou in baza de date
        [HttpPost]
        public IActionResult New(Group grup)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    dbc.Groups.Add(grup);
                    dbc.SaveChanges();
                    TempData["message"] = "Group created successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    return View(grup);
                }
            }
            catch (Exception)
            {
                return View(grup);
            }
        }


        // editarea unui grup
        public IActionResult Edit(int id)
        {
            var grup = dbc.Groups.Find(id);

            return View(grup);
        }

        [HttpPost]
        public IActionResult Edit(int id, Group requestGroup)
        {
            var grup = dbc.Groups.Find(id);
            if (grup == null)
            {
                TempData["message"] = "Group not found!";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                grup.GroupName = requestGroup.GroupName;
                grup.Description = requestGroup.Description;
                dbc.SaveChanges();
                TempData["message"] = "Group edited successfully!";
                return RedirectToAction("Index");
            }

            return View(requestGroup); 
        }


        // stergerea unui grup
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var grup = dbc.Groups.Find(id);

            if (grup == null)
            {
                TempData["message"] = "Group not found!";
                return RedirectToAction("Index");
            }

            dbc.Groups.Remove(grup);
            dbc.SaveChanges();
            TempData["message"] = "Group deleted successfully!";
            return RedirectToAction("Index");
        }
    }
}
