using System.Collections.Generic;

namespace TourAgencyApp
{
    internal class Tour
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int TicketCount { get; set; }
        public decimal Price { get; set; }
        public bool IsActual { get; set; }
        public string CountryCode { get; set; } = "";
        public string Description { get; set; } = "";
        public byte[]? ImagePreview { get; set; }
        public List<string> Types { get; set; } = new();
    }
}
