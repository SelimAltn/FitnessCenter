namespace FitnessCenter.Web.Models.ViewModels
{
    public class LoginViewModel
    {
        public string KullaniciAdiVeyaEmail { get; set; } = null!;
        public string Sifre { get; set; } = null!;
        public bool BeniHatirla { get; set; }
    }
}
