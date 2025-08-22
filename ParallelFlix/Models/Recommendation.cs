using System;

namespace NetflixRecommendationSystem.Models
{
    public class Recommendation
    {
        public Movie Movie { get; set; } = null!;
        public double Score { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Algorithm { get; set; } = string.Empty;
        public DateTime CalculatedAt { get; set; }

        public override string ToString()
        {
            return $"{Movie.Title} (Score: {Score:F2}) - {Reason}";
        }
    }
}
