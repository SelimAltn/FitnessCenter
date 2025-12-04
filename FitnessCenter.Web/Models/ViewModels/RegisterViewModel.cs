namespace FitnessCenter.Web.Models.ViewModels
{
    public class RegisterViewModel
    {
        public string KullaniciAdi { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Sifre { get; set; } = null!;
        public string SifreTekrar { get; set; } = null!;
    }
}
