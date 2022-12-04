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
    /// <returns> String master key of the newly created record </returns>
    public async Task<string> CreateRecord<T> (T data) where T : notnull
    {
        var group = typeof(T).Name;
        var masterKey = Guid.NewGuid().ToString();
        var structuredRecord = new RecordStructure<T>(masterKey, data);
        var groupPath = Path.Join(configuration.DataDirectory, group);

        if (!Directory.Exists(groupPath))
        {
            Directory.CreateDirectory(groupPath); //Type template
            await indexer.BuildGroupIndexDirectoryFor<T>();
        }
        
        var serialisedRecord = await configuration.FileFormat.Serialise(structuredRecord);
        await File.WriteAllTextAsync(Path.Join(configuration.DataDirectory, group, masterKey), serialisedRecord);
        
        await indexer.AddToIndex(structuredRecord);

        return masterKey;
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
    public async Task<RecordStructure<T>[]> FindRecords<T, U>(string byProperty, U value) where T : notnull where U : notnull
    {
        var group = typeof(T).Name;
        var path = Path.Join(configuration.DataDirectory, group, configuration.IndexerDirectory, byProperty);

        if (!File.Exists(path))
        {
            // If this group doesn't even exist in the DB, fail.
            // If the group does exist, but the property they are searching for does not in the record type, fail.
            // If both group, record property exists, but no index directory for this group exists, regenerate all indexes for this group.
            // If group, record property, index directory exists, but no indexer for this specific property, then regenerate indexes for just this property.
        }

        var indexFile = await File.ReadAllLinesAsync(path);
        var index = indexFile
            .Select(line =>
            {
                var last = line.LastIndexOf(" ", StringComparison.Ordinal);
                return last == -1 ? Array.Empty<string>() : new[] { line[..last], line[(last + 1)..] };
            })
            .ToList();

        var values = index.Select(keyValue => keyValue[0]).ToArray();
        var found = new List<RecordStructure<T>>();
        var convertedValue = typeof(U).IsEnum ? Convert.ChangeType(value, typeof(int)).ToString() : value.ToString(); //TODO: Not all enums use int, use getTypeCode instead

        //TODO: Add special case for enums

        var position = Array.BinarySearch(values, convertedValue);
        while (position > 0)
        {
            var record = await GetRecord<T>(index[position][1]);
            if (record is not null)
            {
                found.Add(record);
                values = values.RemoveAt(position);
            }

            position = Array.BinarySearch(values, convertedValue);
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
