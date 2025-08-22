using System.Collections.Generic;

namespace NetflixRecommendationSystem.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<int> SelectedMovieIds { get; set; } = new List<int>();
        public List<Movie> SelectedMovies { get; set; } = new List<Movie>();

        public override string ToString()
        {
            return $"User: {Name} (ID: {Id}) - Selected Movies: {SelectedMovieIds.Count}";
        }
    }
}
