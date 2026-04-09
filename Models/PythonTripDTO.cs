using Microsoft.EntityFrameworkCore;

namespace SkyIQ.Models
{
    public class PythonTripDTO
    {
        public string itinerary_num { get; set; }
        public double starting_fuel { get; set; }
        public string aircraft { get; set; }
        public List<PythonLegDTO> legs { get; set; }
        public double minimum_fuel_reserve { get; set; }
        public double max_fuel_reserve { get; set; }
        public double max_takeoff_weight { get; set; }
        public double max_landing_weight { get; set; }
        public double basic_empty_weight { get; set; }
        public double penalty { get; set; }
        public double lbs_per_hour { get; set; }
        public List<string> errors { get; set; }

        public PythonTripDTO() {
            legs = new List<PythonLegDTO>();
        }
        public PythonTripDTO(TripDTO trip)
        {
            itinerary_num = trip.ItineraryNum;
            starting_fuel = trip.StartingFuel;
            aircraft = trip.AircraftId;
            legs = trip.Legs.Select(l => new PythonLegDTO(l)).ToList();
            //minimum_fuel_reserve = trip.Reserve;
            max_fuel_reserve = trip.MaxFuelReserve;
            max_takeoff_weight = trip.MaxTakeoffWeight;
            max_landing_weight = trip.MaxLandingWeight;
            basic_empty_weight = trip.BasicEmptyWeight;
            penalty = trip.Penalty;
            lbs_per_hour = trip.LbsPerHour; 
            errors = new List<string>(); //dont need error info when passing from .net to python flight calculator
        }
    }

    public class PythonLegDTO
    {
        public string departure { get; set; }
        public string destination { get; set; }
        public List<PythonFuelPriceDTO> arrival_fuel_price { get; set; }
        public List<PythonFuelPriceDTO> departure_fuel_price { get; set; }
        public List<PythonFeeDTO> fees { get; set; }
        public double reserve { get; set; }
        public double fuel_burn { get; set; }
        public List<double> passengers  { get; set; }//list of weights
        public double baggage { get; set; }
        public string leg_num { get; set; }
        public double distance { get; set; }
        public List<double> crew_weight { get; set; }
        public double max_takeoff_weight { get; set; }
        public double max_landing_weight { get; set; }
        public double taxi_fuel_burn { get; set; }
        public double max_ramp_weight { get; set; }


        public PythonLegDTO() {
            arrival_fuel_price = new List<PythonFuelPriceDTO>();
            departure_fuel_price = new List<PythonFuelPriceDTO>();
            fees = new List<PythonFeeDTO>(); 
        }
        public PythonLegDTO(LegDTO leg)
        {
            departure = leg.Departure;
            destination = leg.Destination;
            departure_fuel_price = leg.DepartureFuelPrices.Select(p => new PythonFuelPriceDTO(p)).ToList();
            fees = new List<PythonFeeDTO> { new PythonFeeDTO(leg.WaivedFee) };
            reserve = leg.Reserve;
            fuel_burn = leg.FuelBurn;
            passengers = leg.PassengerWeights != null ? leg.PassengerWeights.Split(",").ToList().Select(l => { return Convert.ToDouble(l); }).ToList() : new List<double> { 0.0 };
            baggage = leg.Baggage;
            leg_num = leg.LegNum.ToString();
            distance = leg.Distance;
            crew_weight = leg.CrewWeight != null ? leg.CrewWeight.Split(",").ToList().Select(l => { return Convert.ToDouble(l); }).ToList() : new List<double> { 180 } ;
            max_takeoff_weight = leg.MaxTakeoffWeight;
            max_landing_weight = leg.MaxLandingWeight;
            max_ramp_weight = leg.MaxRampWeight;
            taxi_fuel_burn = leg.TaxiFuelBurn;
        }
    }

    public class PythonFeeDTO
    {
        public string name { get; set; }
        public double amount { get; set; }
        public bool is_waivable { get; set; }
        public double waived_at { get; set; }
        public string airport { get; set; }

        public PythonFeeDTO() { }
        public PythonFeeDTO(FeeDTO fee)
        {
            name = fee.Name;
            amount = fee.Amount;
            is_waivable = fee.IsWaivable;
            waived_at = fee.WaivedAt;
            airport = fee.Airport;
        }
    }

    public class PythonFuelPriceDTO
    {
        public double min_fuel { get; set; }
        public double price { get; set; }

        public PythonFuelPriceDTO() { }
        public PythonFuelPriceDTO(FuelPrice fuel) {
            min_fuel = fuel.MinFuel;
            price = fuel.Price;
        }
    }
}
