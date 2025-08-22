
==================== Instrucciones de Instalación ====================

1. CREAR EL PROYECTO:
   - Abre Visual Studio 2022
   - Crear nuevo proyecto: Console App (.NET 6.0)
   - Nombre: NetflixRecommendationSystem

2. INSTALAR DEPENDENCIAS:
   Ejecutar en Package Manager Console:
   Install-Package System.Data.SQLite.Core -Version 1.0.118

3. ESTRUCTURA DE CARPETAS:
   NetflixRecommendationSystem/
   ├── Models/
   │   ├── Movie.cs
   │   ├── User.cs
   │   └── Recommendation.cs
   ├── Database/
   │   └── DatabaseManager.cs
   ├── Recommender/
   │   └── RecommendationEngine.cs
   ├── UI/
   │   └── ConsoleInterface.cs
   └── Program.cs

4. IMPORTAR DATASET DE PELÍCULAS:
   Para importar tu dataset con la estructura mostrada:
   
   a) Guarda tu dataset como CSV con headers:
      id,title,original_title,locale,available_globally,release_date,created_date,modified_date,runtime,genre,tags,year,rating,cover_path
   
   b) Agrega este código en Program.cs después de InitializeSystemAsync():
   
   ```csharp
   // Descomenta para importar tu dataset
   // await _dbManager.ImportMoviesFromCsvAsync("ruta_a_tu_dataset.csv");
   // _allMovies = await _dbManager.GetAllMoviesAsync();
   ```

5. COMPILAR Y EJECUTAR:
   - Presiona F5 en Visual Studio
   - O desde terminal: dotnet run

==================== CARACTERÍSTICAS IMPLEMENTADAS ====================

✅ Base de datos SQLite con tablas de películas y usuarios
✅ Interfaz CLI interactiva con paginación
✅ Descomposición especulativa con 5 algoritmos paralelos:
   • Análisis de género
   • Análisis de calificación  
   • Análisis de etiquetas/temas
   • Análisis temporal (año)
   • Análisis de duración
✅ Resultados en tiempo real conforme se calculan
✅ Combinación inteligente de resultados
✅ Métricas de rendimiento (speedup y eficiencia)
✅ Persistencia de preferencias de usuario
✅ Manejo de errores y validación
✅ Código modularizado y comentado
✅ Interfaz visual atractiva con colores y emojis

==================== CARACTERÍSTICAS ADICIONALES ====================

🎯 Interfaz CLI moderna con colores y símbolos
📊 Análisis de rendimiento detallado
🔄 Selección múltiple de películas con preview
⚡ Ejecución paralela real con Task y async/await
🎬 Base de datos preestablecida con 20 películas populares
💾 Sistema de persistencia automática
🏆 Rankings con medallas para mejores recomendaciones
📈 Métricas detalladas de CPU y eficiencia
-->

/* 
INSTRUCCIONES DE USO:

1. El sistema iniciará con una base de datos precargada de películas populares
2. Usa números (ej: 1,3,5) para seleccionar múltiples películas
3. Navega con [N] y [P] entre páginas
4. Presiona [R] para generar recomendaciones
5. Observa las recomendaciones generándose en tiempo real
6. Revisa las métricas de rendimiento al final

PARA IMPORTAR TU DATASET:
- Guárdalo como CSV con la estructura mostrada
- Descomenta la línea de importación en Program.cs
- Especifica la ruta de tu archivo CSV
- El sistema importará automáticamente todas las películas

MÉTRICAS DE RENDIMIENTO:
- Speedup: Cuánto más rápido es el procesamiento paralelo
- Eficiencia: Qué tan bien se utilizan los núcleos del CPU
- Tiempo paralelo vs secuencial en milisegundos
*/