
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetflixRecommendationSystem.Database;
using NetflixRecommendationSystem.Models;
using NetflixRecommendationSystem.Recommender;
using NetflixRecommendationSystem.UI;

namespace NetflixRecommendationSystem
{
    class Program
    {
        private static DatabaseManager _dbManager = null!;
        private static ConsoleInterface _ui = null!;
        private static List<Movie> _allMovies = null!;
        private static User _currentUser = null!;

        static async Task Main(string[] args)
        {
            try
            {
                await InitializeSystemAsync();
                await RunApplicationAsync();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Error crítico: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine("\nPresiona cualquier tecla para salir...");
                Console.ReadKey();
            }
        }

        private static async Task InitializeSystemAsync()
        {
            Console.WriteLine("🚀 Inicializando Sistema de Recomendaciones Netflix...");
            
            // Inicializar componentes
            _dbManager = new DatabaseManager();
            _ui = new ConsoleInterface();
            
            Console.WriteLine("📚 Cargando catálogo de películas...");
            _allMovies = await _dbManager.GetAllMoviesAsync();
            
            Console.WriteLine("👤 Cargando perfil de usuario...");
            _currentUser = await _dbManager.GetUserAsync(1) ?? new User { Id = 1, Name = "Usuario Demo" };
            
            Console.WriteLine($"✅ Sistema inicializado - {_allMovies.Count} películas disponibles");
            await Task.Delay(1000); // Breve pausa para mostrar el progreso
        }

        private static async Task RunApplicationAsync()
        {
            bool continueRunning = true;
            
            while (continueRunning)
            {
                try
                {
                    // Mostrar bienvenida y selección de películas
                    _ui.ShowWelcome();
                    var selectedMovies = _ui.SelectMovies(_allMovies);
                    
                    // Actualizar preferencias del usuario
                    _currentUser.SelectedMovies = selectedMovies;
                    _currentUser.SelectedMovieIds = selectedMovies.Select(m => m.Id).ToList();
                    await _dbManager.SaveUserPreferencesAsync(_currentUser);
                    
                    // Generar recomendaciones
                    await GenerateRecommendationsAsync(selectedMovies);
                    
                    // Preguntar si continuar
                    continueRunning = _ui.AskForRestart();
                }
                catch (Exception ex)
                {
                    _ui.ShowMessage($"❌ Error durante la ejecución: {ex.Message}", ConsoleColor.Red);
                    continueRunning = _ui.AskForRestart();
                }
            }
            
            _ui.ShowMessage("👋 ¡Gracias por usar el Sistema de Recomendaciones Netflix!", ConsoleColor.Green);
        }

        private static async Task GenerateRecommendationsAsync(List<Movie> selectedMovies)
        {
            _ui.ShowRecommendationsRealTime();
            
            // Crear motor de recomendaciones
            var engine = new RecommendationEngine(_allMovies);
            
            // Configurar eventos para mostrar resultados en tiempo real
            var recommendationCount = 0;
            var displayedRecommendations = new List<Recommendation>();
            
            engine.OnRecommendationReady += (recommendation) =>
            {
                recommendationCount++;
                displayedRecommendations.Add(recommendation);
                
                // Mostrar solo las primeras 15 recomendaciones en tiempo real
                if (recommendationCount <= 15)
                {
                    _ui.ShowRecommendation(recommendation, recommendationCount);
                }
            };
            
            engine.OnStatusUpdate += (status) =>
            {
                _ui.ShowStatusUpdate(status);
            };
            
            // Obtener recomendaciones con métricas de rendimiento
            var (recommendations, metrics) = await engine.GetRecommendationsAsync(selectedMovies, 10);
            
            // Mostrar resultados finales
            _ui.ShowFinalRecommendations(recommendations);
            
            // Mostrar métricas de rendimiento
            _ui.ShowPerformanceMetrics(metrics);
            
            _ui.ShowMessage("✅ Análisis de recomendaciones completado exitosamente.", ConsoleColor.Green);
        }
    }
}

