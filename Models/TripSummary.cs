using Newtonsoft.Json;
using NuGet.Protocol;
using System;

namespace SkyIQ.Models
{
    public class TripSummary
    {
        public int Id { get; set; }
        public string ? ItineraryNum { get; set; }
        public List<TripSummaryLeg> Legs { get; set; }
        public string AircraftNumber { get; set; }
        public double Savings { get; set; }
        //public string ? FileName { get; set; }

        public TripSummary() {
            Legs = new List<TripSummaryLeg>();
        }

        public TripSummary(int id, string itineraryNum, List<PythonTripSummaryLeg> legs, string aircraftNumber, double simpleCost)
        {
            Id = id;
            ItineraryNum = itineraryNum;
            Legs = legs.Select(l => new TripSummaryLeg(l)).ToList();
            AircraftNumber = aircraftNumber;
            Savings = simpleCost - Legs.Select(l => l.TotalCost).Sum();
            if(Savings < 0)
            {
                Savings = 0;
            }
        }
    }

    public class TripSummaryLeg { 
        public string Arrival { get; set; }
        public string Departure { get; set; }
        public double TakeoffWeight { get; set; }
        public double LandingWeight { get; set; }
        public double StartFuel { get; set; }
        public double TakeoffFuel { get; set; }
        public double LandingFuel { get; set; }
        public double FuelBurn { get; set; }
        public double FuelUpliftLbs { get; set; }
        public double FuelUpliftGals { get; set; }
        public double FuelCost { get; set; }
        public bool HasWaivedFee { get; set; }
        public double FeeMin { get; set; }
        public double TotalCost { get; set; }
        public List<string> Errors { get; set; }

        public TripSummaryLeg() 
        {
            Errors = new List<string>();
        }
        public TripSummaryLeg(PythonTripSummaryLeg leg)
        {
            Arrival = leg.arrival;
            Departure = leg.departure;
            FuelCost = leg.total_cost;
            HasWaivedFee = false;
            if(leg.fees.Count() > 0)
            {
                var fee = leg.fees.Where(f => f.is_waivable && f.amount > 0 && f.waived_at > 0).FirstOrDefault(); 
                if(fee != null)
                {
                    HasWaivedFee = true;
                    FeeMin = fee.waived_at;
                    FuelCost = leg.total_fuel_purchased_gallons < fee.waived_at ? leg.total_cost - fee.amount : leg.total_cost; 
                }
            }
            LandingFuel = ((int)Math.Ceiling(leg.landing_fuel_lbs / 10.0)) * 10;
            TakeoffFuel = ((int)Math.Ceiling(leg.takeoff_fuel_lbs / 10.0)) * 10;
            StartFuel = ((int)Math.Ceiling(leg.start_fuel / 10.0)) * 10;
            TakeoffWeight = leg.takeoff_fuel_lbs + leg.fixed_weights;
            LandingWeight = leg.landing_fuel_lbs + leg.fixed_weights;
            FuelBurn = leg.fuel_burn_lbs;
            FuelUpliftLbs = leg.total_fuel_purchased_lbs;
            FuelUpliftGals = leg.total_fuel_purchased_gallons;
            TotalCost = leg.total_cost;
            Errors = leg.errors ?? new List<string>(); 
        }
    }

    public class TripSummaryDb
    {
        public int Id { get; set; }
        public string ? ItineraryNum { get; set; }
        public string Details { get; set; }
        public DateTime CreatedOn { get; set; }
        public string UserCompany { get; set; }
        public string ItineraryDetails { get; set; }
        public double Savings { get; set; }
        //public string ? FileName { get; set; }

        public TripSummaryDb() { }
        public TripSummaryDb(TripSummary summary, TripDTO itinerary)
        {
            ItineraryNum = summary.ItineraryNum;
            Details = JsonConvert.SerializeObject(summary);
            ItineraryDetails = JsonConvert.SerializeObject(itinerary);
            Savings = summary.Savings;
        }

        public void Update(TripSummary summary, TripDTO itinerary)
        {
            ItineraryNum = summary.ItineraryNum;
            Details = JsonConvert.SerializeObject(summary);
            ItineraryDetails = JsonConvert.SerializeObject(itinerary);
            Savings = summary.Savings;
        }
    }
}
