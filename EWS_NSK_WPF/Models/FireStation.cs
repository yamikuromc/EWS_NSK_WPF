namespace EWS_NSK_WPF.Models
{
    public class FireStation
    {
        public int StationId { get; set; }
        public string StationName { get; set; }
        public double CenterLat { get; set; }
        public double CenterLng { get; set; }
        public double RadiusMeters { get; set; }
        public string Address { get; set; }
    }
}