namespace SkyIQ.Models
{
    public class PythonTripSummary
    {
        public double simple_cost { get; set; }
        public List<PythonTripSummaryLeg> data { get; set; }
    }
    public class PythonTripSummaryLeg
    {
        public string name { get; set; }
        public string arrival { get; set; }
        public string departure { get; set; }
        public List<PythonFeeDTO> fees { get; set; }
        public double fuel_burn_lbs { get; set; } 
        public double start_fuel { get; set; }
        public double takeoff_fuel_lbs { get; set; }
        public double landing_fuel_lbs { get; set; }
        public double total_fuel_purchased_gallons { get; set; }
        public double total_fuel_purchased_lbs { get; set; }
        public double total_cost { get; set; }
        public double avg_cost_per_gallon { get; set; }        
        public double fixed_weights { get; set; }
        public List<string> errors { get; set; }
    }

}
