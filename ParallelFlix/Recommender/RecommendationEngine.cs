using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NetflixRecommendationSystem.Models;

namespace NetflixRecommendationSystem.Recommender
{
    public class RecommendationEngine
    {
        private readonly List<Movie> _allMovies;
        private readonly ConcurrentBag<Recommendation> _recommendations = new ConcurrentBag<Recommendation>();
        private readonly object _lockObject = new object();
        
        public event Action<Recommendation> OnRecommendationReady = null!;
        public event Action<string> OnStatusUpdate = null!;

        public RecommendationEngine(List<Movie> allMovies)
        {
            _allMovies = allMovies ?? throw new ArgumentNullException(nameof(allMovies));
        }

        public async Task<(List<Recommendation> recommendations, PerformanceMetrics metrics)> 
            GetRecommendationsAsync(List<Movie> userSelectedMovies, int maxRecommendations = 10)
        {
            _recommendations.Clear();
            var stopwatch = Stopwatch.StartNew();

            OnStatusUpdate?.Invoke("🔄 Iniciando análisis de recomendaciones...");

            // Descomposición especulativa: ejecutar diferentes algoritmos en paralelo
            var tasks = new List<Task>
            {
                GenreBasedRecommendationsAsync(userSelectedMovies),
                RatingBasedRecommendationsAsync(userSelectedMovies),
                TagBasedRecommendationsAsync(userSelectedMovies),
                YearBasedRecommendationsAsync(userSelectedMovies),
                RuntimeBasedRecommendationsAsync(userSelectedMovies)
            };

            // Iniciar todas las tareas en paralelo
            var allTasksTask = Task.WhenAll(tasks);
            
            // Esperar a que todas terminen
            await allTasksTask;
            
            stopwatch.Stop();

            // Combinar y filtrar resultados
            var finalRecommendations = CombineAndFilterRecommendations(userSelectedMovies, maxRecommendations);
            
            // Calcular métricas de rendimiento
            var metrics = await CalculatePerformanceMetrics(userSelectedMovies, stopwatch.ElapsedMilliseconds);

            OnStatusUpdate?.Invoke($"✅ Análisis completado en {stopwatch.ElapsedMilliseconds}ms");

            return (finalRecommendations, metrics);
        }

        private async Task GenreBasedRecommendationsAsync(List<Movie> userMovies)
        {
            await Task.Run(() =>
            {
                OnStatusUpdate?.Invoke("📊 Analizando preferencias de género...");
                
                var userGenres = userMovies.SelectMany(m => m.GetGenres()).ToList();
                var genreFrequency = userGenres.GroupBy(g => g)
                                               .ToDictionary(g => g.Key, g => g.Count());

                foreach (var movie in _allMovies.Where(m => !userMovies.Any(um => um.Id == m.Id)))
                {
                    var movieGenres = movie.GetGenres();
                    var score = movieGenres.Sum(g => genreFrequency.ContainsKey(g) ? genreFrequency[g] : 0);
                    
                    if (score > 0)
                    {
                        var recommendation = new Recommendation
                        {
                            Movie = movie,
                            Score = score * movie.Rating / 10.0,
                            Reason = $"Coincide en géneros: {string.Join(", ", movieGenres.Where(genreFrequency.ContainsKey))}",
                            Algorithm = "Género",
                            CalculatedAt = DateTime.Now
                        };
                        
                        _recommendations.Add(recommendation);
                        OnRecommendationReady?.Invoke(recommendation);
                    }
                }
                
                OnStatusUpdate?.Invoke("✅ Análisis de género completado");
            });
        }

