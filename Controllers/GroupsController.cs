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
        public IActionResult Show(int id, [FromForm] Message mesaj)
        {
            // conditie pentru rolul userului

            // daca este valid
            if (ModelState.IsValid)
            {
                // adaugare mesaj in grup
                mesaj.GroupId = id;
                mesaj.SentAt = DateTime.Now;
                dbc.Messages.Add(mesaj);
                dbc.SaveChanges();

                // redirect catre pagina grupului
                return Redirect("/Groups/Show/" + mesaj.GroupId);
            }

            else
            {
                // daca nu este valid
                Group group = dbc.Groups.Include("Messages")
                        .Where(g => g.Id == id)
                        .First();
                return View(group);

            }

            // tempdata - pentru a afisa mesajul de eroare 
            TempData["message"] = "You're not allowed to send messages here!";
            return RedirectToAction("Index");
        }
    }
}
