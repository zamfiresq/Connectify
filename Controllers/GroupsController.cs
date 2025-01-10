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
        private bool IsUserInGroup(int groupId, string userId)
        {
            var inGroup = dbc.UserGroups.Any(ug => ug.GroupId == groupId && ug.UserId == userId);
            Console.WriteLine($"IsUserInGroup result: {inGroup} for groupId={groupId}, userId={userId}");
            return inGroup;
        }



        // show - afisarea unui grup dupa id cu mesajele asociate
        [Authorize(Roles = "User,Admin")]
        public ActionResult Show(int id)
        {
            var currentUserId = _userManager.GetUserId(User);

            // Adminul sau creatorul grupului sunt adăugați automat ca membri
            if (User.IsInRole("Admin") || dbc.Groups.Any(g => g.Id == id && g.UserId == currentUserId))
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

                    // Adaugă adminul ca membru acceptat dacă nu este deja
                    if (User.IsInRole("Admin") && !group.UserGroups.Any(ug => ug.UserId == currentUserId))
                    {
                        var adminMembership = new UserGroup
                        {
                            UserId = currentUserId,
                            GroupId = group.Id,
                            IsAccepted = true
                        };

                        dbc.UserGroups.Add(adminMembership);
                        dbc.SaveChanges();
                    }

                    return View(group);
                }
                else
                {
                    return StatusCode(404); // Grupul nu există
                }
            }

            // Pentru utilizatori obișnuiți, verificăm dacă sunt acceptați
            var userGroup = dbc.UserGroups.FirstOrDefault(ug => ug.GroupId == id && ug.UserId == currentUserId);

            if (userGroup != null && userGroup.IsAccepted)
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
                    return View(group);
                }
                else
                {
                    return StatusCode(404); // Grupul nu există
                }
            }
            else if (userGroup != null && !userGroup.IsAccepted)
            {
                TempData["message"] = "Your request to join the group is pending approval.";
                TempData["messageType"] = "alert-warning";
                return RedirectToAction("Index");
            }
            else
            {
                return StatusCode(403); // Acces interzis
            }
        }




        // show - afisarea unui grup dupa id cu mesajele asociate
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Show(int id, [FromForm] Message message)
        {
            var currentUserId = _userManager.GetUserId(User); // Id-ul utilizatorului curent

            // Verificăm dacă utilizatorul are permisiunea de a adăuga un mesaj
            if (User.IsInRole("Admin") || IsUserInGroup(id, currentUserId))
            {
                message.SentAt = DateTime.Now;
                message.GroupId = id;
                message.UserId = currentUserId;

                message.Id = 0; // Pentru a evita eroarea de duplicate key

                if (ModelState.IsValid)
                {
                    dbc.Messages.Add(message);
                    dbc.SaveChanges();
                    TempData["message"] = "Message added successfully!";
                    TempData["messageType"] = "alert-success";
                    return RedirectToAction("Show", new { id = message.GroupId });
                }
                else
                {
                    // În cazul în care modelul nu este valid, reîncărcăm grupul
                    Group group = dbc.Groups
                        .Include(g => g.Messages)
                        .ThenInclude(m => m.User)
                        .Include(g => g.UserGroups)
                        .ThenInclude(ug => ug.User)
                        .FirstOrDefault(g => g.Id == id);

                    if (group == null)
                    {
                        TempData["message"] = "Group not found!";
                        TempData["messageType"] = "alert-danger";
                        return RedirectToAction("Index");
                    }

                    return View(group);
                }
            }
            else
            {
                return StatusCode(403); // Acces interzis
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

            // Verificăm dacă este admin sau creator
            var group = dbc.Groups.Include(g => g.UserGroups).FirstOrDefault(g => g.Id == id);
            if (group == null)
            {
                TempData["message"] = "Group not found!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            if (User.IsInRole("Admin") || group.UserId == currentUserId)
            {
                TempData["message"] = "You are already a member of this group!";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Show", new { id });
            }

            // Dacă utilizatorul nu este deja în pending sau acceptat
            if (!group.UserGroups.Any(ug => ug.UserId == currentUserId))
            {
                var userGroup = new UserGroup
                {
                    UserId = currentUserId,
                    GroupId = id,
                    IsAccepted = false
                };
                dbc.UserGroups.Add(userGroup);
                dbc.SaveChanges();

                TempData["message"] = "Your request has been sent!";
                TempData["messageType"] = "alert-info";
            }
            else
            {
                TempData["message"] = "You are already in this group or your request is pending.";
                TempData["messageType"] = "alert-warning";
            }

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
