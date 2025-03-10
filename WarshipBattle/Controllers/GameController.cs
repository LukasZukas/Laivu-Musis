using Microsoft.AspNetCore.Mvc;

namespace WarshipBattle.Controllers
{
    public class GameController : Controller
    {
        public IActionResult GameModes()
        {
            return View();
        }
        public IActionResult PlayAgainstBot()
        {
            return View();
        }

        public IActionResult PlayAgainstPlayer()
        {
            return View();
        }

    }
}
