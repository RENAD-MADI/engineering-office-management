namespace RenadWebApp.Models.ViewModels
{
    public class EngSummaryViewModel
    {
        public int Id { get; set; }
        public string? EngName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Offices { get; set; }
        public int TotalRatioForEng { get; set; }
        public int RestOfEngShow { get; set; }

        /// <summary>What the engineer has already been paid.</summary>
        public int EngTake => TotalRatioForEng - RestOfEngShow;
    }
}
