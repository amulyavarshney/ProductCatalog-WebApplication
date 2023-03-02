namespace WebApplication2.Models
{
    public class Manufacturer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string GSTIN { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public int Pin { get; set; }
        public List<ProductManufacturer> ProductManufacturers { get; set; }
    }
}
