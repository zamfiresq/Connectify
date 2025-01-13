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
            var groups = dbc.Groups.Include(g => g.UserGroups).ToList();
            var currentUserId = _userManager.GetUserId(User);

            // grupurile din care face parte utilizatorul curent
            var userGroups = dbc.UserGroups
                .Where(ug => ug.UserId == currentUserId && ug.IsAccepted) // doar grupuri unde userul este membru aprobat
                .Select(ug => ug.GroupId)
                .ToList();

            // grupurile in care utilizatorul are cereri pending
            var pendingGroups = dbc.UserGroups
                .Where(ug => ug.UserId == currentUserId && !ug.IsAccepted) // doar cereri pending
                .Select(ug => ug.GroupId)
                .ToList();

            // adaugarea datelor in ViewBag
            ViewBag.UserGroups = userGroups;          
            ViewBag.PendingGroups = pendingGroups;    // id-urile grupurilor la care userul are cereri pending
            ViewBag.CurrentUserId = currentUserId;    
            ViewBag.Groups = groups;                  

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
                // cautam grupul cu mesajele si membrii asociati
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

                    // grupurile unde userul este acceptat
                    ViewBag.UserGroups = dbc.UserGroups
                        .Where(ug => ug.UserId == currentUserId && ug.IsAccepted)
                        .Select(ug => ug.GroupId)
                        .ToList();

                    return View(group);
                }
                else
                {
                    // Log the error for debugging
                    Console.WriteLine($"Group with ID {id} not found.");
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

                message.Id = 0; // pentru a evita eroarea de duplicate key

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
                    Group group = dbc.Groups.Include(g => g.Messages)
                        .ThenInclude(m => m.User)
                        .Include(g => g.UserGroups)
                        .ThenInclude(ug => ug.User)
                        .FirstOrDefault(g => g.Id == id);

                    if (group == null)
                    {
                        return StatusCode(404); // not found
                    }

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
            var currentUserId = _userManager.GetUserId(User);
            group.UserId = currentUserId; // userul devine moderatorul grupului

            try
            {
                group.GroupName = group.GroupName;
                group.Description = group.Description;

                dbc.Groups.Add(group);
                dbc.SaveChanges();

                // adaugarea userului curent in grup ca membru
                var userGroup = new UserGroup
                {
                    UserId = currentUserId,
                    GroupId = group.Id,
                    IsAccepted = true // automat acceptat
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
                        return StatusCode(403);
                }
                }
                return StatusCode(404); 
        }

        // delete - stergerea unui grup
        // adminul si cel care a creat grupul pot sa-l stearga
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public ActionResult Delete(int id)
        {
            Group? group = dbc.Groups
                        .Include(g => g.Messages)
                        .Include(g => g.UserGroups)
                        .FirstOrDefault(g => g.Id == id);

            if (group == null)
            {
                TempData["message"] = "Group not found!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            // verificam daca userul curent este admin sau creatorul grupului
            if (User.IsInRole("Admin") || group.UserId == _userManager.GetUserId(User))
            {
                // conditia pentru a nu permite stergerea daca mai sunt membri
                if (group.UserGroups != null && group.UserGroups.Count > 1)
                {
                    TempData["message"] = "Group cannot be deleted because it still has members.";
                    TempData["messageType"] = "alert-warning";
                    return RedirectToAction("Show", new { id });
                }

                // daca nu mai sunt membri, stergem grupul
                dbc.Groups.Remove(group);
                dbc.SaveChanges();

                TempData["message"] = "Group deleted successfully!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
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

            // grupul cu membrii asociati
            var group = dbc.Groups.Include(g => g.UserGroups).FirstOrDefault(g => g.Id == id);
            if (group == null)
            {
                TempData["message"] = "Group not found!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            // verificam daca userul curent este deja membru al grupului
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

            // verificam daca utilizatorul este admin sau group creator
            var isAdmin = User.IsInRole("Admin");
            var isCreator = group.UserId == currentUserId;

            // adaugare directa ca membru daca este Admin
            if (isAdmin || isCreator)
            {
                dbc.UserGroups.Add(new UserGroup
                {
                    UserId = currentUserId,
                    GroupId = id,
                    IsAccepted = true // direct aprobat
                });
                dbc.SaveChanges();

                TempData["message"] = "You have been added to the group as a member!";
                TempData["messageType"] = "alert-success";

                return RedirectToAction("Show", new { id });
            }

            // crearea unei cereri de intrare in grup pentru utilizatorii normali
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

            if (group == null)
            {
                TempData["message"] = "Group not found!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            var userGroup = group.UserGroups.FirstOrDefault(ug => ug.UserId == currentUserId);

            // daca userul curent este singurul membru al grupului, nu-l poate parasi
            if (group.UserGroups.Count == 1 && userGroup != null)
            {
                TempData["message"] = "You are the only member of this group! You cannot leave, but you can delete the group.";
                TempData["messageType"] = "alert-warning";
                return RedirectToAction("Show", new { id });
            }

            if (userGroup != null)
            {
                dbc.UserGroups.Remove(userGroup);
                dbc.SaveChanges();
                TempData["message"] = "You have left the group!";
                TempData["messageType"] = "alert-success";
            }
            else
            {
                TempData["message"] = "You are not a member of this group!";
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

            // daca userul este admin sau cel care a creat grupul, poate sa stearga un membru
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