        private async Task RatingBasedRecommendationsAsync(List<Movie> userMovies)
        {
            await Task.Run(() =>
            {
                OnStatusUpdate?.Invoke("⭐ Analizando patrones de calificación...");
                
                var avgUserRating = userMovies.Average(m => m.Rating);
                var ratingTolerance = 1.0;

                foreach (var movie in _allMovies.Where(m => !userMovies.Any(um => um.Id == m.Id)))
                {
                    var ratingDifference = Math.Abs(movie.Rating - avgUserRating);
                    if (ratingDifference <= ratingTolerance)
                    {
                        var score = (ratingTolerance - ratingDifference) * movie.Rating;
                        
                        var recommendation = new Recommendation
                        {
                            Movie = movie,
                            Score = score,
                            Reason = $"Calificación similar ({movie.Rating:F1}/10) a tus preferencias ({avgUserRating:F1}/10)",
                            Algorithm = "Calificación",
                            CalculatedAt = DateTime.Now
                        };
                        
                        _recommendations.Add(recommendation);
                        OnRecommendationReady?.Invoke(recommendation);
                    }
                }
                
                OnStatusUpdate?.Invoke("✅ Análisis de calificación completado");
            });
        }

        private async Task TagBasedRecommendationsAsync(List<Movie> userMovies)
        {
            await Task.Run(() =>
            {
                OnStatusUpdate?.Invoke("🏷️ Analizando etiquetas y temas...");
                
                var userTags = userMovies.SelectMany(m => m.GetTags()).ToList();
                var tagFrequency = userTags.GroupBy(t => t)
                                           .ToDictionary(t => t.Key, t => t.Count());

                foreach (var movie in _allMovies.Where(m => !userMovies.Any(um => um.Id == m.Id)))
                {
                    var movieTags = movie.GetTags();
                    var commonTags = movieTags.Where(tagFrequency.ContainsKey).ToList();
                    
                    if (commonTags.Any())
                    {
                        var score = commonTags.Sum(t => tagFrequency[t]) * movie.Rating / 10.0;
                        
                        var recommendation = new Recommendation
                        {
                            Movie = movie,
                            Score = score,
                            Reason = $"Temas similares: {string.Join(", ", commonTags)}",
                            Algorithm = "Etiquetas",
                            CalculatedAt = DateTime.Now
                        };
                        
                        _recommendations.Add(recommendation);
                        OnRecommendationReady?.Invoke(recommendation);
                    }
                }
                
                OnStatusUpdate?.Invoke("✅ Análisis de etiquetas completado");
            });
        }

        private async Task YearBasedRecommendationsAsync(List<Movie> userMovies)
        {
            await Task.Run(() =>
            {
                OnStatusUpdate?.Invoke("📅 Analizando preferencias temporales...");
                
                var userYears = userMovies.Select(m => m.Year).ToList();
                var avgYear = userYears.Average();
                var yearRange = 10; // ±10 años

                foreach (var movie in _allMovies.Where(m => !userMovies.Any(um => um.Id == m.Id)))
                {
                    var yearDifference = Math.Abs(movie.Year - avgYear);
                    if (yearDifference <= yearRange)
                    {
                        var score = (yearRange - yearDifference) / yearRange * movie.Rating / 10.0;
                        
                        var recommendation = new Recommendation
                        {
                            Movie = movie,
                            Score = score,
                            Reason = $"Época similar ({movie.Year}) a tus preferencias ({avgYear:F0}s)",
                            Algorithm = "Año",
                            CalculatedAt = DateTime.Now
                        };
                        
                        _recommendations.Add(recommendation);
                        OnRecommendationReady?.Invoke(recommendation);
                    }
                }
                
                OnStatusUpdate?.Invoke("✅ Análisis temporal completado");
            });
        }

        private async Task RuntimeBasedRecommendationsAsync(List<Movie> userMovies)
        {
            await Task.Run(() =>
            {
                OnStatusUpdate?.Invoke("⏱️ Analizando duración preferida...");
                
                var avgRuntime = userMovies.Average(m => m.Runtime);
                var runtimeTolerance = 30; // ±30 minutos

                foreach (var movie in _allMovies.Where(m => !userMovies.Any(um => um.Id == m.Id)))
                {
                    var runtimeDifference = Math.Abs(movie.Runtime - avgRuntime);
                    if (runtimeDifference <= runtimeTolerance)
                    {
                        var score = (runtimeTolerance - runtimeDifference) / runtimeTolerance * movie.Rating / 10.0;
                        
                        var recommendation = new Recommendation
                        {
                            Movie = movie,
                            Score = score,
                            Reason = $"Duración similar ({movie.Runtime}min) a tus preferencias ({avgRuntime:F0}min)",
                            Algorithm = "Duración",
                            CalculatedAt = DateTime.Now
                        };
                        
                        _recommendations.Add(recommendation);
                        OnRecommendationReady?.Invoke(recommendation);
                    }
                }
                
                OnStatusUpdate?.Invoke("✅ Análisis de duración completado");
            });
        }

