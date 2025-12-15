using Microsoft.AspNetCore.Identity;

namespace FitnessCenter.Web.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Kullanıcının tema tercihi: "Light" veya "Dark"
        /// </summary>
        public string? ThemePreference { get; set; }
    }
}
