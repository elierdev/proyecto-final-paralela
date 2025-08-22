using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using NetflixRecommendationSystem.Models;

namespace NetflixRecommendationSystem.Database
{
    public class DatabaseManager
    {
        private readonly string _connectionString;
        private const string DB_FILE = "netflixdb.sqlite";

        public DatabaseManager()
        {
            _connectionString = $"Data Source={DB_FILE};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            if (!File.Exists(DB_FILE))
            {
                SQLiteConnection.CreateFile(DB_FILE);
                Console.WriteLine("📁 Base de datos SQLite creada: netflixdb.sqlite");
            }

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // Crear tabla de películas
                var createMoviesTable = @"
                    CREATE TABLE IF NOT EXISTS movies (
                        id INTEGER PRIMARY KEY,
                        title VARCHAR(255) NOT NULL,
                        original_title VARCHAR(255),
                        locale VARCHAR(10),
                        available_globally BOOLEAN DEFAULT 0,
                        release_date DATE,
                        created_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        modified_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        runtime BIGINT DEFAULT 0,
                        genre VARCHAR(255),
                        tags TEXT,
                        year INTEGER,
                        rating REAL DEFAULT 0.0,
                        cover_path VARCHAR(500)
                    );";

                // Crear tabla de usuarios
                var createUsersTable = @"
                    CREATE TABLE IF NOT EXISTS users (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name VARCHAR(100) NOT NULL,
                        selected_movies TEXT
                    );";

                using (var command = new SQLiteCommand(createMoviesTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SQLiteCommand(createUsersTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Insertar datos de prueba si no existen
                InsertSampleData(connection);
                Console.WriteLine("✅ Base de datos inicializada correctamente");
            }
        }

        private void InsertSampleData(SQLiteConnection connection)
        {
            var checkMovies = "SELECT COUNT(*) FROM movies";
            using (var command = new SQLiteCommand(checkMovies, connection))
            {
                var count = Convert.ToInt32(command.ExecuteScalar());
                if (count == 0)
                {
                    InsertSampleMovies(connection);
                    Console.WriteLine("🎬 Datos de películas de prueba insertados");
                }
            }

            var checkUsers = "SELECT COUNT(*) FROM users";
            using (var command = new SQLiteCommand(checkUsers, connection))
            {
                var count = Convert.ToInt32(command.ExecuteScalar());
                if (count == 0)
                {
                    InsertSampleUsers(connection);
                    Console.WriteLine("👤 Usuario de prueba creado");
                }
            }
        }

        private void InsertSampleMovies(SQLiteConnection connection)
        {
            var movies = new[]
            {
                new { title = "The Shawshank Redemption", genre = "Drama", year = 1994, rating = 9.3, tags = "prison,friendship,hope", runtime = 142 },
                new { title = "The Godfather", genre = "Crime,Drama", year = 1972, rating = 9.2, tags = "mafia,family,power", runtime = 175 },
                new { title = "The Dark Knight", genre = "Action,Crime,Drama", year = 2008, rating = 9.0, tags = "superhero,batman,joker", runtime = 152 },
                new { title = "Pulp Fiction", genre = "Crime,Drama", year = 1994, rating = 8.9, tags = "nonlinear,violence,dialogue", runtime = 154 },
                new { title = "Forrest Gump", genre = "Drama,Romance", year = 1994, rating = 8.8, tags = "life,destiny,love", runtime = 142 },
                new { title = "Inception", genre = "Action,Sci-Fi,Thriller", year = 2010, rating = 8.7, tags = "dreams,reality,heist", runtime = 148 },
                new { title = "The Matrix", genre = "Action,Sci-Fi", year = 1999, rating = 8.7, tags = "reality,simulation,philosophy", runtime = 136 },
                new { title = "Goodfellas", genre = "Biography,Crime,Drama", year = 1990, rating = 8.7, tags = "mafia,violence,loyalty", runtime = 146 },
                new { title = "The Lord of the Rings: The Return of the King", genre = "Action,Adventure,Drama", year = 2003, rating = 8.9, tags = "fantasy,epic,friendship", runtime = 201 },
                new { title = "Fight Club", genre = "Drama", year = 1999, rating = 8.8, tags = "philosophy,society,identity", runtime = 139 },
                new { title = "Star Wars: Episode V", genre = "Action,Adventure,Fantasy", year = 1980, rating = 8.7, tags = "space,force,empire", runtime = 124 },
                new { title = "One Flew Over the Cuckoo's Nest", genre = "Drama", year = 1975, rating = 8.7, tags = "mental,hospital,freedom", runtime = 133 },
                new { title = "Interstellar", genre = "Adventure,Drama,Sci-Fi", year = 2014, rating = 8.6, tags = "space,time,family", runtime = 169 },
                new { title = "Parasite", genre = "Comedy,Drama,Thriller", year = 2019, rating = 8.6, tags = "class,society,korean", runtime = 132 },
                new { title = "The Green Mile", genre = "Crime,Drama,Fantasy", year = 1999, rating = 8.6, tags = "prison,supernatural,justice", runtime = 189 },
                new { title = "Avengers: Endgame", genre = "Action,Adventure,Drama", year = 2019, rating = 8.4, tags = "superhero,marvel,time", runtime = 181 },
                new { title = "Spider-Man: Into the Spider-Verse", genre = "Animation,Action,Adventure", year = 2018, rating = 8.4, tags = "animated,superhero,multiverse", runtime = 117 },
                new { title = "Coco", genre = "Animation,Adventure,Comedy", year = 2017, rating = 8.4, tags = "family,music,mexican", runtime = 105 },
                new { title = "Toy Story", genre = "Animation,Adventure,Comedy", year = 1995, rating = 8.3, tags = "toys,friendship,childhood", runtime = 81 },
                new { title = "Joker", genre = "Crime,Drama,Thriller", year = 2019, rating = 8.4, tags = "villain,psychology,society", runtime = 122 }
            };

            foreach (var movie in movies)
            {
                var insertQuery = @"
                    INSERT INTO movies (title, original_title, genre, year, rating, tags, runtime, 
                                      locale, available_globally, release_date, cover_path) 
                    VALUES (@title, @title, @genre, @year, @rating, @tags, @runtime, 
                            'en', 1, @releaseDate, '')";

                using (var command = new SQLiteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@title", movie.title);
                    command.Parameters.AddWithValue("@genre", movie.genre);
                    command.Parameters.AddWithValue("@year", movie.year);
                    command.Parameters.AddWithValue("@rating", movie.rating);
                    command.Parameters.AddWithValue("@tags", movie.tags);
                    command.Parameters.AddWithValue("@runtime", movie.runtime);
                    command.Parameters.AddWithValue("@releaseDate", new DateTime(movie.year, 1, 1));
                    command.ExecuteNonQuery();
                }
            }
        }

        private void InsertSampleUsers(SQLiteConnection connection)
        {
            var insertUser = "INSERT INTO users (name, selected_movies) VALUES (@name, @movies)";
            using (var command = new SQLiteCommand(insertUser, connection))
            {
                command.Parameters.AddWithValue("@name", "Usuario Demo");
                command.Parameters.AddWithValue("@movies", "");
                command.ExecuteNonQuery();
            }
        }

        public async Task<List<Movie>> GetAllMoviesAsync()
        {
            var movies = new List<Movie>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT * FROM movies ORDER BY rating DESC";

                using (var command = new SQLiteCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        movies.Add(new Movie
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("id")),
                            Title = reader.GetString(reader.GetOrdinal("title")),
                            OriginalTitle = reader.IsDBNull(reader.GetOrdinal("original_title")) ? "" : reader.GetString(reader.GetOrdinal("original_title")),
                            Genre = reader.IsDBNull(reader.GetOrdinal("genre")) ? "" : reader.GetString(reader.GetOrdinal("genre")),
                            Tags = reader.IsDBNull(reader.GetOrdinal("tags")) ? "" : reader.GetString(reader.GetOrdinal("tags")),
                            Year = reader.GetInt32(reader.GetOrdinal("year")),
                            Rating = reader.GetDouble(reader.GetOrdinal("rating")),
                            CoverPath = reader.IsDBNull(reader.GetOrdinal("cover_path")) ? "" : reader.GetString(reader.GetOrdinal("cover_path")),
                            Locale = reader.IsDBNull(reader.GetOrdinal("locale")) ? "en" : reader.GetString(reader.GetOrdinal("locale")),
                            AvailableGlobally = reader.GetBoolean(reader.GetOrdinal("available_globally")),
                            Runtime = reader.GetInt64(reader.GetOrdinal("runtime")),
                            ReleaseDate = reader.IsDBNull(reader.GetOrdinal("release_date")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("release_date")),
                            CreatedDate = reader.IsDBNull(reader.GetOrdinal("created_date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("created_date")),
                            ModifiedDate = reader.IsDBNull(reader.GetOrdinal("modified_date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("modified_date"))
                        });
                    }
                }
            }
            return movies;
        }

        public async Task<User> GetUserAsync(int userId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT * FROM users WHERE id = @id";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", userId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var selectedMoviesStr = reader.IsDBNull(reader.GetOrdinal("selected_movies")) ? "" : reader.GetString(reader.GetOrdinal("selected_movies"));
                            var selectedIds = new List<int>();

                            if (!string.IsNullOrEmpty(selectedMoviesStr))
                            {
                                var idStrings = selectedMoviesStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
                                foreach (var idStr in idStrings)
                                {
                                    if (int.TryParse(idStr, out int id))
                                        selectedIds.Add(id);
                                }
                            }

                            return new User
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                Name = reader.GetString(reader.GetOrdinal("name")),
                                SelectedMovieIds = selectedIds
                            };
                        }
                    }
                }
            }
            return null;
        }

        public async Task SaveUserPreferencesAsync(User user)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var selectedMoviesStr = string.Join(",", user.SelectedMovieIds);
                var query = "UPDATE users SET selected_movies = @movies WHERE id = @id";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@movies", selectedMoviesStr);
                    command.Parameters.AddWithValue("@id", user.Id);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /* 
         * NOTA IMPORTANTE: IMPORTACIÓN DEL DATASET
         * 
         * Para importar tu dataset de películas con la estructura mostrada en la imagen:
         * 
         * 1. Guarda tu dataset como CSV con las siguientes columnas:
         *    - id, title, original_title, locale, available_globally, 
         *    - release_date, created_date, modified_date, runtime, genre, tags, year, rating, cover_path
         * 
         * 2. Usa el siguiente método para importar masivamente:
         */
        
        public async Task ImportMoviesFromCsvAsync(string csvFilePath)
        {
            if (!File.Exists(csvFilePath))
            {
                throw new FileNotFoundException($"Archivo CSV no encontrado: {csvFilePath}");
            }

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // Limpiar tabla existente si es necesario
                // await new SQLiteCommand("DELETE FROM movies", connection).ExecuteNonQueryAsync();

                var lines = await File.ReadAllLinesAsync(csvFilePath);
                var insertQuery = @"
                    INSERT OR REPLACE INTO movies 
                    (id, title, original_title, locale, available_globally, release_date, 
                     created_date, modified_date, runtime, genre, tags, year, rating, cover_path) 
                    VALUES (@id, @title, @original_title, @locale, @available_globally, @release_date,
                            @created_date, @modified_date, @runtime, @genre, @tags, @year, @rating, @cover_path)";

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        for (int i = 1; i < lines.Length; i++) // Saltar header
                        {
                            var columns = lines[i].Split(',');
                            if (columns.Length >= 13) // Validar columnas mínimas
                            {
                                using (var command = new SQLiteCommand(insertQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@id", int.Parse(columns[0]));
                                    command.Parameters.AddWithValue("@title", columns[1]);
                                    command.Parameters.AddWithValue("@original_title", columns[2]);
                                    command.Parameters.AddWithValue("@locale", columns[3]);
                                    command.Parameters.AddWithValue("@available_globally", bool.Parse(columns[4]));
                                    command.Parameters.AddWithValue("@release_date", DateTime.Parse(columns[5]));
                                    command.Parameters.AddWithValue("@created_date", DateTime.Parse(columns[6]));
                                    command.Parameters.AddWithValue("@modified_date", DateTime.Parse(columns[7]));
                                    command.Parameters.AddWithValue("@runtime", long.Parse(columns[8]));
                                    command.Parameters.AddWithValue("@genre", columns[9]);
                                    command.Parameters.AddWithValue("@tags", columns[10]);
                                    command.Parameters.AddWithValue("@year", int.Parse(columns[11]));
                                    command.Parameters.AddWithValue("@rating", double.Parse(columns[12]));
                                    command.Parameters.AddWithValue("@cover_path", columns.Length > 13 ? columns[13] : "");
                                    
                                    await command.ExecuteNonQueryAsync();
                                }
                            }
                        }
                        transaction.Commit();
                        Console.WriteLine($"✅ {lines.Length - 1} películas importadas exitosamente");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"Error importando CSV: {ex.Message}");
                    }
                }
            }
        }
    }
}
