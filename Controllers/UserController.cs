using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using SkyIQ.Data;
using SkyIQ.Models;
using System.Text.Encodings.Web;
using System.Text;
using System.ComponentModel.Design;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using EvoPdfClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using SkyIQ.Email;

namespace SkyIQ.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly int FREE_TRIAL_DAYS = 30;

        public UserController(UserManager<ApplicationUser> um, SignInManager<ApplicationUser> sm, RoleManager<IdentityRole> rm, ApplicationDbContext c, IEmailSender es) =>
             (_userManager, _signInManager, _roleManager, _context, _emailSender) = (um, sm, rm, c, es);
        
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ViewCompany(string id)
        {
            var company = await _userManager.FindByIdAsync(id);
            if (company == null) 
            { 
                return RedirectToAction("Companies");
            }

            CompanyDTO dto = new CompanyDTO();
            dto.Id = company.Id;
            dto.Company = company.Company;
            dto.IsEnabled = company.IsEnabled;
            dto.Tails = new List<TailDTO>(); 

            var aircrafts = _context.Aircrafts.Where(a => a.UserCompany == id && a.IsEnabled).ToList();
            var trips = _context.Trips.Where(t => t.UserCompany == id).ToList();

            foreach (var aircraft in aircrafts) 
            {
                TailDTO tail = new TailDTO();
                tail.TailNumber = aircraft.TailNumber;
                tail.ManufacturerModel = aircraft.Manufacturer + " " + aircraft.Type;
                var tailTrips = trips.Where(t => t.ItineraryDetails.Contains("AircraftId\":\"" + aircraft.TailNumber + "\"")).ToList();
                tail.TripsRun30 = tailTrips.Where(t => t.CreatedOn > DateTime.Now.AddDays(-30)).Count(); 
                tail.TripsRun = tailTrips.Count();
                tail.Savings = trips.Select(t => t.Savings).Sum();
                dto.Tails.Add(tail);
            }

            ViewData["Title"] = "Companies List"; 
            return View(dto);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleCompany(string id, bool isEnabled)
        {
            var company = await _userManager.FindByIdAsync(id);
            if (company == null)
            {
                return RedirectToAction("Companies");
            }

            company.IsEnabled = isEnabled;
            await _userManager.UpdateAsync(company);
            _context.SaveChanges();

            return RedirectToAction("ViewCompany", new {id =  company.Id});
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Companies()
        {
            CompanyList list = new CompanyList();
            var data = new List<CompanyDTO>();

            var companies = _userManager.Users.ToList();
            var allAircrafts = _context.Aircrafts.ToList();
            var allTrips = _context.Trips.ToList();

            foreach (var company in companies)
            {
                CompanyDTO dto = new CompanyDTO();
                dto.Company = company.Company;
                dto.Id = company.Id;
                dto.Tails = new List<TailDTO>();

                var aircrafts = allAircrafts.Where(a => a.UserCompany == company.Id).ToList();
                var trips = allTrips.Where(t => t.UserCompany == company.Id).ToList();

                foreach (var aircraft in aircrafts)
                {
                    TailDTO tail = new TailDTO();
                    var tailTrips = trips.Where(t => t.ItineraryDetails.Contains("AircraftId\":\"" + aircraft.TailNumber + "\"")).ToList();
                    tail.TripsRun = tailTrips.Count();
                    tail.Savings = trips.Select(t => t.Savings).Sum();
                    dto.Tails.Add(tail);

                    list.TripsRun += tail.TripsRun; 
                }
                data.Add(dto);
                list.TailNumbers += aircrafts.Count(); 
            }

            list.NumCompanies = companies.Count();
            list.TripsRun = data.Select(c => c.Tails.Select(t => t.TripsRun).Sum()).Sum();
            list.TailNumbers = data.Select(c => c.Tails.Count()).Sum();

            return View(list);
        }

        [HttpPost]
        public List<CopmanyListTableRow> GetCompanies(JQDTParams parameters, int days)
        {
            List<CopmanyListTableRow> list = new List<CopmanyListTableRow>();

            var companies = _userManager.Users.ToList();
            var allAircrafts = _context.Aircrafts.ToList();
            var allTrips = _context.Trips.ToList();
           
            string search = parameters.search?["value"];
            if(search != null)
            {
                allAircrafts = allAircrafts.Where(a => a.TailNumber.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                companies = companies.Where(c => c.Company.Contains(search, StringComparison.OrdinalIgnoreCase) || allAircrafts.Select(a => a.UserCompany).Contains(c.Id) || (c.FirstName + " " + c.LastName).Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            if (days > 0) {
                allTrips = allTrips.Where(t => t.CreatedOn >= DateTime.Now.AddDays(days * -1)).ToList();
            }
            foreach (var company in companies)
            {
                CopmanyListTableRow dto = new CopmanyListTableRow();
                dto.Company = company.Company;
                dto.Id = company.Id;
                dto.Name = company.FirstName + " " + company.LastName;

                var aircrafts = allAircrafts.Where(a => a.UserCompany == company.Id).ToList();
                var trips = allTrips.Where(t => t.UserCompany == company.Id).ToList();
                double savings = 0;
                foreach (var aircraft in aircrafts)
                {
                    TailDTO tail = new TailDTO();
                    var tailTrips = trips.Where(t => t.ItineraryDetails.Contains("AircraftId\":\"" + aircraft.TailNumber + "\"")).ToList();
                    savings += tailTrips.Select(t => t.Savings).Sum();
                    dto.NumTrips += tailTrips.Count();
                }
                dto.NumTails += aircrafts.Count();
                dto.SavingsAccrued = NumberFormatter.FormatNumber(savings);
                list.Add(dto);
            }
            var order = parameters.order != null ? parameters.order[0] : new Order { column = 0, dir = "asc" };
            switch (order.column)
            {
                case 0: 
                    if(order.dir == "asc")
                    {
                        list = list.OrderBy(l => l.Company).ToList(); 
                    }
                    else
                    {
                        list = list.OrderByDescending(l => l.Company).ToList(); 
                    }
                    break;
                case 1:
                    if(order.dir == "asc")
                    {
                        list = list.OrderBy(l => l.Name).ToList(); 
                    }
                    else
                    {
                        list = list.OrderByDescending(l => l.Name).ToList(); 
                    }
                    break;
                case 2: 
                    if(order.dir == "asc")
                    {
                        list = list.OrderBy(l => l.NumTrips).ToList(); 
                    }
                    else
                    {
                        list = list.OrderByDescending(l => l.NumTrips).ToList(); 
                    }
                    break;
                    
                case 3: 
                    if(order.dir == "asc")
                    {
                        list = list.OrderBy(l => l.NumTails).ToList(); 
                    }
                    else
                    {
                        list = list.OrderByDescending(l => l.NumTails).ToList(); 
                    }
                    break;

            }

            return list;
        }

        // GET: Show the Edit Form
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditUserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Organization = user.Company,
                IsEnabled = !user.LockoutEnd.HasValue || user.LockoutEnd <= DateTime.UtcNow // Enabled if lockout is expired
            };

            return View(model);
        }

        public IActionResult AddUser()
        {
            var model = new AddUserViewModel();
            return View(model);
        }

        // POST: Handle user creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUser(AddUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Company = model.Organization,
                RoleName = model.IsAdmin ? "Admin" : "StandardUser",
                EmailConfirmed = false // Require email confirmation
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            // Assign role
            string role = model.IsAdmin ? "Admin" : "StandardUser";
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
            await _userManager.AddToRoleAsync(user, role);

            // Generate email confirmation token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var callbackUrl = Url.Action("ConfirmEmail", "Users", new { userId = user.Id, token = encodedToken }, Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email, "Confirm Your Email",
                $"Please confirm your email by clicking <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>here</a>.");

            return RedirectToAction("ListUsers");
        }

        // GET: Email confirmation endpoint
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return BadRequest("Invalid confirmation link.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (result.Succeeded)
            {
                return RedirectToAction("ListUsers");
            }
            else
            {
                return BadRequest("Email confirmation failed.");
            }
        }

        // POST: Handle form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }

            // Validate if email is already taken
            if (user.Email != model.Email) // Only check if the email is changed
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    ModelState.AddModelError("Email", "This email is already in use.");
                    return View(model);
                }
            }

            // Update user details
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.UserName = model.Email; // Ensure UserName updates to match Email
            user.NormalizedEmail = model.Email.ToUpper();
            user.NormalizedUserName = model.Email.ToUpper(); 
            user.Company = model.Organization;

            // Handle enabled/disabled state
            if (model.IsEnabled)
            {
                user.LockoutEnd = null; // Remove lockout
            }
            else
            {
                user.LockoutEnd = DateTime.UtcNow.AddYears(100); // Lock the user
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("ListUsers"); // Redirect to the users list
            }

            // If update fails, show errors
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }


		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return BadRequest("User ID is required.");
			}

			var user = await _userManager.FindByIdAsync(id);
			if (user == null)
			{
				return NotFound("User not found.");
			}

			// Delete related data (Aircrafts, Trips, etc.)
			// var aircrafts = _context.Aircrafts.Where(a => a.UserCompany == id).ToList();
			// _context.Aircrafts.RemoveRange(aircrafts);

			// var trips = _context.Trips.Where(t => t.UserCompany == id).ToList();
			// _context.Trips.RemoveRange(trips);

			var result = await _userManager.DeleteAsync(user);
			if (!result.Succeeded)
			{
				foreach (var error in result.Errors)
				{
					ModelState.AddModelError(string.Empty, error.Description);
				}

				// Optionally return to a view with errors
				return RedirectToAction("ViewCompany", new { id = id });
			}

			// If removeing aircraft/trips above, uncomment this:
			// await _context.SaveChangesAsync();

			return RedirectToAction("Companies");
		}



		[HttpGet]
        public async Task<IActionResult> Settings()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Auth", "Account", new { area = "Identity" });
            }
            ApplicationUser user = await _userManager.GetUserAsync(User);

            return View(user);
        }
        [HttpPost]
        public async Task<IActionResult> Settings(ApplicationUser user)
        {
            
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Auth", "Account", new { area = "Identity" });
            }
            ApplicationUser update = await _signInManager.UserManager.GetUserAsync(User);
            update.FirstName = user.FirstName;
            update.LastName = user.LastName;
            update.Email = user.Email;
            update.NormalizedEmail = update.Email.ToUpper();
            update.UserName = user.Email;
            update.NormalizedUserName = user.Email.ToUpper(); 
            update.Company = user.Company;

            _context.Users.Update(update);
            _context.SaveChanges(); 

            return View(update);
        }
        
        [HttpPost]
        public Object GetCompanyTails(JQDTParams parameters, int days)
        {
            var company = _userManager.GetUserAsync(User).Result;
            List<TailDTO> list = new List<TailDTO>();

            var allAircrafts = _context.Aircrafts.Where(a => a.UserCompany == company.Id && a.IsEnabled).ToList();
            var allTrips = _context.Trips.Where(a => a.UserCompany == company.Id).ToList();

            string search = parameters.search?["value"];
            if (search != null)
            {
                allAircrafts = allAircrafts.Where(a => a.TailNumber.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            if (days > 0)
            {
                allTrips = allTrips.Where(t => t.CreatedOn >= DateTime.Now.AddDays(days * -1)).ToList();
            }

            foreach (var aircraft in allAircrafts)
            {
                TailDTO tail = new TailDTO();
                tail.TailNumber = aircraft.TailNumber;
                tail.ManufacturerModel = aircraft.Manufacturer + " " + aircraft.Type;
                var tailTrips = allTrips.Where(t => t.ItineraryDetails.Contains("AircraftId\":\"" + aircraft.TailNumber + "\"")).ToList();
                tail.TripsRun30 = tailTrips.Where(t => t.CreatedOn > DateTime.Now.AddDays(-30)).Count();
                tail.TripsRun = tailTrips.Count();
                tail.Savings = tailTrips.Select(t => t.Savings).Sum();
                tail.LastRun = tail.TripsRun == 0 ? null : tailTrips.OrderBy(t => t.CreatedOn).Last().CreatedOn; 

                list.Add(tail);
            }

            string savings = NumberFormatter.FormatNumber(list.Select(t => t.Savings).Sum());
            string TripsRun = NumberFormatter.FormatNumber(list.Select(t => t.TripsRun).Sum());
            string NumTails = NumberFormatter.FormatNumber(list.Count());

            return new { savings, TripsRun, NumTails, tails = list.Select(t => new TailFormattedDTO(t)).ToList()};
        }

        public async void SendFreeTrialEmails()
        {
            //7, 5, 3, 0, -7? //23 25 27 30 
            DateTime now = DateTime.Now; 
            DateTime freeTrialDate = new DateTime(now.Year, now.Month, now.Day).AddDays(FREE_TRIAL_DAYS * -1);
            var users = _context.Users.Where(u => u.CreatedOn >= freeTrialDate).ToList();
            foreach (var user in users) {
                if (user.RoleName != "Admin")
                {
                    int daysRemaining = FREE_TRIAL_DAYS - DateTime.Now.Subtract(user.CreatedOn).Days;
                    if (daysRemaining == 0) {
                        _emailSender.SendEndTrialEmailAsync(user.Email);
                    }
                    else if(daysRemaining == 3 ||  daysRemaining == 5 || daysRemaining == 7)
                    {
                        _emailSender.SendEndTrialDaysEmailAsync(user.Email, daysRemaining);
                    }
                } 
            }
        }       


        public class JQDTParams
        {
            public int draw { get; set; }

            public int start { get; set; }
            public int length { get; set; }


            public Dictionary<string, string> search { get; set; }
            //public Dictionary<string, string> order { get; set; }
            public List<Order> order { get; set; }
            public Dictionary<string, string> columns { get; set; }

        }
        public class Order
        {
            public int column { get; set; }
            public string dir { get; set; }
        }
    }
}
