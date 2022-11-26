using System.Reflection;
using System.Text.Json;
using Microsoft.VisualBasic.CompilerServices;
using UnbloatDB.Serialisers;

namespace UnbloatDB;

public class Database
{
    private readonly Dictionary<string, string> indexerCache;
    private readonly Config configuration;
    private readonly SmartIndexer indexer;

    public Database(Config config)
    {
        configuration = config;
        indexerCache = new Dictionary<string, string>();
        indexer = new SmartIndexer(configuration);
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

    public async Task<T?> GetRecord<T>(string masterKey) where T : notnull
    {
        var group = typeof(T).Name;
        var path = Path.Join(configuration.DataDirectory, group, masterKey);

        if (!File.Exists(path))
        {
            return default;
        }

        await using var openStream = File.OpenRead(path);
        var record = await configuration.FileFormat.Deserialise<T>(openStream);
        return record;
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
}
