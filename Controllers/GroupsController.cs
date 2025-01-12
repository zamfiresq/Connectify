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
            ViewBag.UserGroups = userGroups
                .Select(id => id)
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
        private bool IsUserInGroup(int groupId, string userId)
        {
            return dbc.UserGroups.Any(ug => ug.GroupId == groupId && ug.UserId == userId && ug.IsAccepted);
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
                    .ThenInclude(m => m.User)
                    .Include(g => g.UserGroups)
                    .ThenInclude(ug => ug.User)
                    .FirstOrDefault(g => g.Id == id);

                if (group != null)
                {
                    ViewBag.Group = group;
                    ViewBag.CurrentUserId = currentUserId;

                    // Grupurile unde utilizatorul este acceptat
                    ViewBag.UserGroups = dbc.UserGroups
                        .Where(ug => ug.UserId == currentUserId && ug.IsAccepted)
                        .Select(ug => ug.GroupId)
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
            var currentUserId = _userManager.GetUserId(User);

            if (User.IsInRole("Admin") || IsUserInGroup(id, currentUserId))
            {
                message.SentAt = DateTime.Now;
                message.GroupId = id;
                message.UserId = currentUserId;

                if (string.IsNullOrWhiteSpace(message.TextMessage))
                {
                    ModelState.AddModelError("TextMessage", "The message content cannot be empty.");
                }

                if (ModelState.IsValid)
                {
                    dbc.Messages.Add(message);
                    dbc.SaveChanges();

                    TempData["message"] = "Message added successfully!";
                    TempData["messageType"] = "alert-success";

                    return RedirectToAction("Show", new { id });
                }
                else
                {
                    TempData["message"] = "Message could not be added!";
                    TempData["messageType"] = "alert-danger";

                    // Load group details for re-rendering the view
                    var group = dbc.Groups
                        .Include(g => g.Messages)
                        .ThenInclude(m => m.User)
                        .Include(g => g.UserGroups)
                        .ThenInclude(ug => ug.User)
                        .FirstOrDefault(g => g.Id == id);

                    if (group == null)
                    {
                        return StatusCode(404); // not found
                    }

                    ViewBag.Group = group;
                    ViewBag.CurrentUserId = currentUserId;

                    return View(group);
                }
            }
            else
            {
                return StatusCode(403); // access denied
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
            var currentUserId = _userManager.GetUserId(User);
            group.UserId = currentUserId; // Creatorul devine moderator

            try
            {
                group.GroupName = group.GroupName;
                group.Description = group.Description;

                dbc.Groups.Add(group);
                dbc.SaveChanges();

                // Adăugarea utilizatorului curent în grup ca membru acceptat
                var userGroup = new UserGroup
                {
                    UserId = currentUserId,
                    GroupId = group.Id,
                    IsAccepted = true // Automat acceptat
                };

                dbc.UserGroups.Add(userGroup);
                dbc.SaveChanges();

                TempData["message"] = "Group created successfully!";
                TempData["messageType"] = "alert-success";

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["message"] = "There was an error creating the group.";
                TempData["messageType"] = "alert-danger";
                return View(group);
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

            // Load group with members
            var group = dbc.Groups.Include(g => g.UserGroups).FirstOrDefault(g => g.Id == id);
            if (group == null)
            {
                TempData["message"] = "Group not found!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            // Check if the user is already a member or has a pending request
            var userGroup = group.UserGroups?.FirstOrDefault(ug => ug.UserId == currentUserId);

            if (userGroup != null)
            {
                if (userGroup.IsAccepted)
                {
                    TempData["message"] = "You are already a member of this group!";
                    TempData["messageType"] = "alert-success";
                }
                else
                {
                    TempData["message"] = "Your join request is already pending!";
                    TempData["messageType"] = "alert-warning";
                }
                return RedirectToAction("Show", new { id });
            }

            // Create a new join request
            dbc.UserGroups.Add(new UserGroup
            {
                UserId = currentUserId,
                GroupId = id,
                IsAccepted = false
            });
            dbc.SaveChanges();

            TempData["message"] = "Your request to join has been sent!";
            TempData["messageType"] = "alert-info";

            return RedirectToAction("Index");
        }




        // leave group
        [Authorize(Roles = "User,Admin")]
        public IActionResult LeaveGroup(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            var group = dbc.Groups.Include(g => g.UserGroups).FirstOrDefault(g => g.Id == id);

            if (group != null)
            {
                var userGroup = group.UserGroups.FirstOrDefault(ug => ug.UserId == currentUserId);

                // Dacă utilizatorul este moderator și singurul membru acceptat
                if (group.UserId == currentUserId && group.UserGroups.Count(ug => ug.IsAccepted) == 1)
                {
                    TempData["message"] = "You cannot leave the group as you are the only member! You can only delete it.";
                    TempData["messageType"] = "alert-warning";
                    return RedirectToAction("Show", new { id = group.Id });
                }

                // Permite părăsirea grupului pentru alți membri
                if (userGroup != null)
                {
                    dbc.UserGroups.Remove(userGroup);
                    dbc.SaveChanges();
                    TempData["message"] = "You have left the group!";
                    TempData["messageType"] = "alert-success";
                }
            }
            else
            {
                TempData["message"] = "Group not found!";
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



        // metoda pentru acceptarea cererii de intrare in grup
        [Authorize(Roles = "User,Admin")]
        public IActionResult ApproveUser(int groupId, string userId)
        {
            var userGroup = dbc.UserGroups.FirstOrDefault(ug => ug.GroupId == groupId && ug.UserId == userId);

            if (userGroup != null)
            {
                userGroup.IsAccepted = true;
                dbc.SaveChanges();
                TempData["message"] = "User has been approved!";
                TempData["messageType"] = "alert-success";
            }
            else
            {
                TempData["message"] = "User not found!";
                TempData["messageType"] = "alert-danger";
            }

            return RedirectToAction("Show", new { id = groupId });
        }

        // metoda pentru respingerea cererii de intrare in grup
        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        public IActionResult DeclineUser(int groupId, string userId)
        {
            var userGroup = dbc.UserGroups.FirstOrDefault(ug => ug.GroupId == groupId && ug.UserId == userId);

            if (userGroup != null && !userGroup.IsAccepted)
            {
                dbc.UserGroups.Remove(userGroup);
                dbc.SaveChanges();
                TempData["message"] = "Request has been declined.";
                TempData["messageType"] = "alert-danger";
            }
            else
            {
                TempData["message"] = "Request not found or already approved.";
                TempData["messageType"] = "alert-warning";
            }

            return RedirectToAction("Show", new { id = groupId });
        }


    }
}
