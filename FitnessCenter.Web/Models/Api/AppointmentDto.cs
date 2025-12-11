using System;

namespace FitnessCenter.Web.Models.Api
{
    public class AppointmentDto
    {
        public int Id { get; set; }

        public DateTime BaslangicZamani { get; set; }
        public DateTime BitisZamani { get; set; }

        public string HizmetAdi { get; set; } = string.Empty;
        public string EgitmenAdSoyad { get; set; } = string.Empty;
        public string SalonAdi { get; set; } = string.Empty;

        public string Durum { get; set; } = string.Empty;
    }
}
