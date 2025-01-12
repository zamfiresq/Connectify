using Microsoft.AspNetCore.Mvc;
using Connectify.Models;
using System.Linq;
using Connectify.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Connectify.Controllers
{
    public class ApplicationUserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ApplicationUserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)

        {
            _context = context;
            _userManager = userManager;
        }





        [Authorize]
        public IActionResult Edit(string id)
        {
            var currentUserId = _userManager.GetUserId(User);
            var user = _context.Users.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            //verificam daca userul logat este admin sau daca este cel care are profilul actual
            var isAdmin = User.IsInRole("Admin"); // verificare admin
            if (user.Id != currentUserId && !isAdmin)
            {
                return Forbid(); // forbidden
            }

            // model de editare pentru utilizator
            var editModel = new ApplicationUserEdit
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Bio = user.Bio,
                IsPrivate = user.IsPrivate
            };

            return View(editModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ApplicationUserEdit model)
        {
            var currentUserId = _userManager.GetUserId(User);
            var user = _context.Users.FirstOrDefault(u => u.Id == model.Id);

            if (user == null)
            {
                return NotFound();
            }

            // verificam daca userul logat este admin sau daca este cel care are profilul actual
            var isAdmin = User.IsInRole("Admin");
            if (user.Id != currentUserId && !isAdmin)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // dam update la datele userului
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Bio = model.Bio;
            user.IsPrivate = model.IsPrivate;

            // salvare in db
            _context.SaveChanges();

            return RedirectToAction("Show", new { id = user.Id });
        }


        // search action
        public IActionResult Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return PartialView("UserSearchResults", new List<ApplicationUser>());

            var users = _context.Users
                .Where(u => u.FirstName.Contains(query) || u.LastName.Contains(query))
                .OrderBy(u => u.FirstName)
                .Take(10)
                .ToList();

            return PartialView("UserSearchResults", users);
        }

        // show - afisarea profilului unui user
        public IActionResult Show(string id)
        {
            var user = _context.Users
                .Include(u => u.Posts)
                    .ThenInclude(p => p.Comments)
                .Include(u => u.Followers) // urmaritori
                .Include(u => u.Following) // cei pe care ii urmaresc
                .FirstOrDefault(u => u.Id == id);

            // verificam daca userul exista
            if (user == null)
            {
                return NotFound(); 
            }

            // user curent 
            var currentUserId = _userManager.GetUserId(User);

            // statusul urmaririi
            var followRequest = _context.FollowRequests
                .FirstOrDefault(r => r.SenderId == currentUserId && r.ReceiverId == id);

            // setare in viewbag
            ViewBag.IsFollowing = followRequest != null && followRequest.IsAccepted;
            ViewBag.IsPending = followRequest != null && !followRequest.IsAccepted;

            // daca userul curent este acelasi cu cel al profilului
            ViewBag.IsCurrentUser = currentUserId == id;

            // calculare nr de urmaritori si urmariti
            ViewBag.FollowersCount = _context.FollowRequests.Count(r => r.ReceiverId == id && r.IsAccepted);
            ViewBag.FollowingCount = _context.FollowRequests.Count(r => r.SenderId == id && r.IsAccepted);

            return View(user);
        }



        // FOLLOW / UNFOLLOW ACTIONS
        // trimiterea unei cereri de urmarire
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult SendFollowRequest(string receiverId)
        {
            var senderId = _userManager.GetUserId(User);

            // nu trebuie sa permitem sa ne urmarim pe noi insine
            if (senderId == receiverId)
            {
                TempData["message"] = "You cannot follow yourself!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            // daca exista deja o cerere de urmarire neacceptata
            var existingRequest = _context.FollowRequests
                .FirstOrDefault(r => r.SenderId == senderId && r.ReceiverId == receiverId && !r.IsAccepted);

            if (existingRequest != null)
            {
                TempData["message"] = "Follow request already sent!";
                TempData["messageType"] = "alert-warning";
                return RedirectToAction("Show", new { id = receiverId });
            }

            // cream o noua cerere de urmarire
            var followRequest = new FollowRequest
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                IsAccepted = false
            };

            _context.FollowRequests.Add(followRequest);
            _context.SaveChanges();

            TempData["message"] = "Follow request sent successfully!";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("Show", new { id = receiverId });
        }



        // acceptarea unei cereri de urmarire
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult AcceptFollowRequest(int requestId)
        {
            var currentUserId = _userManager.GetUserId(User);

            // gasim cererea de urmarire
            var followRequest = _context.FollowRequests
                .FirstOrDefault(r => r.Id == requestId && r.ReceiverId == currentUserId && !r.IsAccepted);

            if (followRequest == null)
            {
                TempData["message"] = "Follow request not found or already accepted.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("FollowRequests");
            }

            // acceptam cererea
            followRequest.IsAccepted = true;
            _context.SaveChanges();

            TempData["message"] = "Follow request accepted!";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("FollowRequests");
        }



        // refuzarea unei cereri de urmarire
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult DeclineFollowRequest(int requestId)
        {
            var currentUserId = _userManager.GetUserId(User);

            var followRequest = _context.FollowRequests
                .FirstOrDefault(r => r.Id == requestId && r.ReceiverId == currentUserId);

            if (followRequest == null)
            {
                TempData["message"] = "Follow request not found.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("FollowRequests");
            }

            // stergem cererea
            _context.FollowRequests.Remove(followRequest);
            _context.SaveChanges();

            TempData["message"] = "Follow request declined.";
            TempData["messageType"] = "alert-warning";

            return RedirectToAction("FollowRequests");
        }


        // afisarea listei cu cererile de urmarire
        [Authorize(Roles = "User,Admin")]
        public IActionResult FollowRequests()
        {
            var currentUserId = _userManager.GetUserId(User);

            var requests = _context.FollowRequests
                .Include(r => r.Sender)
                .Where(r => r.ReceiverId == currentUserId && !r.IsAccepted)
                .ToList();

            return View(requests);
        }


        // posibilitatea de a da cancel la request 
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult CancelFollowRequest(int requestId)
        {
            var currentUserId = _userManager.GetUserId(User);

            var followRequest = _context.FollowRequests
                .FirstOrDefault(r => r.Id == requestId && r.SenderId == currentUserId && !r.IsAccepted);

            // daca nu gasim cererea sau aceasta a fost deja acceptata
            if (followRequest == null)
            {
                TempData["message"] = "Follow request not found or already processed.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            _context.FollowRequests.Remove(followRequest);
            _context.SaveChanges();

            TempData["message"] = "Follow request canceled.";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Index");
        }

        // indexul utilizatorilor
        public IActionResult Index()
        {
            var currentUserId = _userManager.GetUserId(User);

            // lista cu toti userii, cu exceptia celui curent
            var users = _context.Users
                .Where(u => u.Id != currentUserId)
                .ToList();

            // userii pe care ii urmareste userul curent
            var following = _context.FollowRequests
                .Where(fr => fr.SenderId == currentUserId && fr.IsAccepted)
                .Select(fr => fr.ReceiverId)
                .ToList();

            ViewBag.Following = following;
            return View(users);
        }


    }
}

