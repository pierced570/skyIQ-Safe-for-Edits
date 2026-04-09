using System.ComponentModel.DataAnnotations.Schema;

namespace SkyIQ.Models
{
    public class TripDTO
    {
        public int Id { get; set; }
        public string ItineraryNum { get; set; }
        public List<LegDTO> Legs { get; set; }
        public double StartingFuel { get; set; }
        public string AircraftId { get; set; }
        //public double Reserve { get; set; }
        public double MaxFuelReserve { get; set; }
        public double MaxTakeoffWeight { get; set; }
        public double MaxLandingWeight { get; set; }
        public double BasicEmptyWeight { get; set; }
        public double Penalty { get; set; }
        public double LbsPerHour { get; set; }
        public List<string> ImportErrors { get; set; }

        public TripDTO()
        {
            Legs = new List<LegDTO>();
            ImportErrors = new List<string>();
        }
    }
    public class LegDTO
    {
        public int LegNum { get; set; }
        public string Departure { get; set; }
        public string Destination { get; set; }
        public List<FuelPrice> DepartureFuelPrices { get; set; }
        public FeeDTO WaivedFee { get; set; }
        public List<FeeDTO> OtherFees { get; set; }
        public double Baggage { get; set; }
        public double Distance { get; set; }
        public double FuelBurn { get; set; }
        public string CrewWeight { get; set; }
        public string PassengerWeights { get; set; }
        public double MaxTakeoffWeight { get; set; }
        public double MaxLandingWeight { get; set; }
        public double Reserve { get; set; }

        public double TaxiFuelBurn { get; set; }
        public double MaxRampWeight { get; set; }
        public double DefaultPaxWeight { get; set; }
        public double DefaultBaggageWeight { get; set; }
        public double DefaultCrewWeight { get; set; }

        public LegDTO()
        {
            DepartureFuelPrices = new List<FuelPrice> (); 
            OtherFees = new List<FeeDTO>();
            PassengerWeights = "0";
            CrewWeight = "0";//default from skyiq
        }

    }
    public class FuelPrice
    {
        public double MinFuel { get; set; }
        public double Price { get; set; }

        public FuelPrice() { }
        public FuelPrice(PythonFuelPriceDTO price)
        {
            MinFuel = price.min_fuel;
            Price = price.price;
        }
    }
    public class FeeDTO
    {
        public string Name { get; set; }
        public double Amount { get; set; }
        public bool IsWaivable { get; set; }
        public double WaivedAt { get; set; }
        public string Airport { get; set; }

        public FeeDTO() { }
        public FeeDTO(PythonFeeDTO fee)
        {
            Name = fee != null ? fee.name : null;
            Amount = fee != null ? fee.amount : 0;
            IsWaivable = fee != null ? fee.is_waivable : false;
            WaivedAt = fee != null ? fee.waived_at : 0;
            Airport = fee != null ? fee.airport : null;
        }
    }
}
