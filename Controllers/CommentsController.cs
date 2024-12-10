using Microsoft.AspNetCore.Mvc;

namespace Connectify.Controllers
{
    public class CommentsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
