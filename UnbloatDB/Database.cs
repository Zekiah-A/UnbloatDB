using System.Reflection;

namespace UnbloatDB;

public class Database
{
    private readonly Dictionary<string, string> indexerCache;
    private readonly Config configuration;
    private readonly SmartIndexer indexer;

    public Database(Config config)
    {
        indexerCache = new Dictionary<string, string>();
        indexer = new SmartIndexer(configuration);
        configuration = config;
    }

    public async Task CreateRecord<T> (T record) where T : notnull
    {
        var group = typeof(T).Name;
        var masterKey = new Guid().ToString();
        var structuredRecord = new RecordStructure(masterKey, record);
        var groupPath = Path.Join(configuration.DataDirectory, group);

        if (!Directory.Exists(groupPath))
        {
            Directory.CreateDirectory(groupPath); //Type template
            await indexer.BuildGroupIndexDirectoryFor<T>();
        }
        
        var serialisedRecord = await configuration.FileFormat.Serialise(structuredRecord);
        await File.WriteAllTextAsync(serialisedRecord, Path.Join(configuration.DataDirectory, group, masterKey));
        
        await indexer.AddToIndex(structuredRecord);
    }

    public async Task DeleteRecord<T> (string masterKey, bool deleteRefrences = false)
    {
        var group = typeof(T).Name;
        var recordPath = Path.Join(configuration.DataDirectory, group, masterKey);
        
        if (File.Exists(group))
        {
            File.Delete(recordPath);
        }
    }

    public async Task GetRecord<T>(string masterKey)
    {
        
    }

    public bool GroupExists(string name)
    {
        return File.Exists(Path.Join(configuration.DataDirectory, name));
    }
}