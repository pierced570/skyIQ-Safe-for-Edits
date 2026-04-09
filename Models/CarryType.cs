namespace SkyIQ.Models
{
    public class CarryType
    {
        public int Id { get; set;  }
        public string Name { get; set; }
        public double PenaltyRate { get; set; }
        public double CruiseFuelBurn { get; set; } //fuelburn/hour
    }
}
