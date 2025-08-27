using Microsoft.AspNetCore.Identity;
namespace UserService.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }
}
