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





        [Authorize] // Ensure the user is logged in
        public IActionResult Edit(string id)
        {
            // Get the currently logged-in user's ID
            var currentUserId = _userManager.GetUserId(User);

            // Find the user by id
            var user = _context.Users.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Check if the logged-in user is an admin or the owner of the profile
            var isAdmin = User.IsInRole("Admin"); // Check if the logged-in user is an admin
            if (user.Id != currentUserId && !isAdmin)
            {
                return Forbid(); // Return 403 Forbidden if unauthorized
            }

            // Map user to a ViewModel
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
        [Authorize] // Ensure the user is logged in
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ApplicationUserEdit model)
        {
            // Get the currently logged-in user's ID
            var currentUserId = _userManager.GetUserId(User);

            // Find the user by id
            var user = _context.Users.FirstOrDefault(u => u.Id == model.Id);

            if (user == null)
            {
                return NotFound();
            }

            // Check if the logged-in user is an admin or the owner of the profile
            var isAdmin = User.IsInRole("Admin");
            if (user.Id != currentUserId && !isAdmin)
            {
                return Forbid(); // Return 403 Forbidden if unauthorized
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Update the user's fields
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Bio = model.Bio;
            user.IsPrivate = model.IsPrivate;

            // Save changes to the database
            _context.SaveChanges();

            return RedirectToAction("Show", new { id = user.Id });
        }



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

        public IActionResult Show(string id)
        {
            // Găsim utilizatorul prin ID
            var user = _context.Users
                .Include(u => u.Posts)
                    .ThenInclude(p => p.Comments)
                .Include(u => u.Followers) // Include relația pentru urmăritori
                .Include(u => u.Following) // Include relația pentru urmăriți
                .FirstOrDefault(u => u.Id == id);

            // Verificăm dacă utilizatorul există
            if (user == null)
            {
                return NotFound(); // Utilizatorul nu a fost găsit
            }

            // Obținem ID-ul utilizatorului curent
            var currentUserId = _userManager.GetUserId(User);

            // Verificăm starea urmării
            var followRequest = _context.FollowRequests
                .FirstOrDefault(r => r.SenderId == currentUserId && r.ReceiverId == id);

            // Setăm starea urmării în ViewBag
            ViewBag.IsFollowing = followRequest != null && followRequest.IsAccepted;
            ViewBag.IsPending = followRequest != null && !followRequest.IsAccepted;

            // Verificăm dacă utilizatorul curent este proprietarul profilului
            ViewBag.IsCurrentUser = currentUserId == id;

            // Calculăm numărul de urmăritori și urmăriți
            ViewBag.FollowersCount = _context.FollowRequests.Count(r => r.ReceiverId == id && r.IsAccepted);
            ViewBag.FollowingCount = _context.FollowRequests.Count(r => r.SenderId == id && r.IsAccepted);

            // Returnăm view-ul cu datele utilizatorului
            return View(user);
        }



        // FOLLOW / UNFOLLOW ACTIONS
        // trimiterea unei cereri de urmarire
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult SendFollowRequest(string receiverId)
        {
            var senderId = _userManager.GetUserId(User);

            // Nu permite utilizatorilor să trimită cereri către ei înșiși
            if (senderId == receiverId)
            {
                TempData["message"] = "You cannot follow yourself!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            // Verifică dacă există deja o cerere de urmărire neacceptată
            var existingRequest = _context.FollowRequests
                .FirstOrDefault(r => r.SenderId == senderId && r.ReceiverId == receiverId && !r.IsAccepted);

            if (existingRequest != null)
            {
                TempData["message"] = "Follow request already sent!";
                TempData["messageType"] = "alert-warning";
                return RedirectToAction("Show", new { id = receiverId });
            }

            // Creează o cerere nouă
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

            // Găsește cererea de urmărire
            var followRequest = _context.FollowRequests
                .FirstOrDefault(r => r.Id == requestId && r.ReceiverId == currentUserId && !r.IsAccepted);

            if (followRequest == null)
            {
                TempData["message"] = "Follow request not found or already accepted.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("FollowRequests");
            }

            // Acceptă cererea
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

            _context.FollowRequests.Remove(followRequest);
            _context.SaveChanges();

            TempData["message"] = "Follow request declined.";
            TempData["messageType"] = "alert-warning";

            return RedirectToAction("FollowRequests");
        }


        // lista cererilor de urmarire
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


        // posibilitatea de a da cancel la cererea de urmarire
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult CancelFollowRequest(int requestId)
        {
            var currentUserId = _userManager.GetUserId(User);

            var followRequest = _context.FollowRequests
                .FirstOrDefault(r => r.Id == requestId && r.SenderId == currentUserId && !r.IsAccepted);

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

            // Preluăm lista tuturor utilizatorilor (excluzând utilizatorul curent)
            var users = _context.Users
                .Where(u => u.Id != currentUserId)
                .ToList();

            // Preluăm utilizatorii pe care îi urmărește deja utilizatorul curent
            var following = _context.FollowRequests
                .Where(fr => fr.SenderId == currentUserId && fr.IsAccepted)
                .Select(fr => fr.ReceiverId)
                .ToList();

            ViewBag.Following = following;
            return View(users); // Trimitem utilizatorii către view
        }


    }
}

