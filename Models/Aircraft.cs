using Microsoft.EntityFrameworkCore;

namespace SkyIQ.Models
{
    public class Aircraft
    {
        public int Id { get; set; }
        public string TailNumber { get; set; }
        public double BasicEmptyWeight { get; set; }
        public double MaxTakeoffWeight { get; set; }
        public double MaxLandingWeight { get; set; }
        public double PreferredReserve { get; set; }
        public double MaxFuelCapacity { get; set; }
        public string Manufacturer { get; set; }
        public string Type { get; set; }
        public string UserCompany { get; set; }

        public double TaxiFuelBurn { get; set; }
        public double MaxRampWeight { get; set; }
        public double DefaultPaxWeight { get; set; }
        public double DefaultBaggageWithPax { get; set; }
        public double DefaultBaggageNoPax { get; set; }
        public double DefaultPICWeight { get; set; }
        public double DefaultSICWeight { get; set; }
        public double DefaultCabinWeight { get; set; }
        public bool IsEnabled { get; set; }

        public int CarryTypeId { get; set; }
        public double PenaltyRate { get; set; }
        public double CruiseFuelBurn { get; set; }
    }
}
