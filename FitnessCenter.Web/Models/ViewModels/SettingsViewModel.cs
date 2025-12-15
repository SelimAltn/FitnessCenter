namespace FitnessCenter.Web.Models.ViewModels
{
    /// <summary>
    /// Ayarlar sayfası için ViewModel
    /// </summary>
    public class SettingsViewModel
    {
        /// <summary>
        /// Tema tercihi: "Light" veya "Dark"
        /// </summary>
        public string ThemePreference { get; set; } = "Light";
    }
}
