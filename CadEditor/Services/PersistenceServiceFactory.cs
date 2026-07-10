namespace CadEditor.Services;

public static class PersistenceServiceFactory
{
    public static IPersistenceService Create(PersistenceFormat format) => format switch
    {
        PersistenceFormat.Json => new JsonPersistenceService(),
        PersistenceFormat.Sqlite => new SqlitePersistenceService(),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };
}
