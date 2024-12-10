using Microsoft.AspNetCore.Mvc;

namespace Connectify.Controllers
{
    public class MessagesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