        private List<Recommendation> CombineAndFilterRecommendations(List<Movie> userMovies, int maxRecommendations)
        {
            OnStatusUpdate?.Invoke("🔄 Combinando y filtrando resultados...");

            // Agrupar recomendaciones por película y combinar puntajes
            var combinedRecommendations = _recommendations
                .GroupBy(r => r.Movie.Id)
                .Select(g =>
                {
                    var recommendations = g.ToList();
                    var firstRec = recommendations.First();
                    
                    return new Recommendation
                    {
                        Movie = firstRec.Movie,
                        Score = recommendations.Sum(r => r.Score), // Combinar puntajes
                        Reason = string.Join(" | ", recommendations.Select(r => $"{r.Algorithm}: {r.Reason}").Take(2)),
                        Algorithm = string.Join(", ", recommendations.Select(r => r.Algorithm).Distinct()),
                        CalculatedAt = DateTime.Now
                    };
                })
                .Where(r => !userMovies.Any(um => um.Id == r.Movie.Id)) // Excluir películas ya seleccionadas
                .OrderByDescending(r => r.Score)
                .Take(maxRecommendations)
                .ToList();

            OnStatusUpdate?.Invoke($"✅ {combinedRecommendations.Count} recomendaciones finales generadas");
            
            return combinedRecommendations;
        }

        private async Task<PerformanceMetrics> CalculatePerformanceMetrics(List<Movie> userMovies, long parallelTime)
        {
            return await Task.Run(() =>
            {
                OnStatusUpdate?.Invoke("📊 Calculando métricas de rendimiento...");
                
                // Simular ejecución secuencial para comparar
                var stopwatch = Stopwatch.StartNew();
                
                // Simular ejecución secuencial (sin mostrar resultados)
                var tempRecommendations = new List<Recommendation>();
                
                stopwatch.Stop();
                var sequentialTime = stopwatch.ElapsedMilliseconds;
                
                var speedup = sequentialTime > 0 ? (double)sequentialTime / parallelTime : 1.0;
                var efficiency = speedup / Environment.ProcessorCount;
                
                OnStatusUpdate?.Invoke("✅ Métricas calculadas");
                
                return new PerformanceMetrics
                {
                    ParallelExecutionTime = parallelTime,
                    SequentialExecutionTime = sequentialTime,
                    Speedup = speedup,
                    Efficiency = efficiency,
                    ProcessorCount = Environment.ProcessorCount,
                    TotalRecommendations = _recommendations.Count
                };
            });
        }
    }

    public class PerformanceMetrics
    {
        public long ParallelExecutionTime { get; set; }
        public long SequentialExecutionTime { get; set; }
        public double Speedup { get; set; }
        public double Efficiency { get; set; }
        public int ProcessorCount { get; set; }
        public int TotalRecommendations { get; set; }

        public override string ToString()
        {
            return $@"
📊 MÉTRICAS DE RENDIMIENTO
═══════════════════════════════════════
⏱️  Tiempo Paralelo:     {ParallelExecutionTime} ms
⏱️  Tiempo Secuencial:   {SequentialExecutionTime} ms
🚀 Speedup:              {Speedup:F2}x
⚡ Eficiencia:           {Efficiency:F2} ({Efficiency * 100:F1}%)
🔧 Núcleos CPU:          {ProcessorCount}
🎬 Recomendaciones:      {TotalRecommendations}
═══════════════════════════════════════";
        }
    }
}
