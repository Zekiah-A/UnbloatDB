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
        var group = nameof(T);
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

    public async Task<RecordStructure?> GetRecord<T>(string masterKey) where T : notnull
    {
        var group = nameof(T);
        var path = Path.Join(configuration.DataDirectory, group, masterKey);

        if (!File.Exists(path))
        {
            return default;
        }

        await using var openStream = File.OpenRead(path);
        var record = await configuration.FileFormat.Deserialise<RecordStructure>(openStream);
        return record;
    }
    
    /// <summary>
    /// Gets the first record from a supplied query property and value being searched for.
    /// </summary>
    /// <param name="byProperty">Name of property in record that we are searching for.</param>
    /// <param name="value">Value of the property being searched for.</param>
    /// <typeparam name="T">Type/group of record we are searching for</typeparam>
    public async Task<RecordStructure[]> FindRecords<T>(string byProperty, string value) where T : notnull
    {
        var group = nameof(T);
        var path = Path.Join(configuration.DataDirectory, group, "si", byProperty);

        if (!File.Exists(path))
        {
            // If this group doesn't even exist in the DB, fail.
            // If the group does exist, but the property they are searching for does not in the record type, fail.
            // If both group, record property exists, but no index directory for this group exists, regenerate all indexes for this group.
            // If group, record property, index directory exists, but no indexer for this specific property, then regenerate indexes for just this property.
        }
        
        var indexFile = await File.ReadAllLinesAsync(path);
        var index = indexFile
            .Select(line => line.Split(" "))
            .Where(keyVal => keyVal.Length == 2)
            .ToList();
        
        var keys = index.Select(keyValue => keyValue[1]) as string[];
        var values = index.Select(keyValue => keyValue[0]) as string[];
        var found = new List<RecordStructure>();

        var position = Array.BinarySearch(values, value);
        while (position != -1)
        {
            var record = await GetRecord<T>(keys[position]);
            if (record is not null)
            {
                found.Add(record);    
            }

            position = Array.BinarySearch(values, value);
        }

        return found.ToArray(); 
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
