using System.IO;
using System.Text.Json;
using CadEditor.Models;
using Microsoft.Data.Sqlite;

namespace CadEditor.Services;

public class SqlitePersistenceService : IPersistenceService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    public void Save(string path, IEnumerable<Shape> shapes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(shapes);

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();

        using var transaction = connection.BeginTransaction();
        ExecuteNonQuery(connection, transaction, "DROP TABLE IF EXISTS Shapes;");
        ExecuteNonQuery(connection, transaction,
            "CREATE TABLE Shapes (Id INTEGER PRIMARY KEY AUTOINCREMENT, Type TEXT NOT NULL, PropertiesJson TEXT NOT NULL);");

        foreach (var shape in shapes)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO Shapes (Type, PropertiesJson) VALUES ($type, $propertiesJson);";
            command.Parameters.AddWithValue("$type", GetShapeType(shape));
            command.Parameters.AddWithValue("$propertiesJson", JsonSerializer.Serialize(shape, shape.GetType(), SerializerOptions));
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public List<Shape> Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Type, PropertiesJson FROM Shapes ORDER BY Id;";

        var shapes = new List<Shape>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            string type = reader.GetString(0);
            string propertiesJson = reader.GetString(1);
            shapes.Add(DeserializeShape(type, propertiesJson));
        }

        return shapes;
    }

    private static void ExecuteNonQuery(SqliteConnection connection, SqliteTransaction transaction, string commandText)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        command.ExecuteNonQuery();
    }

    private static string GetShapeType(Shape shape) => shape switch
    {
        LineShape => "line",
        CircleShape => "circle",
        RectangleShape => "rectangle",
        PolygonShape => "polygon",
        ArcShape => "arc",
        _ => throw new NotSupportedException($"Unsupported shape type: {shape.GetType().Name}")
    };

    private static Shape DeserializeShape(string type, string propertiesJson) => type switch
    {
        "line" => Deserialize<LineShape>(propertiesJson),
        "circle" => Deserialize<CircleShape>(propertiesJson),
        "rectangle" => Deserialize<RectangleShape>(propertiesJson),
        "polygon" => Deserialize<PolygonShape>(propertiesJson),
        "arc" => Deserialize<ArcShape>(propertiesJson),
        _ => throw new NotSupportedException($"Unsupported shape type: {type}")
    };

    private static T Deserialize<T>(string propertiesJson)
        where T : Shape
    {
        return JsonSerializer.Deserialize<T>(propertiesJson, SerializerOptions)
            ?? throw new InvalidDataException($"Invalid JSON for {typeof(T).Name}.");
    }
}
