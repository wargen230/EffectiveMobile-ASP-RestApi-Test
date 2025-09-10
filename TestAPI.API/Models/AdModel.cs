namespace TestAPI.Models
{
    public class AdModel
    {
        public string Name { get; set; }
        public List<string> Locations { get; set; } = new ();
    }
}
