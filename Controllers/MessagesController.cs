using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Connectify.Data;
using Connectify.Models;
using Microsoft.AspNetCore.Identity;

namespace Connectify.Controllers
{
    public class MessagesController : Controller
    {

        private readonly ApplicationDbContext dbc;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public MessagesController(
            ApplicationDbContext dbc,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
        )
        {
            this.dbc = dbc;
            _userManager = userManager;
            _roleManager = roleManager;
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
            ViewBag.Message = message;
            return View(message);
        }

        [HttpPost]
        public IActionResult Edit(int id, Message updatedMessage)
        {
            var message = dbc.Messages.Find(id);

            try
            {
                message.TextMessage = updatedMessage.TextMessage;
                dbc.SaveChanges();
                TempData["message"] = "Message updated successfully!";
                return Redirect("/Groups/Show/" + message.GroupId);
            }
            catch (Exception)
            {
                TempData["message"] = "Message not found!";
                return Redirect("/Groups/Show/" + message.GroupId);
            }
        }

        // crearea unui mesaj nou
        public IActionResult New()
        {
            return View();
        }


        [HttpPost]
        public IActionResult New(Message message)
        {
            message.SentAt = DateTime.Now;

            try
            {
                dbc.Messages.Add(message);
                dbc.SaveChanges();
                return Redirect("/Groups/Show/" + message.GroupId);
            }
            catch (Exception)
            {
                return Redirect("/Groups/Show/" + message.GroupId);
            }
        }


    }
}

