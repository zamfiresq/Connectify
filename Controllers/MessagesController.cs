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
            var message = dbc.Messages.Find(id);
            if (message == null)
            {
                TempData["message"] = "Message not found!";
                return RedirectToAction("Index");
            }

            dbc.Messages.Remove(message);
            dbc.SaveChanges();
            TempData["message"] = "Message deleted successfully!";
            return Redirect("/Groups/Show/" + message.GroupId);
        }



        // editarea intr-o pagina separata de view
        public IActionResult Edit(int id)
        {
            var message = dbc.Messages.Find(id);

            return View(message);
        }

        [HttpPost]
        public IActionResult Edit(int id, Message updatedMessage)
        {
            var message = dbc.Messages.Find(id);

            if(ModelState.IsValid)
            {
                message.TextMessage = updatedMessage.TextMessage;
                dbc.SaveChanges();
                return Redirect("/Groups/Show/" + message.GroupId);
            }
            else
            {
                return View(updatedMessage);
            }
        }

    }
}

