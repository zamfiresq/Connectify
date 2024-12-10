using Microsoft.AspNetCore.Mvc;

namespace Connectify.Controllers
{
    public class PostsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
