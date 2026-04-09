namespace SkyIQ.Models
{
    public class CompanyDTO
    {
        public string Id { get; set; }
        public string Company { get; set; }
        public bool IsEnabled { get; set; }
        public List<TailDTO> Tails {get; set;} 
    }
    public class TailDTO
    {
        public string TailNumber { get; set; }
        public string ManufacturerModel { get; set; }
        public int TripsRun30 { get; set; }
        public int TripsRun { get; set; }
        public double Savings { get; set; }
        public DateTime ? LastRun { get; set; }
    }
    
    public class TailFormattedDTO
    {
        public string TailNumber { get; set; }
        public string ManufacturerModel { get; set; }
        public string TripsRun30 { get; set; }
        public string TripsRun { get; set; }
        public string Savings { get; set; }
        public DateTime ? LastRun { get; set; }

        public TailFormattedDTO() { }
        public TailFormattedDTO(TailDTO tail)
        {
            TailNumber = tail.TailNumber;
            ManufacturerModel = tail.ManufacturerModel;
            TripsRun30 = NumberFormatter.FormatNumber(tail.TripsRun30);
            TripsRun = NumberFormatter.FormatNumber(tail.TripsRun);
            Savings = NumberFormatter.FormatNumber(tail.Savings);
            LastRun = tail.LastRun;
        }
    }

}
