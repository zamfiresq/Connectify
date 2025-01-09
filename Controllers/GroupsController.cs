using Connectify.Data;
using Connectify.Models;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize(Roles = "User,Admin")]
        public IActionResult Index()
        {
            // preluarea tuturor grupurilor
            var groups = dbc.Groups.Include(g=>g.UserGroups).ToList();
            var currentUserId = _userManager.GetUserId(User);

            // grupurile din care face parte utilizatorul curent
            var userGroups = dbc.UserGroups
                .Where(ug => ug.UserId == currentUserId)
                .Select(ug => ug.GroupId)
                .ToList();

            // adaugarea datelor in ViewBag
            ViewBag.UserGroups = userGroups.Where(id => id.HasValue)
                .Select(id => id.Value)
                .ToList();                            // id-urile grupurilor la care userul are acces
            ViewBag.CurrentUserId = currentUserId;     
            ViewBag.Groups = groups;                  // toate grupurile

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
            }

            return View();
        }



        // functie de verificare daca un user face parte dintr-un grup
        public bool IsUserInGroup(int groupId, string userId)
        {
            return dbc.UserGroups.Any(ug => ug.GroupId == groupId && ug.UserId == userId);
        }


        // show - afisarea unui grup dupa id cu mesajele asociate
        [Authorize(Roles = "User,Admin")]
        public ActionResult Show(int id)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (User.IsInRole("Admin") || IsUserInGroup(id, currentUserId))
            {
                var group = dbc.Groups
                    .Include(g => g.Messages)
                    .Include(g => g.UserGroups)
                    .ThenInclude(ug => ug.User)
                    .FirstOrDefault(g => g.Id == id);

                if (group != null)
                {
                    ViewBag.Group = group;
                    ViewBag.CurrentUserId = currentUserId;

                    // grupurile din care face parte utilizatorul curent
                    ViewBag.UserGroups = dbc.UserGroups
                        .Where(ug => ug.UserId == currentUserId)
                        .Select(ug => ug.GroupId.Value) 
                        .ToList();

                    return View(group);
                }
                else
                {
                    return StatusCode(404); // not found
                }
            }
            else
            {
                return StatusCode(403); // denied
            }
        }


        // show - afisarea unui grup dupa id cu mesajele asociate
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Show(int id, [FromForm] Message message)
        {
            var currentUserId = _userManager.GetUserId(User); // id-ul utilizatorului curent pentru a-l putea folosi in view
            // daca userul e admin sau face parte din grupul respectiv
            if (User.IsInRole("Admin") || IsUserInGroup(id, currentUserId))
            {
                message.SentAt = DateTime.Now;
                message.GroupId = id;
                message.UserId = currentUserId;

                // daca mesajul este valid
                if (ModelState.IsValid)
                {
                    dbc.Messages.Add(message);
                    dbc.SaveChanges();
                    TempData["message"] = "Message added successfully!";
                    return Redirect("/Groups/Show/" + message.GroupId);
                }
                else
                {
                    Group group = dbc.Groups.Include("Messages")
                        .Include(Message => Message.User)
                        .Where(g => g.Id == id)
                        .FirstOrDefault(g => g.Id == id);

                    return View(group);
                }
            }
            else
            {
                return StatusCode(403); // acces interzis
            }
        }



        // New - formular pentru crearea unui grup
        [Authorize(Roles = "User,Admin")]
        public IActionResult New()
        {
            Group group = new Group();
            return View(group);
        }



        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult New(Group group)
        {
            // userul care creeaza grupul va fi salvat in baza de date
            var currentUserId = _userManager.GetUserId(User);
            group.UserId = currentUserId;

            try
            {
                group.GroupName = group.GroupName;
                group.Description = group.Description;

                dbc.Groups.Add(group);
                dbc.SaveChanges();

                // crearea unei inregistrari in UserGroup pentru utilizatorul curent
                var userGroup = new UserGroup
                {
                    UserId = currentUserId,
                    GroupId = group.Id
                };

                dbc.UserGroups.Add(userGroup);
                dbc.SaveChanges();

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return View();
            }
        }


        // edit
        [Authorize(Roles = "User,Admin")]
        public IActionResult Edit(int id)
        {
            Group? group = dbc.Groups.Where(g => g.Id == id).FirstOrDefault(); // cautam grupul dupa id

            // daca userul e admin sau a creat grupul respectiv, poate sa-l editeze
            if (User.IsInRole("Admin") || group?.UserId == _userManager.GetUserId(User))
            {
                if (group != null)
                {
                    return View(group);
                }
                else
                {
                    return StatusCode(404); // not found
                }
            }
            else
            {
                return StatusCode(403); // acces interzis
            }
        }

        // formularul de editare
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Edit(int id, Group requestGroup)
        {
                Group? group = dbc.Groups.Find(id);

                if (group != null)
                {
                    if (User.IsInRole("Admin") || group.UserId == _userManager.GetUserId(User))
                    {
                        group.GroupName = requestGroup.GroupName;
                        group.Description = requestGroup.Description;

                        dbc.SaveChanges();
                        TempData["message"] = "Group updated successfully!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        return StatusCode(403); // forbidden
                }
                }
                return StatusCode(404); // not found
        }

            // delete - stergerea unui grup
            // adminul si cel care a creat grupul pot sa-l stearga
            [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public ActionResult Delete(int id)
        {
            Group? group = dbc.Groups.Include(g => g.Messages)
                        .FirstOrDefault(g => g.Id == id);

            // daca userul e admin sau a creat grupul respectiv, poate sa-l stearga
            if (User.IsInRole("Admin") || group?.UserId == _userManager.GetUserId(User))
            {
                if (group != null)
                {
                    dbc.Groups.Remove(group);
                    dbc.SaveChanges();
                    TempData["message"] = "Group deleted successfully!";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index");
                }
                else
                {
                    return StatusCode(404); // not found
                }
            }
            else
            {
                return StatusCode(403); // acces interzis
            }
        }



        // FUNCTIONALITATI SPECIFICE 
        // join group
        [Authorize(Roles = "User,Admin")]
        public IActionResult JoinGroup(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            // daca userul nu face parte din grup
            if (!IsUserInGroup(id, currentUserId))
            {
                var userGroup = new UserGroup
                {
                    UserId = currentUserId,
                    GroupId = id
                };
                dbc.UserGroups.Add(userGroup);
                dbc.SaveChanges();
                TempData["message"] = "You have joined the group!";
                TempData["messageType"] = "alert-success";
            }

            else
            {
                TempData["message"] = "You are already in this group!";
                TempData["messageType"] = "alert-danger";
            }

            return RedirectToAction("Index");
        }


        // leave group
        [Authorize(Roles = "User,Admin")]
        public IActionResult LeaveGroup(int id)
        {
            var currentUserId = _userManager.GetUserId(User);

            var userGroup = dbc.UserGroups
                .FirstOrDefault(ug => ug.GroupId == id && ug.UserId == currentUserId);

            if (userGroup != null)
            {
                dbc.UserGroups.Remove(userGroup);
                dbc.SaveChanges();

                TempData["message"] = "You have successfully left the group.";
                TempData["messageType"] = "alert-warning";
            }
            else
            {
                TempData["message"] = "You are not part of this group.";
                TempData["messageType"] = "alert-danger";
            }

            return RedirectToAction("Index");
        }


        // remove user from group
        [Authorize(Roles = "User,Admin")]
        public IActionResult RemoveMember(int groupId, string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var group = dbc.Groups.Include(g => g.UserGroups).FirstOrDefault(g => g.Id == groupId);

            if (group == null)
            {
                return StatusCode(404); // grupul nu a fost gasit
            }

            if (User.IsInRole("Admin") || group.UserId == currentUserId)
            {
                var userGroup = dbc.UserGroups
                    .FirstOrDefault(ug => ug.GroupId == groupId && ug.UserId == userId);

                if (userGroup != null)
                {
                    dbc.UserGroups.Remove(userGroup);
                    dbc.SaveChanges();

                    TempData["message"] = "Member has been removed successfully!";
                    TempData["messageType"] = "alert-success";
                }
                else
                {
                    TempData["message"] = "User is not part of this group.";
                    TempData["messageType"] = "alert-danger";
                }

                return RedirectToAction("Show", new { id = groupId });
            }

            // validare pt cazul in care userul care incearca sa paraseasca grupul este singurul moderator

            return StatusCode(403); // acces interzis
        }

    }
}
