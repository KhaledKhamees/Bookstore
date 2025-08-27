using Microsoft.AspNetCore.Identity;

namespace UserService.Models
{
    public class ApplicationRole : IdentityRole
    {
        // Additional properties can be added here
        public ApplicationRole() { }
        public ApplicationRole(string roleName) : base(roleName)
        {
        }
    }
}
