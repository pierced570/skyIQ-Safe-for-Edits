using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SkyIQ.Data;
using SkyIQ.Models;
using System.Diagnostics;

namespace SkyIQ.Controllers
{
    public class HomeController : Controller
    {
        public ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager; 
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager; 

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager; 
        }
        [Authorize]
        public async Task<IActionResult> Index()
        {
            if (!_signInManager.IsSignedIn(HttpContext.User))
            {
                return RedirectToAction("Auth", "Account", new { area = "Identity" });
            }

            ApplicationUser user = await _signInManager.UserManager.GetUserAsync(User);
            
            List<TripSummaryDb> dbTrip = _context.Trips.Where(t => t.UserCompany == user.Id).ToList();
            List<TripSummary> trips = dbTrip.Select(dt => {
                var t = JsonConvert.DeserializeObject<TripSummary>(dt.Details);
                t.Id = dt.Id;
                return t;
            }).ToList();
            ViewData["Trips"] = trips.Count() > 0 ? trips : new List<TripSummary>();
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<ActionResult> PreviousFlights()
        {
            if (!_signInManager.IsSignedIn(HttpContext.User))
            {
                return RedirectToAction("Auth", "Account", new { area = "Identity" });
            }

            ApplicationUser user = await _signInManager.UserManager.GetUserAsync(User);

            List<TripSummaryDb> dbTrip = _context.Trips.Where(t => t.UserCompany == user.Id).ToList();
            List<TripSummary> trips = dbTrip.Select(dt => {
                var t = JsonConvert.DeserializeObject<TripSummary>(dt.Details);
                t.Id = dt.Id;
                return t;
            }).ToList();
            var triplist = trips.Count() > 0 ? trips : new List<TripSummary>();
            return PartialView("_CategoryList", triplist);
        }

    }
}