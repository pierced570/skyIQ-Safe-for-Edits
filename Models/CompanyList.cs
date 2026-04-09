namespace SkyIQ.Models
{
    using System.Collections.Generic;

    public class CompanyList
    {
        public int TripsRun { get; set; }
        public int TailNumbers { get; set; }
        public int NumCompanies { get; set; }
    }

    public class CopmanyListTableRow
    {
        public string Id { get; set; }
        public string Company { get; set; }
        public string Name { get; set; }
        public int NumTrips { get; set; }
        public int NumTails { get; set; }
        public string SavingsAccrued { get; set; }
    }
}
