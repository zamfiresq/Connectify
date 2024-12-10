using Microsoft.AspNetCore.Mvc;

namespace Connectify.Controllers
{
    public class GroupsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
