using System;
using System.Collections.Generic;

namespace NetflixRecommendationSystem.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OriginalTitle { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public int Year { get; set; }
        public double Rating { get; set; }
        public string CoverPath { get; set; } = string.Empty;
        public string Locale { get; set; } = string.Empty;
        public bool AvailableGlobally { get; set; }
        public DateTime ReleaseDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public long Runtime { get; set; }

        public List<string> GetGenres()
        {
            return string.IsNullOrEmpty(Genre) ? new List<string>() : 
                   new List<string>(Genre.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }

        public List<string> GetTags()
        {
            return string.IsNullOrEmpty(Tags) ? new List<string>() : 
                   new List<string>(Tags.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }

        public override string ToString()
        {
            return $"[{Id}] {Title} ({Year}) - Rating: {Rating:F1}/10 - Genre: {Genre}";
        }
    }
}
