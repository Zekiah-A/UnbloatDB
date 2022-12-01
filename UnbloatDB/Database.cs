namespace UnbloatDB;

public sealed class Database
{
    private readonly Config configuration;
    private readonly SmartIndexer indexer;

    public Database(Config config)
    {
        configuration = config;
        indexer = new SmartIndexer(configuration);
    }

    /// <summary>
    /// Creates a new record in the database from supplied data
    /// </summary>
    /// <param name="data">The data of the record we are creating (assigned a master key, converted into record structure by DB).</param>
    /// <typeparam name="T">The data type of the record we are creating.</typeparam>
    public async Task CreateRecord<T> (T data) where T : notnull
    {
        var group = typeof(T).Name;
        var masterKey = Guid.NewGuid().ToString();
        var structuredRecord = new RecordStructure<T>(masterKey, data);
        var groupPath = Path.Join(configuration.DataDirectory, group);

        if (!Directory.Exists(groupPath))
        {
            Directory.CreateDirectory(groupPath); //Type template
            indexer.BuildGroupIndexDirectoryFor<T>();
        }
        
        var serialisedRecord = await configuration.FileFormat.Serialise(structuredRecord);
        await File.WriteAllTextAsync(Path.Join(configuration.DataDirectory, group, masterKey), serialisedRecord);
        
        await indexer.AddToIndex(structuredRecord);
    }

    /// <summary>
    /// Gets the first record from a supplied query property and value being searched for.
    /// </summary>
    /// <param name="byProperty">Name of property in record that we are searching for.</param>
    /// <param name="masterKey">Value of the property being searched for.</param>
    /// <typeparam name="T">The data type of the record we are searching for.</typeparam>
    public async Task<RecordStructure<T>?> GetRecord<T>(string masterKey) where T : notnull
    {
        var group = typeof(T).Name;
        var path = Path.Join(configuration.DataDirectory, group, masterKey);

        if (!File.Exists(path))
        {
            return default;
        }

        await using var openStream = File.OpenRead(path);
        var record = await configuration.FileFormat.Deserialise<RecordStructure<T>>(openStream);
        return record;
    }
    
    /// <summary>
    /// Gets the first record from a supplied query property and value being searched for.
    /// </summary>
    /// <param name="byProperty">Name of property in record that we are searching for.</param>
    /// <param name="value">Value of the property being searched for.</param>
    /// <typeparam name="T">The data type of the record we are searching for.</typeparam>
    public async Task<RecordStructure<T>[]> FindRecords<T>(string byProperty, string value) where T : notnull
    {
        var group = typeof(T).Name;
        var path = Path.Join(configuration.DataDirectory, group, "0si", byProperty);

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
        var found = new List<RecordStructure<T>>();

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

    /// <summary>
    /// Deletes a specified record (via masterkey) from the database.
    /// </summary>
    /// <param name="masterKey">The masterkey of the record that is being deleted.</param>
    /// <param name="deleteRefrences">Delete all references to this record by other records via intraKey.</param>
    /// <typeparam name="T">The data type of the record that is being deleted.</typeparam>
    public async Task DeleteRecord<T> (string masterKey, bool deleteRefrences = false) where T : notnull
    {
        var group = typeof(T).Name;
        var recordPath = Path.Join(configuration.DataDirectory, group, masterKey);
        
        if (File.Exists(group))
        {
            File.Delete(recordPath);
        }
    }
}
