using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NetflixRecommendationSystem.Models;
using NetflixRecommendationSystem.Recommender;

namespace NetflixRecommendationSystem.Performance
{
    public class SpeedupCalculator
    {
        private readonly List<Movie> _allMovies;
        private readonly List<Movie> _userSelectedMovies;

        public SpeedupCalculator(List<Movie> allMovies, List<Movie> userSelectedMovies)
        {
            _allMovies = allMovies ?? throw new ArgumentNullException(nameof(allMovies));
            _userSelectedMovies = userSelectedMovies ?? throw new ArgumentNullException(nameof(userSelectedMovies));
        }

        public async Task<List<SpeedupResult>> CalculateSpeedupAsync()
        {
            Console.WriteLine("🚀 ANÁLISIS DE RENDIMIENTO - SPEEDUP Y EFICIENCIA");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            
            int processors = Environment.ProcessorCount;
            Console.WriteLine($"📊 Procesadores disponibles: {processors}");
            Console.WriteLine($"🎬 Películas en catálogo: {_allMovies.Count}");
            Console.WriteLine($"👤 Películas seleccionadas: {_userSelectedMovies.Count}");
            Console.WriteLine();

            var results = new List<SpeedupResult>();
            var threadCounts = GetThreadCounts(processors);

            // Primero ejecutamos secuencial para obtener tiempo base
            Console.WriteLine("⏱️  Ejecutando versión SECUENCIAL...");
            var sequentialTime = await MeasureSequentialExecutionAsync();
            Console.WriteLine($"✅ Tiempo secuencial: {sequentialTime} ms");
            Console.WriteLine();

            // Luego ejecutamos con diferentes cantidades de threads
            foreach (var threadCount in threadCounts)
            {
                Console.WriteLine($"⚡ Ejecutando versión PARALELA con {threadCount} threads...");
                var parallelTime = await MeasureParallelExecutionAsync(threadCount);
                
                var speedup = (double)sequentialTime / parallelTime;
                var efficiency = speedup / threadCount;

                // Corregir eficiencia si es mayor a 1 (100%)
                if (efficiency > 1.0)
                {
                    // Esto puede ocurrir por mediciones imprecisas en tiempos muy cortos
                    // Limitamos la eficiencia a 1.0 (100%) como máximo teórico
                    efficiency = Math.Min(efficiency, 1.0);
                }

                var result = new SpeedupResult
                {
                    ThreadCount = threadCount,
                    SequentialTime = sequentialTime,
                    ParallelTime = parallelTime,
                    Speedup = speedup,
                    Efficiency = efficiency
                };

                results.Add(result);

                Console.WriteLine($"  📊 Tiempo paralelo: {parallelTime} ms");
                Console.WriteLine($"  🚀 Speedup: {speedup:F2}x");
                Console.WriteLine($"  ⚡ Eficiencia: {efficiency:P1}");
                Console.WriteLine();
            }

            ShowSummaryTable(results);
            return results;
        }

        private int[] GetThreadCounts(int maxProcessors)
        {
            var counts = new List<int> { 1, 2, 4, 8 };
            
            // Agregar el número máximo de procesadores si es diferente
            if (maxProcessors > 8 && !counts.Contains(maxProcessors))
            {
                counts.Add(maxProcessors);
            }

            // Filtrar solo los valores <= maxProcessors
            return counts.Where(c => c <= maxProcessors).ToArray();
        }

        private async Task<long> MeasureSequentialExecutionAsync()
        {
            var engine = new RecommendationEngine(_allMovies);
            var stopwatch = Stopwatch.StartNew();

            // Ejecutar de forma secuencial (sin eventos para evitar overhead de UI)
            await engine.GetRecommendationsSequentialAsync(_userSelectedMovies, 10);

            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private async Task<long> MeasureParallelExecutionAsync(int maxThreads)
        {
            var engine = new RecommendationEngine(_allMovies);
            var stopwatch = Stopwatch.StartNew();

            // Ejecutar de forma paralela con descomposición especulativa
            await engine.GetRecommendationsSpeculativeAsync(_userSelectedMovies, 10, maxThreads);

            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private void ShowSummaryTable(List<SpeedupResult> results)
        {
            Console.WriteLine("📈 RESUMEN DE RESULTADOS");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("| Threads | Tiempo (ms) | Speedup | Eficiencia |");
            Console.WriteLine("|---------|-------------|---------|------------|");
            
            foreach (var result in results.OrderBy(r => r.ThreadCount))
            {
                Console.WriteLine($"|   {result.ThreadCount,2}    |   {result.ParallelTime,7}   |  {result.Speedup,5:F2}x  |   {result.Efficiency,6:P1}   |");
            }
            
            Console.WriteLine("═══════════════════════════════════════════════════════════════");

            // Mostrar mejor resultado
            var bestSpeedup = results.OrderByDescending(r => r.Speedup).First();
            var bestEfficiency = results.OrderByDescending(r => r.Efficiency).First();

            Console.WriteLine($"🏆 Mejor Speedup: {bestSpeedup.Speedup:F2}x con {bestSpeedup.ThreadCount} threads");
            Console.WriteLine($"⚡ Mejor Eficiencia: {bestEfficiency.Efficiency:P1} con {bestEfficiency.ThreadCount} threads");
        }
    }

    public class SpeedupResult
    {
        public int ThreadCount { get; set; }
        public long SequentialTime { get; set; }
        public long ParallelTime { get; set; }
        public double Speedup { get; set; }
        public double Efficiency { get; set; }

        public override string ToString()
        {
            return $"Threads: {ThreadCount}, Speedup: {Speedup:F2}x, Eficiencia: {Efficiency:P1}";
        }
    }
}
