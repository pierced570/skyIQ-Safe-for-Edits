using System.ComponentModel.DataAnnotations.Schema;

namespace SkyIQ.Models
{
    public class Trip
    {
        public int Id { get; set; }
        public string ItineraryNum { get; set; }
        public List<Leg> Legs { get; set; }
        public double StartingFuel { get; set; }
        public string AircraftId { get; set; }

        public Trip()
        {
            Legs = new List<Leg>();
        }
    }

    public class Leg
    {
        public int LegNum { get; set; }
        public string Departure { get; set; }
        public string Destination { get; set; }
        public double ArrivalFuelPrice { get; set; }
        public double DepartureFuelPrice { get; set; }
        public string Fees { get; set; }
        public int NumPassengers { get; set; }
        public List<double> PassengerWeights { get; set; }
        public double Baggage { get; set; }
        public double Reserve { get; set; }
        public double MaxTakeoffWeight { get; set; }
        public double MaxLandingWeight { get; set; }
        public double FuelBurn { get; set; }
        public double Distance { get; set; }

        [NotMapped]
        public string PassengerWeightsStr { get; set; }

        public Leg()
        {
            PassengerWeights = new List<double>();
        }
    }
}
