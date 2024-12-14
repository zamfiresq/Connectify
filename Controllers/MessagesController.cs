using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Connectify.Data;
using Connectify.Models;

namespace Connectify.Controllers
{
    public class MessagesController : Controller
    {

        private readonly ApplicationDbContext dbc;

        public MessagesController(ApplicationDbContext dbc)
        {
            this.dbc = dbc;
        }


        // stergerea mesajului
        [HttpPost]
        public IActionResult Delete(int id)
        {
            Message mesaj = dbc.Messages.Find(id);

            // mesajul se poate sterge de catre admin sau de catre userul care apartine acelui grup
            dbc.Messages.Remove(mesaj);
            dbc.SaveChanges();
            TempData["message"] = "Your message was successfully deleted!";
            return Redirect("/Groups/Show/" +  mesaj.GroupId);
        }


        // editarea intr-o pagina separata de view
        public IActionResult Edit(int id)
        {
            Message mesaj = dbc.Messages.Find(id);

            // adminul / userul care a trimis mesajul il poate edita
            return View(mesaj);
        }

        [HttpPost]
        public IActionResult Edit(int id, Message cerereMesaj)
        {
            Message mesaj = dbc.Messages.Find(id);

            // verificare roluri
            if (ModelState.IsValid)
            {
                mesaj.TextMessage = cerereMesaj.TextMessage;
                dbc.SaveChanges();
                return Redirect("/Groups/Show" + mesaj.GroupId);
            }

            return View(cerereMesaj);
        }
    }
}
