using System.IO;
using System.Text.Json;
using CadEditor.Models;

namespace CadEditor.Services;

public class JsonPersistenceService : IPersistenceService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public void Save(string path, IEnumerable<Shape> shapes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(shapes);

        var json = JsonSerializer.Serialize(shapes.ToList(), SerializerOptions);
        File.WriteAllText(path, json);
    }

    public List<Shape> Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<Shape>>(json, SerializerOptions) ?? new List<Shape>();
    }
}
