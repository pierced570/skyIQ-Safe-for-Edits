using Newtonsoft.Json;

namespace SkyIQ.Models
{
    public class EmailList
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Emails { get; set; }

        public EmailList() { }
        public EmailList(EmailListDTO emailList)
        {
            Id = emailList.Id;
            UserId = emailList.UserId;
            Emails = JsonConvert.SerializeObject(emailList.Emails);
        }
    }

    public class EmailListDTO
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public List<EmailDTO> Emails { get; set; }
        public string ItineraryNum { get; set; }
        public string AircraftNum { get; set; }
        public int TripSummaryId { get; set; }

        public EmailListDTO() 
        {
            Emails = new List<EmailDTO>();
        }
        public EmailListDTO(EmailList emailList) 
        {
            Id = emailList.Id; 
            UserId = emailList.UserId;
            Emails = JsonConvert.DeserializeObject<List<EmailDTO>>(emailList.Emails);
        }
    }
    public class EmailDTO
    {
        public string Email { get; set; }
        public DateTime LastEmailed { get; set; }    
        public bool IsChecked { get; set; }
    }
}
