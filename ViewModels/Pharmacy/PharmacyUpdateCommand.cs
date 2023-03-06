namespace DuyProject.API.ViewModels.Pharmacy
{
    public class PharmacyUpdateCommand
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Drugs { get; set; }
        public string DoctorId { get; set; }
        public string LogoId { get; set; }
        public string Column { get; set; }
        public string ReferenceImage { get; set; }
        public string Type { get; set; }
    }
}