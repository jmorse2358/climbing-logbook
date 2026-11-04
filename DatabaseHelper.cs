using SQLite;
using System.IO;
using Microsoft.Maui.Storage;
using System.Xml.Linq;
using System.Diagnostics;

namespace Climbing_Logbook;
public static class DatabaseHelper
{
    private static SQLiteConnection _database;
    private static readonly string database_name = "logbook.db";
    private static readonly string database_path = Path.Combine(FileSystem.AppDataDirectory, database_name);

    public static void initialize_database()
    {
        Debug.WriteLine($"{database_path}");
        if (!File.Exists(database_path))
        {
            // copy from /raw to writable location
            using var stream = FileSystem.OpenAppPackageFileAsync(database_name).Result;
            using var fileStream = File.Create(database_path);
            stream.CopyTo(fileStream);
            //Debug.WriteLine($"file does indeed exist, db path: {database_path}");
        }

        _database = new SQLiteConnection(database_path);

        // Ensure the climb_entry table exists
        _database.CreateTable<climb_entry>();
    }

    public static SQLiteConnection get_database()
    {
        if (_database == null) // the database exists
        {
            if (!File.Exists(database_path)) // the file exists at the specified path
            {
                using var stream = FileSystem.OpenAppPackageFileAsync(database_name).Result; // open it up & save data
                using var fileStream = File.Create(database_path); // creates the new copy in readable location
                stream.CopyTo(fileStream); // transfers over all the data 
            }

            _database = new SQLiteConnection(database_path);
        }


        return _database;
    }

    // takes the functionality out of the read_entries procedure, centralised here how
    public static List<climb_entry> get_climbs()
    {
        // initializes the database if its not there 
        if (_database == null)
            initialize_database();
        var database = get_database();
        return database.Table<climb_entry>().OrderByDescending(c => c.DateAdded).ToList();
    }
    public static void add_climb(string name, string notes, string actualGrade, string recommendedGrade, string tagChecksum, int attempts, bool outdoors, string rockType, double rockQuality, string climb_type, int score, DateTime dateAdded, DateTime dateModified)
    {
        var database = get_database();
        var climb = new climb_entry
        {
            Name = name,
            Notes = notes,
            ActualGrade = actualGrade,
            RecommendGrade = recommendedGrade,
            TagCheckSum = tagChecksum,
            Attempts = attempts,
            Outdoor = outdoors ? 1 : 0,
            RockType = rockType,
            RockQuality = rockQuality,
            ClimbType = climb_type,
            Score = score,
            DateAdded = dateAdded.ToString("yyyy-MM-dd"),
            DateModified = dateModified.ToString("yyyy-MM-dd")
        };
        database.Insert(climb);
    }
    public static void delete_entry(int id)
    {
        var database = get_database();
        var climb = database.Table<climb_entry>().FirstOrDefault(c => c.Id == id);
        if (climb != null)
        {
            database.Delete(climb);
        }
    }

    public static void edit_entry(int id, string name, string notes, string actualGrade, string recommendedGrade, string tagChecksum, int attempts, bool outdoors, string rockType, double rockQuality, string climb_type, int score, DateTime dateAdded, DateTime dateModified)
    {
        var database = get_database();
        var climb = database.Table<climb_entry>().FirstOrDefault(c => c.Id == id);
        if (climb != null)
        {
            // required fields
            climb.Name = name;
            climb.ActualGrade = actualGrade;
            climb.Attempts = attempts;

            // non required fields
            climb.Notes = notes;
            climb.RecommendGrade = recommendedGrade;
            climb.TagCheckSum = tagChecksum;
            climb.Outdoor = outdoors ? 1 : 0; // some handwavy c# casting
            climb.RockType = rockType;
            climb.RockQuality = rockQuality;
            climb.ClimbType = climb_type;
            climb.Score = score;
            climb.DateAdded = dateAdded.ToString();
            climb.DateModified = dateModified.ToString();
            database.Update(climb);
        }
    }

    public class climb_entry
    {
        // sets the ID as the primary key
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; } // adding an Isd value for alleged better ORM management

        public string Name { get; set; }
        public string Notes { get; set; }
        public string ActualGrade { get; set; }
        public string RecommendGrade { get; set; }
        public string TagCheckSum { get; set; }
        public int Attempts { get; set; }
        public int Outdoor { get; set; }
        public string RockType { get; set; }
        public double RockQuality { get; set; }
        public string ClimbType { get; set; }
        public int Score { get; set; }
        public string DateAdded { get; set; }
        public string DateModified { get; set; }
    }
}
