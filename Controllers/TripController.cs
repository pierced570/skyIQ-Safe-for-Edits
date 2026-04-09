using Microsoft.AspNetCore.Mvc;
using SkyIQ.Models;
using Microsoft.EntityFrameworkCore;
using SkyIQ.Data;
using Python.Runtime;
using System.Diagnostics;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Numerics;
using EvoPdfClient;
using SkyIQ.Email;
using SkyIQ.Migrations;
using Microsoft.CodeAnalysis.CSharp.Syntax;



namespace SkyIQ.Controllers
{
    public class TripController : Controller
    {
        public ApplicationDbContext _context;
        public IConfiguration _config;
        public IEmailSender _emailSender;
        private readonly IWebHostEnvironment _host;

        private readonly SignInManager<ApplicationUser> _signInManager;

        public TripController(ApplicationDbContext context, IConfiguration config, SignInManager<ApplicationUser> signIn, IEmailSender emailSender, IWebHostEnvironment host)
        {
            _context = context;
            _config = config;
            _signInManager = signIn;
            _emailSender = emailSender;
            _host = host;
        }

        [Authorize]
        public IActionResult Index()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Auth", "Account", new { area = "Identity" });
            }

            return View();
        }
        //Plan Trip Form
        [HttpGet]
        public async Task<IActionResult> PlanTrip()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Auth", "Account", new { area = "Identity" });
            }

            ApplicationUser user = await _signInManager.UserManager.GetUserAsync(User);

            ViewData["Aircrafts"] = _context.Aircrafts.Where(a => a.UserCompany == user.Id && a.IsEnabled).ToList();
            ViewData["SummaryId"] = 0;
            ViewData["IsFromBackBtn"] = false;
            TripDTO trip = new TripDTO();
            trip.StartingFuel = 0;
            trip.AircraftId = ""; 
            trip.ItineraryNum = "";

            trip.Legs.Add(new LegDTO
            {
                Departure = "",
                Destination ="",
                WaivedFee = new FeeDTO(),
                PassengerWeights = "0",
                Baggage = 0,
                DepartureFuelPrices = new List<FuelPrice> { new FuelPrice() },
                CrewWeight = "0",
                FuelBurn = 0,
                Distance = 0,
                Reserve = 0,
                TaxiFuelBurn = 0,
                MaxRampWeight = 0
            });

            return View(trip);
        }
        [HttpPost]
        public async Task<IActionResult> PlanTrip([FromForm(Name = "formFile")]IFormFile formFile, TripDTO trip, int summaryId, bool isFromBackBtn)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Auth", "Account", new { area = "Identity" });
            }

            ApplicationUser user = await _signInManager.UserManager.GetUserAsync(User);
            var aircrafts = _context.Aircrafts.Where(a => a.UserCompany == user.Id && a.IsEnabled).ToList();
            ViewData["Aircrafts"] = aircrafts;
            ViewData["SummaryId"] = summaryId;
            ViewData["IsFromBackBtn"] = isFromBackBtn; 

            if (!string.IsNullOrEmpty(trip.AircraftId))
            {
                return View(trip);
            }

            if(summaryId > 0)
            {

                TripSummaryDb dbTrip = _context.Trips.Where(t => t.Id == summaryId).FirstOrDefault();
                trip = JsonConvert.DeserializeObject<TripDTO>(dbTrip.ItineraryDetails);
                if(trip == null)
                {
                    return View(new TripDTO() { Legs = new List<LegDTO> { new LegDTO { DepartureFuelPrices = new List<FuelPrice> { new FuelPrice() } } } });
                }
                return View(trip);
            }

            var jobDirectory = _config["ItinerariesPath"];
            if (!Directory.Exists(jobDirectory))
            {
                Directory.CreateDirectory(jobDirectory);
            }

            //make sure file has unique name
            var fileext = Guid.NewGuid().ToString() + formFile.FileName.Replace(" ", "").Replace(":", "_").Replace("*", "_");
            while (System.IO.File.Exists(jobDirectory + fileext))
            {
                fileext = Guid.NewGuid().ToString() + formFile.FileName.Replace(" ", "").Replace(":", "_").Replace("*", "_");

            }

            var filePath = Path.Combine(jobDirectory, fileext);

            using (var stream = System.IO.File.Create(filePath))
            {
                await formFile.CopyToAsync(stream);
            }


            PythonTripDTO pythonTrip = ParseItinerary(filePath);
            if(pythonTrip == null)
            {
                trip = new TripDTO();
                trip.StartingFuel = 0;
                trip.AircraftId = ""; //tail number 
                trip.ItineraryNum = "";

                trip.Legs.Add(new LegDTO
                {
                    Departure = "",
                    Destination = "",
                    WaivedFee = new FeeDTO(),
                    PassengerWeights = "0",
                    Baggage = 0,
                    DepartureFuelPrices = new List<FuelPrice> { new FuelPrice() },
                    CrewWeight = "0",
                    FuelBurn = 0,
                    Distance = 0,
                    Reserve = 0,
                    TaxiFuelBurn = 0,
                    MaxRampWeight = 0
                });
                return View(trip); 
            }

            trip = new TripDTO();
            trip.StartingFuel = pythonTrip.starting_fuel;
            trip.AircraftId = pythonTrip.aircraft; //tail number 
            trip.ItineraryNum = pythonTrip.itinerary_num;
            trip.ImportErrors = pythonTrip.errors;

            var aircraft = aircrafts.Where(a => a.TailNumber == trip.AircraftId).FirstOrDefault();
            aircraft = aircraft ?? new Aircraft(); 
            
            if(trip.ImportErrors.Count() == 1 && trip.ImportErrors[0].Contains("line 0, position 0"))
            {
                trip.ImportErrors[0] = "Trip sheet not detected. Please reupload or manually plan your trip."; 
            }

            foreach (PythonLegDTO pythonLeg in pythonTrip.legs)
            {
                var waivedFee = pythonLeg.fees.Where(f => f.airport == pythonLeg.destination && f.is_waivable).FirstOrDefault();
                var paxWeights = pythonLeg.passengers.Select(p => { p = p == -1 ? aircraft.DefaultPaxWeight : p; return p; }).ToList(); 
                trip.Legs.Add(new LegDTO
                {
                    Departure = pythonLeg.departure,
                    Destination = pythonLeg.destination,
                    WaivedFee = new FeeDTO(waivedFee),
                    PassengerWeights = paxWeights != null ? string.Join(", ", paxWeights) : "0",
                    Baggage = aircraft == null ? 0 : pythonLeg.passengers == null || pythonLeg.passengers.Count() == 0 ? aircraft.DefaultBaggageNoPax : aircraft.DefaultBaggageWithPax, //pythonLeg.baggage + ((pythonLeg.passengers.Count() + pythonLeg.crew_weight.Count()) * 50),
                    DepartureFuelPrices = pythonLeg.departure_fuel_price.Count() > 0 ? pythonLeg.departure_fuel_price.Select(fp => { return new FuelPrice(fp); }).ToList() : new List<FuelPrice> { new FuelPrice() },
                    CrewWeight = aircraft != null ? aircraft.DefaultPICWeight + ", " + aircraft.DefaultSICWeight + ", " + aircraft.DefaultCabinWeight : "",//pythonLeg.crew_weight != null ? string.Join(", ", pythonLeg.crew_weight) : "180",
                    FuelBurn = pythonLeg.fuel_burn,
                    Distance = pythonLeg.distance,
                    Reserve = pythonLeg.reserve, 
                    TaxiFuelBurn = pythonLeg.taxi_fuel_burn,
                    MaxRampWeight = pythonLeg.max_ramp_weight
                });
            }

            return View(trip);
        }
        //View Summary after confirmations and emails
        [HttpGet]
        public async Task<IActionResult> TripSummary(int id)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Auth", "Account", new { area = "Identity" });
            }

            ApplicationUser user = await _signInManager.UserManager.GetUserAsync(User);

            TripSummaryDb dbTrip = _context.Trips.Where(t => t.Id == id && t.UserCompany == user.Id).FirstOrDefault();
            TripSummary trip = JsonConvert.DeserializeObject<TripSummary>(dbTrip.Details);
            trip.Id = dbTrip.Id;
            return View(trip);
        }
        //Initial calculations after plan trip form submitted
        [HttpPost]
        public async Task<ActionResult> TripRoughSummary(TripDTO trip, int summaryId)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Auth", "Account", new { area = "Identity" });
            }
            ApplicationUser user = await _signInManager.UserManager.GetUserAsync(User);

            trip.Legs = trip.Legs.Where(t => t.LegNum > 0).OrderBy(t => t.LegNum).ToList(); 

            PythonTripDTO pyTrip = new PythonTripDTO(trip);


            string fileName = _config["PythonPathCalculator"];

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = _config["PythonPath"];
            string json = JsonConvert.SerializeObject(pyTrip);
            json = "\"" + json.Replace("\"", "\\\"") + "\"";
            start.Arguments = string.Format("{0} {1} {2} {3}", fileName, trip.StartingFuel, 1000, json);
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            string calculations = "";

            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    calculations = reader.ReadToEnd();
                }
            }

            TripSummary summary = new TripSummary();
            if (calculations.Length > 0)
            {
                var decoded = JsonConvert.DeserializeObject<PythonTripSummary>(calculations);
                summary = new TripSummary(summaryId, pyTrip.itinerary_num, decoded.data, pyTrip.aircraft, decoded.simple_cost);
            }

            TripRoughSummary rough = new TripRoughSummary { Trip = trip, Summary = summary }; 

            return View(rough);
        }
        //Take calculations to be finalized, get user emails
        [HttpPost]
        public async Task<IActionResult> TripEmailList(TripSummary summary, TripDTO trip)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Auth", "Account", new { area = "Identity" });
            }
            ApplicationUser user = await _signInManager.UserManager.GetUserAsync(User);
            
            TripSummaryDb dbSum = new TripSummaryDb(summary, trip);

            if (summary.Id > 0)
            {

                dbSum = _context.Trips.Where(t => t.Id == summary.Id).FirstOrDefault();
                dbSum.Update(summary, trip);
                _context.Trips.Update(dbSum);
            }
            else
            {
                dbSum.CreatedOn = DateTime.Now;
                dbSum.UserCompany = user.Id;

                _context.Trips.Add(dbSum);
            }
            await _context.SaveChangesAsync();

            var list = _context.EmailLists.Where(el => el.UserId == user.Id).FirstOrDefault();
            EmailListDTO emailList = list == null ? new EmailListDTO() : new EmailListDTO(list);
            emailList.AircraftNum = summary.AircraftNumber;
            emailList.ItineraryNum = summary.ItineraryNum ?? summary.Id.ToString();
            emailList.TripSummaryId = dbSum.Id;
            emailList.Emails = emailList.Emails.OrderByDescending(e => e.LastEmailed).Take(10).Select(e => { e.IsChecked = false; return e; }).ToList();
            return View(emailList);
        }

        [HttpGet]
        public async Task<IActionResult> TripEmailList(int id)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Auth", "Account", new { area = "Identity" });
            }
            ApplicationUser user = await _signInManager.UserManager.GetUserAsync(User);
            
            var dbSum = _context.Trips.Where(t => t.Id == id).FirstOrDefault();
            TripSummary summary = JsonConvert.DeserializeObject<TripSummary>(dbSum.Details);

            var list = _context.EmailLists.Where(el => el.UserId == user.Id).FirstOrDefault();
            EmailListDTO emailList = list == null ? new EmailListDTO() : new EmailListDTO(list);
            emailList.AircraftNum = summary.AircraftNumber;
            emailList.ItineraryNum = summary.ItineraryNum ?? summary.Id.ToString();
            emailList.TripSummaryId = dbSum.Id;
            emailList.Emails = emailList.Emails.OrderByDescending(e => e.LastEmailed).Take(10).Select(e => { e.IsChecked = false; return e; }).ToList();
            return View(emailList);
        }
        //Finalize trip - save to db and sends out emails
        [HttpPost]
        public async Task<IActionResult> TripSummary(EmailListDTO dto)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Auth", "Account", new { area = "Identity" });
            }
            ApplicationUser user = await _signInManager.UserManager.GetUserAsync(User);

            dto.UserId = user.Id;
            dto.Emails = dto.Emails.Where(e => e.IsChecked && !string.IsNullOrEmpty(e.Email)).Select(e => { e.LastEmailed = DateTime.Now; return e; }).ToList();

            string tripDisplayNumber = dto.ItineraryNum ?? dto.TripSummaryId.ToString();

            Dictionary<string, byte[]> pdfs = new Dictionary<string, byte[]>();
            var fullSummary = ExportSummaryPdf(dto.TripSummaryId, true);
            if (fullSummary != null) 
            {
                pdfs.Add("Trip" + tripDisplayNumber + "Summary.pdf", fullSummary); 
            }
            var shortSummary = ExportSummaryPdf(dto.TripSummaryId, false);
            if(shortSummary != null)
            {
                pdfs.Add("Trip" + tripDisplayNumber + "QuickRef.pdf", shortSummary);
            }

            foreach (var email in dto.Emails)
            {
                if (pdfs.Count() > 0)
                {
                    await _emailSender.SendEmailWithAttachmentAsync(email.Email, tripDisplayNumber, $"<a href=\"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}/Trip/TripSummary/{dto.TripSummaryId}\">Trip Summary</a>", pdfs, "Trip" + tripDisplayNumber + "Summary.pdf");
                }
                else
                {
                    await _emailSender.SendTripSummaryAsync(email.Email, tripDisplayNumber, $"<a href=\"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}/Trip/TripSummary/{dto.TripSummaryId}\">Trip Summary</a>"); 
                }
            }

			EmailList dbList = _context.EmailLists.Where(el => el.UserId == user.Id).FirstOrDefault();

			if (dbList == null)
			{
				_context.EmailLists.Add(new EmailList(dto));
				_context.SaveChanges();
			}
			else
			{
				EmailListDTO dbDTO = new EmailListDTO(dbList);

				foreach (var email in dto.Emails)
				{
					var dbEmail = dbDTO.Emails
						.Where(e => e.Email == email.Email)
						.FirstOrDefault();

					if (dbEmail == null)
					{
						dbDTO.Emails.Add(email);
					}
					else
					{
						dbEmail.LastEmailed = DateTime.Now;
					}
				}

				// REMOVE emails not in dto
				dbDTO.Emails = dbDTO.Emails
					.Where(e => dto.Emails.Any(d => d.Email == e.Email))
					.ToList();

				dbList.Emails = JsonConvert.SerializeObject(dbDTO.Emails);
				_context.EmailLists.Update(dbList);
				_context.SaveChanges();
			}

			return RedirectToAction("TripSummary", new {id = dto.TripSummaryId});
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SummaryPDF(int id)
        {
            TripSummaryDb dbTrip = _context.Trips.Where(t => t.Id == id).FirstOrDefault();
            TripSummary trip = JsonConvert.DeserializeObject<TripSummary>(dbTrip.Details);
            trip.Id = dbTrip.Id;
            var aircraft = _context.Aircrafts.Where(a => a.TailNumber == trip.AircraftNumber && a.IsEnabled).FirstOrDefault();
            
            var aircraftName = aircraft != null ? aircraft.Manufacturer + " " + aircraft.Type : "";
            ViewData["AircraftName"] = aircraftName; 

            return View(trip);
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ShortSummaryPDF(int id)
        {
            TripSummaryDb dbTrip = _context.Trips.Where(t => t.Id == id).FirstOrDefault();
            TripSummary trip = JsonConvert.DeserializeObject<TripSummary>(dbTrip.Details);

            trip.Id = dbTrip.Id;
            var aircraft = _context.Aircrafts.Where(a => a.TailNumber == trip.AircraftNumber && a.IsEnabled).FirstOrDefault();
            
            var aircraftName = aircraft != null ? aircraft.Manufacturer + " " + aircraft.Type : "";
            ViewData["AircraftName"] = aircraftName; 

            return View(trip);
        }

        public PythonTripDTO ParseItinerary(string itinerary)
        {
            string fileName = _config["PythonPathItineraryReader"];
            string model = _config["PythonModel"];

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = _config["PythonPath"];
            start.Arguments = string.Format("{0} {1} {2}", fileName, itinerary, model);
            try
            {
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                string file_details = "";
                using (Process process = Process.Start(start))
                {
                    using (StreamReader reader = process.StandardOutput)

                    {
                        file_details = reader.ReadToEnd();
                    }
                }
                if (file_details.Length > 0)
                {
                    if (file_details.Contains("IMPORT ERROR:"))
                    {
                        return new PythonTripDTO { errors = new List<string> { file_details } };
                    }
                    var decoded = JsonConvert.DeserializeObject<PythonTripDTO>(file_details);
                    return decoded;
                }
                else return null;
            }
            catch (Exception ex)
            {
                return new PythonTripDTO { errors = new List<string> { ex.Message } };
            }
            
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
        [HttpGet]
        public byte[] ExportSummaryPdf(int id, bool isFullSummary)
        {
            try
            {
                HtmlToPdfConverter htmlToPdfConverter = new HtmlToPdfConverter();

                var domainName = _config["EvoPDFDomain"];

                htmlToPdfConverter.LicenseKey = "pSs7Kjk5Kjs5Kjs7JDoqOTskOzgkMzMzMyo6";
                var webRoot = _host.WebRootPath;
                string url = domainName + ( isFullSummary ? "/Trip/SummaryPDF/" : "/Trip/ShortSummaryPDF/") + id;

                string htmlStringWithPageNumbers = "<span style='text-align:right;display:block;width:100%;'>&p;</span>";

                HtmlToPdfVariableElement footerHtmlWithPageNumbers = new HtmlToPdfVariableElement(htmlStringWithPageNumbers, url);

                // Set the HTML element to fit the container height
                //footerHtmlWithPageNumbers.FitHeight = true;

                // Add variable HTML element with page numbering to footer
                htmlToPdfConverter.PdfFooterOptions.AddElement(footerHtmlWithPageNumbers);


                htmlToPdfConverter.PdfDocumentOptions.ShowFooter = true;
                htmlToPdfConverter.PdfDocumentOptions.PdfPageSize = PdfPageSize.Letter;
                htmlToPdfConverter.PdfDocumentOptions.PdfPageOrientation = PdfPageOrientation.Portrait;

                htmlToPdfConverter.PdfFooterOptions.FooterHeight = 18;
                htmlToPdfConverter.PdfFooterOptions.PageNumberingStartIndex = 0;
                htmlToPdfConverter.PdfFooterOptions.PageNumberingPageCountIncrement = 1;


                htmlToPdfConverter.PdfDocumentOptions.TopMargin = 18;
                htmlToPdfConverter.PdfDocumentOptions.RightMargin = 36;
                htmlToPdfConverter.PdfDocumentOptions.BottomMargin = 18;
                htmlToPdfConverter.PdfDocumentOptions.LeftMargin = 36; //.Margins = 20;      


                htmlToPdfConverter.HtmlViewerWidth = (int)PdfPageSize.Ledger.Height;

                htmlToPdfConverter.PdfDocumentOptions.StretchToFit = true;

                /*htmlToPdfConverter.HtmlViewerHeight = (int)PdfPageSize.Ledger.Height;
                htmlToPdfConverter.HtmlViewerWidth = (int)PdfPageSize.Ledger.Width;
                  */

                string filepath = _config["SummariesPath"] + "Trip" + id + (isFullSummary ? "FullSummary" : "QuickRef") + ".pdf";
                // Convert the HTML page given by an URL to a PDF document in a memory buffer
                byte[] outPdfBuffer = htmlToPdfConverter.ConvertUrl(url);
                System.IO.File.WriteAllBytes(filepath, outPdfBuffer);

                //return filepath;
                return outPdfBuffer;
            }
            catch
            {
                return null;
            }

        }
        public async Task<List<LegDTO>> UploadExtraItinerary(IFormFile formFile)
        {
            List<LegDTO> newLegs = new List<LegDTO>();
           
            ApplicationUser user = await _signInManager.UserManager.GetUserAsync(User);
            var aircrafts = _context.Aircrafts.Where(a => a.UserCompany == user.Id && a.IsEnabled).ToList();

            var jobDirectory = _config["ItinerariesPath"];
            if (!Directory.Exists(jobDirectory))
            {
                Directory.CreateDirectory(jobDirectory);
            }

            //make sure file has unique name
            var fileext = Guid.NewGuid().ToString() + formFile.FileName.Replace(" ", "");
            while (System.IO.File.Exists(jobDirectory + fileext))
            {
                fileext = Guid.NewGuid().ToString() + formFile.FileName.Replace(" ", "");

            }

            var filePath = Path.Combine(jobDirectory, fileext);

            using (var stream = System.IO.File.Create(filePath))
            {
                await formFile.CopyToAsync(stream);
            }


            PythonTripDTO pythonTrip = ParseItinerary(filePath);
            if (pythonTrip == null)
            {
                newLegs.Add(new LegDTO
                {
                    Departure = "",
                    Destination = "",
                    WaivedFee = new FeeDTO(),
                    PassengerWeights = "0",
                    Baggage = 0,
                    DepartureFuelPrices = new List<FuelPrice> { new FuelPrice() },
                    CrewWeight = "0",
                    FuelBurn = 0,
                    Distance = 0,
                    Reserve = 0,
                    TaxiFuelBurn = 0,
                    MaxRampWeight = 0
                });

                return newLegs;
            }

            var aircraft = aircrafts.Where(a => a.TailNumber == pythonTrip.aircraft).FirstOrDefault();
            aircraft = aircraft ?? new Aircraft();

            foreach (PythonLegDTO pythonLeg in pythonTrip.legs)
            {
                var waivedFee = pythonLeg.fees.Where(f => f.airport == pythonLeg.destination && f.is_waivable).FirstOrDefault();
                var paxWeights = pythonLeg.passengers.Select(p => { p = p == -1 ? aircraft.DefaultPaxWeight : p; return p; }).ToList();
                newLegs.Add(new LegDTO
                {
                    Departure = pythonLeg.departure,
                    Destination = pythonLeg.destination,
                    WaivedFee = new FeeDTO(waivedFee),
                    PassengerWeights = paxWeights != null ? string.Join(", ", paxWeights) : "0",
                    Baggage = aircraft == null ? 0 : pythonLeg.passengers == null || pythonLeg.passengers.Count() == 0 ? aircraft.DefaultBaggageNoPax : aircraft.DefaultBaggageWithPax, //pythonLeg.baggage + ((pythonLeg.passengers.Count() + pythonLeg.crew_weight.Count()) * 50),
                    DepartureFuelPrices = pythonLeg.departure_fuel_price.Count() > 0 ? pythonLeg.departure_fuel_price.Select(fp => { return new FuelPrice(fp); }).ToList() : new List<FuelPrice> { new FuelPrice() },
                    CrewWeight = aircraft != null ? aircraft.DefaultPICWeight + ", " + aircraft.DefaultSICWeight + ", " + aircraft.DefaultCabinWeight: "",//pythonLeg.crew_weight != null ? string.Join(", ", pythonLeg.crew_weight) : "180",
                    FuelBurn = pythonLeg.fuel_burn,
                    Distance = pythonLeg.distance,
                    Reserve = pythonLeg.reserve,
                    TaxiFuelBurn = pythonLeg.taxi_fuel_burn,
                    MaxRampWeight = pythonLeg.max_ramp_weight
                });
            }

            return newLegs;
        }
    }
}
