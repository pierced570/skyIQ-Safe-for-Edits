using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Python.Runtime;
using SkyIQ.Data;
using SkyIQ.Models;
using System.Numerics;

namespace SkyIQ.Controllers
{
    [Authorize]
    public class FleetController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;

        //private Dictionary<string, List<string>> aircrafts = new Dictionary<string, List<string>> { 
        //    { "Boeing", new List<string> { "737", "737 Max", "747-8", "767", "777", "777X", "787" } },
        //    { "Cessna", new List<string> { "Citation M2", "CJ3+", "CJ4", "XLS+", "Latitude", "Sovereign+", "Longitude", "Mustang", "Encore+", "X+"} },
        //};

        public FleetController(ApplicationDbContext context, SignInManager<ApplicationUser> signIn)
        {
            _context = context;
            _signInManager = signIn;
        }
        public async Task<IActionResult> Index()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Auth", "Account", new { area = "Identity" });
            }
            ApplicationUser user = await _signInManager.UserManager.GetUserAsync(User);

            List<Aircraft> aircrafts = _context.Aircrafts.Where(a => a.UserCompany == user.Id && a.IsEnabled).ToList();
            return View(aircrafts);
        }

        public IActionResult AddPlane()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Auth", "Account", new { area = "Identity" });
            }

            //ViewData["Aircrafts"] = aircrafts;

            AircraftDTO dto = new AircraftDTO();
            dto.CarryTypes = _context.CarryTypes.ToList(); 

            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> AddPlane(Aircraft aircraft)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Auth", "Account", new { area = "Identity" });
            }

            ApplicationUser user = await _signInManager.UserManager.GetUserAsync(User);
            aircraft.UserCompany = user.Id;
            aircraft.IsEnabled = true; 
            _context.Aircrafts.Add(aircraft);
            _context.SaveChanges();
            return RedirectToAction("EditPlane", new { tail = aircraft.TailNumber });
        }

        public async Task<IActionResult> EditPlane(int id)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Auth", "Account", new { area = "Identity" });
            }

            ApplicationUser user = await _signInManager.UserManager.GetUserAsync(User);

            //ViewData["Aircrafts"] = aircrafts;
            Aircraft plane = _context.Aircrafts.Where(a => a.Id == id && a.UserCompany == user.Id && a.IsEnabled).FirstOrDefault();

            if(plane == null)
            {
                return RedirectToAction("Index");
            }

            AircraftDTO dto = new AircraftDTO();
            dto.CarryTypes = _context.CarryTypes.ToList();
            dto.Aircraft = plane; 

            return View(dto);
        }
        [HttpPost]
        public async Task<IActionResult> EditPlane(Aircraft aircraft)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Auth", "Account", new { area = "Identity" });
            }

            ApplicationUser user = await _signInManager.UserManager.GetUserAsync(User);

            //ViewData["Aircrafts"] = aircrafts;
            Aircraft oldAir = _context.Aircrafts.Where(a => a.Id == aircraft.Id && a.UserCompany == user.Id).FirstOrDefault();


            if (oldAir == null)
            {
                return RedirectToAction("AddPlane", new { plane = aircraft });
            }

            oldAir.TailNumber = aircraft.TailNumber;
            oldAir.BasicEmptyWeight = aircraft.BasicEmptyWeight;
            oldAir.MaxLandingWeight = aircraft.MaxLandingWeight;
            oldAir.MaxTakeoffWeight = aircraft.MaxTakeoffWeight;
            oldAir.PreferredReserve = aircraft.PreferredReserve;
            oldAir.MaxFuelCapacity = aircraft.MaxFuelCapacity;
            oldAir.Manufacturer = aircraft.Manufacturer;
            oldAir.Type = aircraft.Type;
            oldAir.TaxiFuelBurn = aircraft.TaxiFuelBurn;
            oldAir.MaxRampWeight = aircraft.MaxRampWeight;
            oldAir.DefaultPaxWeight = aircraft.DefaultPaxWeight;
            oldAir.DefaultBaggageWithPax = aircraft.DefaultBaggageWithPax;
            oldAir.DefaultBaggageNoPax = aircraft.DefaultBaggageNoPax;
            oldAir.DefaultPICWeight = aircraft.DefaultPICWeight;
            oldAir.DefaultSICWeight = aircraft.DefaultSICWeight;
            oldAir.DefaultCabinWeight = aircraft.DefaultCabinWeight;
            oldAir.CarryTypeId = aircraft.CarryTypeId;
            oldAir.CruiseFuelBurn = aircraft.CruiseFuelBurn;
            oldAir.PenaltyRate = aircraft.PenaltyRate;

            _context.Aircrafts.Update(oldAir);
            _context.SaveChanges();

            AircraftDTO dto = new AircraftDTO();
            dto.CarryTypes = _context.CarryTypes.ToList();
            dto.Aircraft = aircraft;

            return View(dto);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Auth", "Account", new { area = "Identity" });
            }
            Aircraft aircraft = _context.Aircrafts.Where(a => a.Id == id).FirstOrDefault(); 
            if(aircraft != null)
            {
                aircraft.IsEnabled = false;
                _context.Aircrafts.Update(aircraft); 
                _context.SaveChanges();
            }
            return Json(new { redirectToUrl = Url.Action("Index", "Fleet") });

        }

        public async Task<IActionResult> SavingsAccrued()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Auth", "Account", new { area = "Identity" });
            }
            ApplicationUser user = await _signInManager.UserManager.GetUserAsync(User);

            List<Aircraft> allAircrafts = _context.Aircrafts.Where(a => a.UserCompany == user.Id && a.IsEnabled).ToList();
            var trips = _context.Trips.Where(t => t.UserCompany == user.Id).ToList();

            List<TailDTO> aircrafts = new List<TailDTO>(); 

            foreach(var craft in allAircrafts)
            {
                TailDTO aircraft = new TailDTO();
                aircraft.TailNumber = craft.TailNumber; 
                var craftTrips = trips.Where(t => t.ItineraryDetails.Contains("AircraftId\":\"" + craft.TailNumber + "\"")).ToList();
                aircraft.TripsRun = craftTrips.Count();
                aircraft.Savings = craftTrips.Select(t => t.Savings).Sum();

                aircrafts.Add(aircraft); 
            }

            return View(aircrafts);
        }

    }
}
