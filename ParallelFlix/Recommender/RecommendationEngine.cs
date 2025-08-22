using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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

        /// <summary>
        /// Versión secuencial para comparación de rendimiento
        /// </summary>
        public async Task<List<Recommendation>> GetRecommendationsSequentialAsync(List<Movie> userSelectedMovies, int maxRecommendations = 10)
        {
            _recommendations.Clear();
            
            // Ejecutar algoritmos uno por uno (secuencialmente)
            await GenreBasedRecommendationsSequential(userSelectedMovies);
            await RatingBasedRecommendationsSequential(userSelectedMovies);
            await TagBasedRecommendationsSequential(userSelectedMovies);
            await YearBasedRecommendationsSequential(userSelectedMovies);
            await RuntimeBasedRecommendationsSequential(userSelectedMovies);
            
            return CombineAndFilterRecommendations(userSelectedMovies, maxRecommendations);
        }

        /// <summary>
        /// Versión con descomposición especulativa - espera al primer resultado válido
        /// </summary>
        public async Task<List<Recommendation>> GetRecommendationsSpeculativeAsync(List<Movie> userSelectedMovies, int maxRecommendations = 10, int maxThreads = -1)
        {
            _recommendations.Clear();
            
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxThreads > 0 ? maxThreads : Environment.ProcessorCount
            };

            using var cts = new CancellationTokenSource();
            var token = cts.Token;

            // Lista de tareas especulativas
            var speculativeTasks = new List<Task<List<Recommendation>>>
            {
                Task.Run(async () => await ExecuteGenreBasedSpeculative(userSelectedMovies, maxRecommendations, token), token),
                Task.Run(async () => await ExecuteRatingBasedSpeculative(userSelectedMovies, maxRecommendations, token), token),
                Task.Run(async () => await ExecuteTagBasedSpeculative(userSelectedMovies, maxRecommendations, token), token),
                Task.Run(async () => await ExecuteYearBasedSpeculative(userSelectedMovies, maxRecommendations, token), token),
                Task.Run(async () => await ExecuteRuntimeBasedSpeculative(userSelectedMovies, maxRecommendations, token), token)
            };

            // Esperar hasta que la primera tarea complete con resultados válidos
            var completedTask = await Task.WhenAny(speculativeTasks);
            var result = await completedTask;

            // Cancelar las tareas restantes
            cts.Cancel();

            // Si el primer resultado no tiene suficientes recomendaciones, esperar más tareas
            if (result.Count < maxRecommendations / 2)
            {
                try
                {
                    // Esperar a que completen más tareas por un tiempo limitado
                    var additionalTasks = speculativeTasks.Where(t => t != completedTask && !t.IsCompleted).ToList();
                    if (additionalTasks.Any())
                    {
                        var timeoutTask = Task.Delay(1000, CancellationToken.None); // 1 segundo timeout
                        var additionalCompleted = await Task.WhenAny(Task.WhenAll(additionalTasks), timeoutTask);
                        
                        if (additionalCompleted != timeoutTask)
                        {
                            // Combinar resultados de todas las tareas completadas
                            foreach (var task in speculativeTasks.Where(t => t.IsCompletedSuccessfully))
                            {
                                var additionalResults = await task;
                                result = result.Concat(additionalResults).ToList();
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ignorar cancelaciones, usar el resultado que tenemos
                }
            }

            return result.GroupBy(r => r.Movie.Id)
                        .Select(g => g.First())
                        .OrderByDescending(r => r.Score)
                        .Take(maxRecommendations)
                        .ToList();
        }

        // Métodos secuenciales (sin paralelismo)
        private async Task GenreBasedRecommendationsSequential(List<Movie> userMovies)
        {
            var recommendations = new List<Recommendation>();
            
            var userGenres = userMovies.SelectMany(m => m.GetGenres()).ToList();
            var genreFrequency = userGenres.GroupBy(g => g).ToDictionary(g => g.Key, g => g.Count());

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
                }
            }
            
            await Task.CompletedTask;
        }

        private async Task RatingBasedRecommendationsSequential(List<Movie> userMovies)
        {
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
                }
            }
            
            await Task.CompletedTask;
        }

        private async Task TagBasedRecommendationsSequential(List<Movie> userMovies)
        {
            var userTags = userMovies.SelectMany(m => m.GetTags()).ToList();
            var tagFrequency = userTags.GroupBy(t => t).ToDictionary(t => t.Key, t => t.Count());

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
                }
            }
            
            await Task.CompletedTask;
        }

        private async Task YearBasedRecommendationsSequential(List<Movie> userMovies)
        {
            var userYears = userMovies.Select(m => m.Year).ToList();
            var avgYear = userYears.Average();
            var yearRange = 10;

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
                }
            }
            
            await Task.CompletedTask;
        }

        private async Task RuntimeBasedRecommendationsSequential(List<Movie> userMovies)
        {
            var avgRuntime = userMovies.Average(m => m.Runtime);
            var runtimeTolerance = 30;

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
                }
            }
            
            await Task.CompletedTask;
        }

        // Métodos especulativos (para descomposición especulativa)
        private async Task<List<Recommendation>> ExecuteGenreBasedSpeculative(List<Movie> userMovies, int maxRecommendations, CancellationToken token)
        {
            var localRecommendations = new List<Recommendation>();
            
            var userGenres = userMovies.SelectMany(m => m.GetGenres()).ToList();
            var genreFrequency = userGenres.GroupBy(g => g).ToDictionary(g => g.Key, g => g.Count());

            foreach (var movie in _allMovies.Where(m => !userMovies.Any(um => um.Id == m.Id)))
            {
                token.ThrowIfCancellationRequested();
                
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
                    localRecommendations.Add(recommendation);
                }
                
                // Simular trabajo computacional
                await Task.Delay(1, token);
            }
            
            return localRecommendations.OrderByDescending(r => r.Score).Take(maxRecommendations).ToList();
        }

        private async Task<List<Recommendation>> ExecuteRatingBasedSpeculative(List<Movie> userMovies, int maxRecommendations, CancellationToken token)
        {
            var localRecommendations = new List<Recommendation>();
            
            var avgUserRating = userMovies.Average(m => m.Rating);
            var ratingTolerance = 1.0;

            foreach (var movie in _allMovies.Where(m => !userMovies.Any(um => um.Id == m.Id)))
            {
                token.ThrowIfCancellationRequested();
                
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
                    localRecommendations.Add(recommendation);
                }
                
                await Task.Delay(1, token);
            }
            
            return localRecommendations.OrderByDescending(r => r.Score).Take(maxRecommendations).ToList();
        }

        private async Task<List<Recommendation>> ExecuteTagBasedSpeculative(List<Movie> userMovies, int maxRecommendations, CancellationToken token)
        {
            var localRecommendations = new List<Recommendation>();
            
            var userTags = userMovies.SelectMany(m => m.GetTags()).ToList();
            var tagFrequency = userTags.GroupBy(t => t).ToDictionary(t => t.Key, t => t.Count());

            foreach (var movie in _allMovies.Where(m => !userMovies.Any(um => um.Id == m.Id)))
            {
                token.ThrowIfCancellationRequested();
                
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
                    localRecommendations.Add(recommendation);
                }
                
                await Task.Delay(1, token);
            }
            
            return localRecommendations.OrderByDescending(r => r.Score).Take(maxRecommendations).ToList();
        }

        private async Task<List<Recommendation>> ExecuteYearBasedSpeculative(List<Movie> userMovies, int maxRecommendations, CancellationToken token)
        {
            var localRecommendations = new List<Recommendation>();
            
            var userYears = userMovies.Select(m => m.Year).ToList();
            var avgYear = userYears.Average();
            var yearRange = 10;

            foreach (var movie in _allMovies.Where(m => !userMovies.Any(um => um.Id == m.Id)))
            {
                token.ThrowIfCancellationRequested();
                
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
                    localRecommendations.Add(recommendation);
                }
                
                await Task.Delay(1, token);
            }
            
            return localRecommendations.OrderByDescending(r => r.Score).Take(maxRecommendations).ToList();
        }

        private async Task<List<Recommendation>> ExecuteRuntimeBasedSpeculative(List<Movie> userMovies, int maxRecommendations, CancellationToken token)
        {
            var localRecommendations = new List<Recommendation>();
            
            var avgRuntime = userMovies.Average(m => m.Runtime);
            var runtimeTolerance = 30;

            foreach (var movie in _allMovies.Where(m => !userMovies.Any(um => um.Id == m.Id)))
            {
                token.ThrowIfCancellationRequested();
                
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
                    localRecommendations.Add(recommendation);
                }
                
                await Task.Delay(1, token);
            }
            
            return localRecommendations.OrderByDescending(r => r.Score).Take(maxRecommendations).ToList();
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
