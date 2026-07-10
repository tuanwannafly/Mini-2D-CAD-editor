using CadEditor.Models;

namespace CadEditor.Services;

public interface IPersistenceService
{
    void Save(string path, IEnumerable<Shape> shapes);

    List<Shape> Load(string path);
}
