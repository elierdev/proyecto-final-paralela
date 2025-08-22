using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetflixRecommendationSystem.Models;
using NetflixRecommendationSystem.Recommender;

namespace NetflixRecommendationSystem.UI
{
    public class ConsoleInterface
    {
        private const int MOVIES_PER_PAGE = 10;

        public void ShowWelcome()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(@"
    ███╗   ██╗███████╗████████╗███████╗██╗     ██╗██╗  ██╗
    ████╗  ██║██╔════╝╚══██╔══╝██╔════╝██║     ██║╚██╗██╔╝
    ██╔██╗ ██║█████╗     ██║   █████╗  ██║     ██║ ╚███╔╝ 
    ██║╚██╗██║██╔══╝     ██║   ██╔══╝  ██║     ██║ ██╔██╗ 
    ██║ ╚████║███████╗   ██║   ██║     ███████╗██║██╔╝ ██╗
    ╚═╝  ╚═══╝╚══════╝   ╚═╝   ╚═╝     ╚══════╝╚═╝╚═╝  ╚═╝");
            
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n    🎬 SISTEMA DE RECOMENDACIONES AVANZADO 🎬");
            Console.WriteLine("    ═══════════════════════════════════════════");
            Console.ResetColor();
        }

        public List<Movie> SelectMovies(List<Movie> allMovies)
        {
            var selectedMovies = new List<Movie>();
            var currentPage = 0;
            var totalPages = (int)Math.Ceiling((double)allMovies.Count / MOVIES_PER_PAGE);

            while (true)
            {
                Console.Clear();
                ShowWelcome();
                
                Console.WriteLine($"\n📚 CATÁLOGO DE PELÍCULAS (Página {currentPage + 1}/{totalPages})");
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                
                var startIndex = currentPage * MOVIES_PER_PAGE;
                var endIndex = Math.Min(startIndex + MOVIES_PER_PAGE, allMovies.Count);
                
                for (int i = startIndex; i < endIndex; i++)
                {
                    var movie = allMovies[i];
                    var isSelected = selectedMovies.Any(s => s.Id == movie.Id);
                    var prefix = isSelected ? "✅" : "  ";
                    var color = isSelected ? ConsoleColor.Green : ConsoleColor.White;
                    
                    Console.ForegroundColor = color;
                    Console.WriteLine($"{prefix} {i + 1:D2}. {movie}");
                    Console.ResetColor();
                }

                Console.WriteLine("\n═══════════════════════════════════════════════════════════");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"🎯 Películas seleccionadas: {selectedMovies.Count}");
                Console.ResetColor();
                
                Console.WriteLine("\n📋 OPCIONES:");
                Console.WriteLine("   • Número(s) - Seleccionar/deseleccionar película (ej: 1,5,7)");
                Console.WriteLine("   • [N] - Siguiente página");
                Console.WriteLine("   • [P] - Página anterior");
                Console.WriteLine("   • [R] - Obtener recomendaciones");
                Console.WriteLine("   • [A] - Análisis de rendimiento (Speedup/Eficiencia)");
                Console.WriteLine("   • [Q] - Salir");
                
                Console.Write("\n➤ Selección: ");
                var input = Console.ReadLine()?.Trim().ToUpper();

                if (string.IsNullOrEmpty(input)) continue;

                switch (input)
                {
                    case "N":
                        if (currentPage < totalPages - 1) currentPage++;
                        break;
                    case "P":
                        if (currentPage > 0) currentPage--;
                        break;
                    case "R":
                        if (selectedMovies.Count == 0)
                        {
                            ShowMessage("⚠️  Selecciona al menos una película para obtener recomendaciones.", ConsoleColor.Yellow);
                            continue;
                        }
                        return selectedMovies;
                    case "A":
                        if (selectedMovies.Count == 0)
                        {
                            ShowMessage("⚠️  Selecciona al menos una película para realizar análisis de rendimiento.", ConsoleColor.Yellow);
                            continue;
                        }
                        // Marcar que se solicitó análisis de rendimiento
                        selectedMovies.Add(new Movie { Id = -1, Title = "PERFORMANCE_ANALYSIS" });
                        return selectedMovies;
                    case "Q":
                        Environment.Exit(0);
                        break;
                    default:
                        // Procesar números de película
                        var numbers = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var numStr in numbers)
                        {
                            if (int.TryParse(numStr.Trim(), out int movieNum) && 
                                movieNum >= 1 && movieNum <= allMovies.Count)
                            {
                                var movie = allMovies[movieNum - 1];
                                if (selectedMovies.Any(s => s.Id == movie.Id))
                                {
                                    selectedMovies.RemoveAll(s => s.Id == movie.Id);
                                }
                                else
                                {
                                    selectedMovies.Add(movie);
                                }
                            }
                        }
                        break;
                }
            }
        }

        public void ShowRecommendationsRealTime()
        {
            Console.Clear();
            ShowWelcome();
            Console.WriteLine("\n🔍 GENERANDO RECOMENDACIONES EN TIEMPO REAL...");
            Console.WriteLine("═══════════════════════════════════════════════════════");
        }

        public void ShowRecommendation(Recommendation recommendation, int count)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n🎬 #{count:D2} {recommendation.Movie.Title} ({recommendation.Movie.Year})");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"    ⭐ {recommendation.Movie.Rating:F1}/10 | 🎭 {recommendation.Movie.Genre}");
            Console.WriteLine($"    💡 {recommendation.Reason}");
            Console.WriteLine($"    🤖 Algoritmo: {recommendation.Algorithm} | 📊 Score: {recommendation.Score:F2}");
            Console.ResetColor();
        }

        public void ShowStatusUpdate(string status)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{status}");
            Console.ResetColor();
        }

        public void ShowFinalRecommendations(List<Recommendation> recommendations)
        {
            Console.WriteLine("\n\n🏆 TOP RECOMENDACIONES FINALES");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            
            for (int i = 0; i < recommendations.Count; i++)
            {
                var rec = recommendations[i];
                Console.ForegroundColor = i < 3 ? ConsoleColor.Yellow : ConsoleColor.White;
                
                var medal = i switch
                {
                    0 => "🥇",
                    1 => "🥈",
                    2 => "🥉",
                    _ => $"{i + 1:D2}."
                };
                
                Console.WriteLine($"{medal} {rec.Movie.Title} ({rec.Movie.Year})");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"    ⭐ {rec.Movie.Rating:F1}/10 | 📊 Score: {rec.Score:F2}");
                Console.WriteLine($"    🎭 {rec.Movie.Genre}");
                Console.WriteLine($"    💡 {rec.Reason}");
                Console.ResetColor();
                Console.WriteLine();
            }
        }

        public void ShowPerformanceMetrics(PerformanceMetrics metrics)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(metrics.ToString());
            Console.ResetColor();
        }

        public void ShowMessage(string message, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"\n{message}");
            Console.ResetColor();
            Console.WriteLine("\nPresiona cualquier tecla para continuar...");
            Console.ReadKey();
        }

        public bool AskForRestart()
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════");
            Console.WriteLine("¿Deseas realizar otra búsqueda de recomendaciones?");
            Console.WriteLine("[S] - Sí, nueva búsqueda");
            Console.WriteLine("[N] - No, salir");
            Console.Write("\n➤ Opción: ");
            
            var input = Console.ReadLine()?.Trim().ToUpper();
            return input == "S" || input == "SI" || input == "Y" || input == "YES";
        }
    }
}
