using Microsoft.AspNetCore.Identity;

namespace SkyIQ.Models
{
    public class ApplicationUser : IdentityUser 
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Company { get; set; }
        public string RoleName { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
