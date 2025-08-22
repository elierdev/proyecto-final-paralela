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
                // Top 20 películas (estas se mostrarán en la UI)
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
                new { title = "Joker", genre = "Crime,Drama,Thriller", year = 2019, rating = 8.4, tags = "villain,psychology,society", runtime = 122 },

                // Películas adicionales (180 más) - Solo para recomendaciones
                new { title = "Titanic", genre = "Drama,Romance", year = 1997, rating = 7.8, tags = "love,disaster,ship", runtime = 194 },
                new { title = "Avatar", genre = "Action,Adventure,Fantasy", year = 2009, rating = 7.8, tags = "alien,nature,war", runtime = 162 },
                new { title = "The Lion King", genre = "Animation,Adventure,Drama", year = 1994, rating = 8.5, tags = "disney,animals,family", runtime = 88 },
                new { title = "Gladiator", genre = "Action,Adventure,Drama", year = 2000, rating = 8.5, tags = "rome,revenge,warrior", runtime = 155 },
                new { title = "The Departed", genre = "Crime,Drama,Thriller", year = 2006, rating = 8.5, tags = "undercover,police,crime", runtime = 151 },
                new { title = "Whiplash", genre = "Drama,Music", year = 2014, rating = 8.5, tags = "music,obsession,drums", runtime = 106 },
                new { title = "The Prestige", genre = "Drama,Mystery,Thriller", year = 2006, rating = 8.5, tags = "magic,rivalry,twist", runtime = 130 },
                new { title = "Memento", genre = "Mystery,Thriller", year = 2000, rating = 8.4, tags = "memory,revenge,puzzle", runtime = 113 },
                new { title = "Saving Private Ryan", genre = "Drama,War", year = 1998, rating = 8.6, tags = "war,sacrifice,brotherhood", runtime = 169 },
                new { title = "Schindler's List", genre = "Biography,Drama,History", year = 1993, rating = 8.9, tags = "holocaust,hero,history", runtime = 195 },
                new { title = "The Silence of the Lambs", genre = "Crime,Drama,Thriller", year = 1991, rating = 8.6, tags = "serial,killer,psychology", runtime = 118 },
                new { title = "Se7en", genre = "Crime,Drama,Mystery", year = 1995, rating = 8.6, tags = "detective,serial,killer", runtime = 127 },
                new { title = "Spirited Away", genre = "Animation,Adventure,Family", year = 2001, rating = 9.2, tags = "anime,magic,childhood", runtime = 125 },
                new { title = "City of God", genre = "Crime,Drama", year = 2002, rating = 8.6, tags = "favela,violence,childhood", runtime = 130 },
                new { title = "Life is Beautiful", genre = "Comedy,Drama,Romance", year = 1997, rating = 8.6, tags = "holocaust,love,hope", runtime = 116 },
                new { title = "The Green Book", genre = "Biography,Comedy,Drama", year = 2018, rating = 8.2, tags = "racism,friendship,music", runtime = 130 },
                new { title = "1917", genre = "Drama,Thriller,War", year = 2019, rating = 8.3, tags = "war,mission,one-shot", runtime = 119 },
                new { title = "Once Upon a Time in Hollywood", genre = "Comedy,Drama", year = 2019, rating = 7.6, tags = "hollywood,1960s,actor", runtime = 161 },
                new { title = "Mad Max: Fury Road", genre = "Action,Adventure,Sci-Fi", year = 2015, rating = 8.1, tags = "desert,chase,apocalypse", runtime = 120 },
                new { title = "Her", genre = "Drama,Romance,Sci-Fi", year = 2013, rating = 8.0, tags = "ai,love,loneliness", runtime = 126 },

                // Drama
                new { title = "Manchester by the Sea", genre = "Drama", year = 2016, rating = 7.8, tags = "grief,family,tragedy", runtime = 137 },
                new { title = "Moonlight", genre = "Drama", year = 2016, rating = 7.4, tags = "identity,coming-of-age,lgbt", runtime = 111 },
                new { title = "The Social Network", genre = "Biography,Drama", year = 2010, rating = 7.7, tags = "facebook,technology,friendship", runtime = 120 },
                new { title = "There Will Be Blood", genre = "Drama", year = 2007, rating = 8.2, tags = "oil,greed,american-dream", runtime = 158 },
                new { title = "No Country for Old Men", genre = "Crime,Drama,Thriller", year = 2007, rating = 8.1, tags = "violence,fate,texas", runtime = 122 },
                new { title = "12 Years a Slave", genre = "Biography,Drama,History", year = 2013, rating = 8.1, tags = "slavery,freedom,survival", runtime = 134 },
                new { title = "Birdman", genre = "Comedy,Drama", year = 2014, rating = 7.7, tags = "theater,ego,surreal", runtime = 119 },
                new { title = "The Revenant", genre = "Action,Adventure,Drama", year = 2015, rating = 8.0, tags = "survival,revenge,bear", runtime = 156 },
                new { title = "La La Land", genre = "Comedy,Drama,Music", year = 2016, rating = 8.0, tags = "love,dreams,music", runtime = 128 },
                new { title = "The Shape of Water", genre = "Drama,Fantasy,Romance", year = 2017, rating = 7.3, tags = "love,creature,cold-war", runtime = 123 },

                // Horror/Thriller
                new { title = "Get Out", genre = "Horror,Mystery,Thriller", year = 2017, rating = 7.7, tags = "racism,horror,psychology", runtime = 104 },
                new { title = "A Quiet Place", genre = "Drama,Horror,Sci-Fi", year = 2018, rating = 7.5, tags = "silence,family,monsters", runtime = 90 },
                new { title = "Hereditary", genre = "Drama,Horror,Mystery", year = 2018, rating = 7.3, tags = "family,occult,grief", runtime = 127 },
                new { title = "The Babadook", genre = "Drama,Horror,Mystery", year = 2014, rating = 6.8, tags = "motherhood,grief,monster", runtime = 94 },
                new { title = "It Follows", genre = "Horror,Mystery,Thriller", year = 2014, rating = 6.8, tags = "curse,supernatural,teen", runtime = 100 },
                new { title = "The Witch", genre = "Drama,Fantasy,Horror", year = 2015, rating = 6.9, tags = "witch,puritan,family", runtime = 92 },
                new { title = "Midsommar", genre = "Drama,Horror,Mystery", year = 2019, rating = 7.1, tags = "cult,sweden,breakup", runtime = 148 },
                new { title = "Us", genre = "Horror,Mystery,Thriller", year = 2019, rating = 6.8, tags = "doppelganger,family,horror", runtime = 116 },

                // Comedy
                new { title = "The Grand Budapest Hotel", genre = "Adventure,Comedy,Crime", year = 2014, rating = 8.1, tags = "hotel,murder,comedy", runtime = 99 },
                new { title = "Jojo Rabbit", genre = "Comedy,Drama,War", year = 2019, rating = 7.9, tags = "nazi,comedy,childhood", runtime = 108 },
                new { title = "Knives Out", genre = "Comedy,Crime,Drama", year = 2019, rating = 7.9, tags = "murder,mystery,family", runtime = 130 },
                new { title = "The Big Lebowski", genre = "Comedy,Crime", year = 1998, rating = 8.1, tags = "bowling,kidnapping,dude", runtime = 117 },
                new { title = "Superbad", genre = "Comedy", year = 2007, rating = 7.6, tags = "teen,friendship,party", runtime = 113 },
                new { title = "Step Brothers", genre = "Comedy", year = 2008, rating = 6.9, tags = "stepbrothers,comedy,family", runtime = 98 },
                new { title = "Anchorman", genre = "Comedy", year = 2004, rating = 7.2, tags = "news,comedy,1970s", runtime = 94 },
                new { title = "Tropic Thunder", genre = "Action,Comedy,War", year = 2008, rating = 7.0, tags = "war,actors,comedy", runtime = 107 },

                // Sci-Fi
                new { title = "Blade Runner 2049", genre = "Action,Drama,Mystery,Sci-Fi", year = 2017, rating = 8.0, tags = "android,future,identity", runtime = 164 },
                new { title = "Ex Machina", genre = "Drama,Sci-Fi,Thriller", year = 2014, rating = 7.7, tags = "ai,turing,test", runtime = 108 },
                new { title = "Arrival", genre = "Drama,Sci-Fi", year = 2016, rating = 7.9, tags = "alien,language,time", runtime = 116 },
                new { title = "The Martian", genre = "Adventure,Drama,Sci-Fi", year = 2015, rating = 8.0, tags = "mars,survival,science", runtime = 144 },
                new { title = "Gravity", genre = "Drama,Sci-Fi,Thriller", year = 2013, rating = 7.7, tags = "space,survival,isolation", runtime = 91 },
                new { title = "2001: A Space Odyssey", genre = "Adventure,Sci-Fi", year = 1968, rating = 8.3, tags = "space,evolution,ai", runtime = 149 },
                new { title = "Alien", genre = "Horror,Sci-Fi", year = 1979, rating = 8.4, tags = "space,monster,survival", runtime = 117 },
                new { title = "Aliens", genre = "Action,Adventure,Sci-Fi", year = 1986, rating = 8.3, tags = "space,marines,aliens", runtime = 137 },
                new { title = "Terminator 2", genre = "Action,Sci-Fi", year = 1991, rating = 8.5, tags = "robot,time-travel,future", runtime = 137 },
                new { title = "E.T.", genre = "Family,Sci-Fi", year = 1982, rating = 7.8, tags = "alien,friendship,childhood", runtime = 115 },

                // Action/Adventure  
                new { title = "John Wick", genre = "Action,Crime,Thriller", year = 2014, rating = 7.4, tags = "assassin,revenge,dog", runtime = 101 },
                new { title = "Mission Impossible", genre = "Action,Adventure,Thriller", year = 1996, rating = 7.1, tags = "spy,impossible,team", runtime = 110 },
                new { title = "The Raid", genre = "Action,Crime,Thriller", year = 2011, rating = 7.6, tags = "martial-arts,building,swat", runtime = 101 },
                new { title = "Casino Royale", genre = "Action,Adventure,Thriller", year = 2006, rating = 8.0, tags = "bond,poker,spy", runtime = 144 },
                new { title = "Die Hard", genre = "Action,Thriller", year = 1988, rating = 8.2, tags = "christmas,building,terrorist", runtime = 132 },
                new { title = "Heat", genre = "Action,Crime,Drama", year = 1995, rating = 8.2, tags = "heist,cop,criminal", runtime = 170 },
                new { title = "Speed", genre = "Action,Adventure,Crime", year = 1994, rating = 7.2, tags = "bus,bomb,chase", runtime = 116 },
                new { title = "The Rock", genre = "Action,Adventure,Thriller", year = 1996, rating = 7.4, tags = "alcatraz,prison,chemical", runtime = 136 },

                // Animation
                new { title = "Finding Nemo", genre = "Animation,Adventure,Comedy", year = 2003, rating = 8.1, tags = "fish,ocean,father", runtime = 100 },
                new { title = "The Incredibles", genre = "Animation,Action,Adventure", year = 2004, rating = 8.0, tags = "superhero,family,pixar", runtime = 125 },
                new { title = "Wall-E", genre = "Animation,Adventure,Family", year = 2008, rating = 8.4, tags = "robot,environment,love", runtime = 98 },
                new { title = "Up", genre = "Animation,Adventure,Comedy", year = 2009, rating = 8.2, tags = "old-man,adventure,balloons", runtime = 96 },
                new { title = "Inside Out", genre = "Animation,Comedy,Drama", year = 2015, rating = 8.1, tags = "emotions,childhood,mind", runtime = 95 },
                new { title = "Monsters Inc", genre = "Animation,Comedy,Family", year = 2001, rating = 8.1, tags = "monsters,child,energy", runtime = 92 },
                new { title = "Shrek", genre = "Animation,Adventure,Comedy", year = 2001, rating = 7.8, tags = "ogre,fairy-tale,love", runtime = 90 },
                new { title = "How to Train Your Dragon", genre = "Animation,Action,Adventure", year = 2010, rating = 8.1, tags = "dragon,viking,friendship", runtime = 98 },
                new { title = "Frozen", genre = "Animation,Adventure,Comedy", year = 2013, rating = 7.4, tags = "ice,sister,disney", runtime = 102 },
                new { title = "Moana", genre = "Animation,Adventure,Comedy", year = 2016, rating = 7.6, tags = "ocean,polynesian,journey", runtime = 107 },

                // Crime
                new { title = "Scarface", genre = "Crime,Drama", year = 1983, rating = 8.3, tags = "drugs,miami,power", runtime = 170 },
                new { title = "Casino", genre = "Crime,Drama", year = 1995, rating = 8.2, tags = "vegas,gambling,mafia", runtime = 178 },
                new { title = "The Untouchables", genre = "Crime,Drama,Thriller", year = 1987, rating = 7.9, tags = "prohibition,chicago,capone", runtime = 119 },
                new { title = "Donnie Brasco", genre = "Biography,Crime,Drama", year = 1997, rating = 7.7, tags = "undercover,fbi,mafia", runtime = 127 },
                new { title = "The Long Good Friday", genre = "Crime,Drama,Mystery", year = 1980, rating = 7.6, tags = "london,gangster,ira", runtime = 114 },
                new { title = "Miller's Crossing", genre = "Crime,Drama", year = 1990, rating = 7.7, tags = "prohibition,loyalty,betrayal", runtime = 115 },
                new { title = "L.A. Confidential", genre = "Crime,Drama,Mystery", year = 1997, rating = 8.2, tags = "corruption,hollywood,noir", runtime = 138 },
                new { title = "Training Day", genre = "Crime,Drama,Thriller", year = 2001, rating = 7.7, tags = "corrupt-cop,rookie,street", runtime = 122 },

                // Western
                new { title = "The Good, the Bad and the Ugly", genre = "Adventure,Western", year = 1966, rating = 8.8, tags = "civil-war,gold,standoff", runtime = 178 },
                new { title = "Unforgiven", genre = "Drama,Western", year = 1992, rating = 8.2, tags = "retired-gunslinger,revenge,violence", runtime = 131 },
                new { title = "True Grit", genre = "Adventure,Drama,Western", year = 2010, rating = 7.6, tags = "girl,marshal,revenge", runtime = 110 },
                new { title = "3:10 to Yuma", genre = "Action,Crime,Drama", year = 2007, rating = 7.7, tags = "outlaw,train,moral", runtime = 122 },
                new { title = "The Magnificent Seven", genre = "Action,Adventure,Western", year = 1960, rating = 7.7, tags = "gunfighters,village,protection", runtime = 128 },
                new { title = "Butch Cassidy and the Sundance Kid", genre = "Biography,Crime,Drama", year = 1969, rating = 8.0, tags = "outlaws,friendship,bolivia", runtime = 110 },
                new { title = "The Man Who Shot Liberty Valance", genre = "Drama,Western", year = 1962, rating = 8.1, tags = "legend,truth,democracy", runtime = 123 },
                new { title = "High Noon", genre = "Drama,Thriller,Western", year = 1952, rating = 8.0, tags = "marshal,noon,showdown", runtime = 85 },

                // War
                new { title = "Apocalypse Now", genre = "Drama,Mystery,War", year = 1979, rating = 8.4, tags = "vietnam,madness,river", runtime = 147 },
                new { title = "Full Metal Jacket", genre = "Drama,War", year = 1987, rating = 8.3, tags = "vietnam,boot-camp,war", runtime = 116 },
                new { title = "Platoon", genre = "Drama,War", year = 1986, rating = 8.1, tags = "vietnam,soldiers,morality", runtime = 120 },
                new { title = "Black Hawk Down", genre = "Drama,History,War", year = 2001, rating = 7.7, tags = "somalia,helicopter,soldiers", runtime = 144 },
                new { title = "We Were Soldiers", genre = "Action,Drama,History", year = 2002, rating = 7.2, tags = "vietnam,ia-drang,family", runtime = 138 },
                new { title = "The Thin Red Line", genre = "Drama,History,War", year = 1998, rating = 7.6, tags = "pacific,philosophy,nature", runtime = 170 },
                new { title = "Paths of Glory", genre = "Drama,War", year = 1957, rating = 8.4, tags = "wwi,court-martial,futility", runtime = 88 },
                new { title = "The Bridge on the River Kwai", genre = "Adventure,Drama,War", year = 1957, rating = 8.1, tags = "prisoners,bridge,obsession", runtime = 161 },

                // Romance
                new { title = "Casablanca", genre = "Drama,Romance,War", year = 1942, rating = 8.5, tags = "wwii,sacrifice,love", runtime = 102 },
                new { title = "The Princess Bride", genre = "Adventure,Family,Fantasy", year = 1987, rating = 8.0, tags = "fairy-tale,adventure,love", runtime = 98 },
                new { title = "When Harry Met Sally", genre = "Comedy,Drama,Romance", year = 1989, rating = 7.6, tags = "friendship,love,new-york", runtime = 96 },
                new { title = "Sleepless in Seattle", genre = "Comedy,Drama,Romance", year = 1993, rating = 6.8, tags = "widower,radio,empire-state", runtime = 105 },
                new { title = "Ghost", genre = "Drama,Fantasy,Romance", year = 1990, rating = 7.0, tags = "spirit,pottery,love", runtime = 127 },
                new { title = "Dirty Dancing", genre = "Drama,Music,Romance", year = 1987, rating = 7.0, tags = "dance,summer,class", runtime = 100 },
                new { title = "The Notebook", genre = "Drama,Romance", year = 2004, rating = 7.8, tags = "alzheimer,love,memory", runtime = 123 },
                new { title = "500 Days of Summer", genre = "Comedy,Drama,Romance", year = 2009, rating = 7.7, tags = "breakup,expectations,reality", runtime = 95 },

                // Fantasy
                new { title = "The Lord of the Rings: The Fellowship", genre = "Action,Adventure,Drama", year = 2001, rating = 8.8, tags = "fantasy,ring,journey", runtime = 178 },
                new { title = "The Lord of the Rings: The Two Towers", genre = "Action,Adventure,Drama", year = 2002, rating = 8.7, tags = "fantasy,war,towers", runtime = 179 },
                new { title = "Harry Potter and the Sorcerer's Stone", genre = "Adventure,Family,Fantasy", year = 2001, rating = 7.6, tags = "wizard,school,magic", runtime = 152 },
                new { title = "Harry Potter and the Prisoner of Azkaban", genre = "Adventure,Family,Fantasy", year = 2004, rating = 7.9, tags = "wizard,prison,time", runtime = 142 },
                new { title = "The Chronicles of Narnia", genre = "Adventure,Family,Fantasy", year = 2005, rating = 6.9, tags = "wardrobe,lion,children", runtime = 143 },
                new { title = "Pan's Labyrinth", genre = "Drama,Fantasy,War", year = 2006, rating = 8.2, tags = "fairy-tale,spanish-civil-war,girl", runtime = 118 },
                new { title = "The Dark Crystal", genre = "Adventure,Family,Fantasy", year = 1982, rating = 7.1, tags = "puppets,crystal,quest", runtime = 93 },
                new { title = "Labyrinth", genre = "Adventure,Family,Fantasy", year = 1986, rating = 7.4, tags = "bowie,maze,baby", runtime = 101 },

                // Thriller
                new { title = "North by Northwest", genre = "Action,Adventure,Mystery", year = 1959, rating = 8.3, tags = "mistaken-identity,chase,hitchcock", runtime = 136 },
                new { title = "Vertigo", genre = "Mystery,Romance,Thriller", year = 1958, rating = 8.3, tags = "obsession,fear,height", runtime = 128 },
                new { title = "Rear Window", genre = "Mystery,Thriller", year = 1954, rating = 8.4, tags = "voyeur,wheelchair,murder", runtime = 112 },
                new { title = "Psycho", genre = "Horror,Mystery,Thriller", year = 1960, rating = 8.5, tags = "motel,shower,mother", runtime = 109 },
                new { title = "The Birds", genre = "Drama,Horror,Mystery", year = 1963, rating = 7.7, tags = "birds,attack,california", runtime = 119 },
                new { title = "Dial M for Murder", genre = "Crime,Thriller", year = 1954, rating = 8.2, tags = "perfect-murder,blackmail,telephone", runtime = 105 },
                new { title = "The Third Man", genre = "Film-Noir,Mystery,Thriller", year = 1949, rating = 8.1, tags = "post-war,vienna,zither", runtime = 104 },
                new { title = "Double Indemnity", genre = "Crime,Drama,Film-Noir", year = 1944, rating = 8.3, tags = "insurance,murder,femme-fatale", runtime = 107 },

                // Biography
                new { title = "Gandhi", genre = "Biography,Drama", year = 1982, rating = 8.0, tags = "india,non-violence,independence", runtime = 191 },
                new { title = "Malcolm X", genre = "Biography,Drama,History", year = 1992, rating = 7.7, tags = "civil-rights,islam,assassination", runtime = 202 },
                new { title = "Ray", genre = "Biography,Drama,Music", year = 2004, rating = 7.7, tags = "blind,piano,soul", runtime = 152 },
                new { title = "Walk the Line", genre = "Biography,Drama,Music", year = 2005, rating = 7.8, tags = "johnny-cash,country,addiction", runtime = 136 },
                new { title = "The Aviator", genre = "Biography,Drama", year = 2004, rating = 7.5, tags = "howard-hughes,aviation,ocd", runtime = 170 },
                new { title = "A Beautiful Mind", genre = "Biography,Drama", year = 2001, rating = 8.2, tags = "mathematician,schizophrenia,nobel", runtime = 135 },
                new { title = "The Theory of Everything", genre = "Biography,Drama,Romance", year = 2014, rating = 7.7, tags = "hawking,physics,als", runtime = 123 },
                new { title = "Bohemian Rhapsody", genre = "Biography,Drama,Music", year = 2018, rating = 7.9, tags = "queen,freddie-mercury,aids", runtime = 134 },

                // Documentary Style
                new { title = "Citizenfour", genre = "Documentary,Biography", year = 2014, rating = 8.0, tags = "snowden,surveillance,whistleblower", runtime = 114 },
                new { title = "Free Solo", genre = "Documentary,Sport", year = 2018, rating = 8.1, tags = "climbing,el-capitan,risk", runtime = 100 },
                new { title = "Won't You Be My Neighbor", genre = "Documentary,Biography", year = 2018, rating = 8.4, tags = "mister-rogers,kindness,children", runtime = 94 },
                new { title = "March of the Penguins", genre = "Documentary,Family", year = 2005, rating = 7.5, tags = "antarctica,penguins,survival", runtime = 80 },

                // Musical
                new { title = "The Greatest Showman", genre = "Biography,Drama,Musical", year = 2017, rating = 7.5, tags = "circus,barnum,dreams", runtime = 105 },
                new { title = "Mamma Mia!", genre = "Comedy,Musical,Romance", year = 2008, rating = 6.4, tags = "abba,greece,wedding", runtime = 108 },
                new { title = "Chicago", genre = "Comedy,Crime,Musical", year = 2002, rating = 7.1, tags = "jazz-age,murder,fame", runtime = 113 },
                new { title = "Moulin Rouge", genre = "Drama,Musical,Romance", year = 2001, rating = 7.6, tags = "paris,cabaret,love", runtime = 127 },
                new { title = "The Sound of Music", genre = "Biography,Drama,Family", year = 1965, rating = 8.0, tags = "austria,von-trapp,nazi", runtime = 174 },
                new { title = "Singin' in the Rain", genre = "Comedy,Musical,Romance", year = 1952, rating = 8.3, tags = "hollywood,transition,talkies", runtime = 103 },
                new { title = "West Side Story", genre = "Crime,Drama,Musical", year = 1961, rating = 7.5, tags = "romeo-juliet,gangs,new-york", runtime = 152 },

                // Sports
                new { title = "Rocky", genre = "Drama,Sport", year = 1976, rating = 8.1, tags = "boxing,underdog,philadelphia", runtime = 120 },
                new { title = "Raging Bull", genre = "Biography,Drama,Sport", year = 1980, rating = 8.2, tags = "boxing,jealousy,violence", runtime = 129 },
                new { title = "The Karate Kid", genre = "Drama,Family,Sport", year = 1984, rating = 7.3, tags = "martial-arts,mentor,tournament", runtime = 126 },
                new { title = "Rudy", genre = "Biography,Drama,Sport", year = 1993, rating = 7.5, tags = "football,notre-dame,determination", runtime = 114 },
                new { title = "Field of Dreams", genre = "Drama,Family,Fantasy", year = 1989, rating = 7.5, tags = "baseball,father,corn", runtime = 107 },
                new { title = "The Natural", genre = "Drama,Sport", year = 1984, rating = 7.4, tags = "baseball,comeback,lightning", runtime = 138 },
                new { title = "Hoosiers", genre = "Drama,Sport", year = 1986, rating = 7.5, tags = "basketball,indiana,underdogs", runtime = 114 },
                new { title = "Remember the Titans", genre = "Biography,Drama,Sport", year = 2000, rating = 7.8, tags = "football,integration,teamwork", runtime = 113 },

                // More Recent Films (2020-2024)
                new { title = "Dune", genre = "Action,Adventure,Drama", year = 2021, rating = 8.0, tags = "desert,spice,prophecy", runtime = 155 },
                new { title = "No Time to Die", genre = "Action,Adventure,Thriller", year = 2021, rating = 7.3, tags = "bond,final,nanobots", runtime = 163 },
                new { title = "Spider-Man: No Way Home", genre = "Action,Adventure,Fantasy", year = 2021, rating = 8.2, tags = "multiverse,spider-men,villains", runtime = 148 },
                new { title = "The Batman", genre = "Action,Crime,Drama", year = 2022, rating = 7.8, tags = "detective,riddler,vengeance", runtime = 176 },
                new { title = "Top Gun: Maverick", genre = "Action,Drama", year = 2022, rating = 8.3, tags = "naval-aviation,legacy,mission", runtime = 131 },
                new { title = "Everything Everywhere All at Once", genre = "Action,Adventure,Comedy", year = 2022, rating = 7.8, tags = "multiverse,laundry,everything", runtime = 139 },
                new { title = "The Banshees of Inisherin", genre = "Comedy,Drama", year = 2022, rating = 7.8, tags = "friendship,ireland,ending", runtime = 114 },
                new { title = "Tar", genre = "Drama,Music", year = 2022, rating = 7.4, tags = "conductor,scandal,classical", runtime = 158 },
                new { title = "Avatar: The Way of Water", genre = "Action,Adventure,Fantasy", year = 2022, rating = 7.6, tags = "pandora,ocean,family", runtime = 192 },
                new { title = "Glass Onion", genre = "Comedy,Crime,Drama", year = 2022, rating = 7.2, tags = "murder-mystery,greece,detective", runtime = 139 }
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
