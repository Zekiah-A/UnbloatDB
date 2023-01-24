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
    /// Gets the first record from a supplied query property and value being searched for. For getting a record by it's
    /// unique Master Key, please use GetRecord<T>(string masterKey) instead, otherwise, this method will not return any results.
    /// </summary>
    /// <param name="byProperty">Name of property in record that we are searching for.</param>
    /// <param name="value">Value of the property being searched for.</param>
    /// <typeparam name="U">The data type of the record we are searching for.</typeparam>
    public async Task<RecordStructure<T>[]> FindRecords<T, U>(string byProperty, U value) where T : notnull where U : notnull
    {
        var group = typeof(T).Name;
        var path = Path.Join(configuration.DataDirectory, group, configuration.IndexerDirectory, byProperty);

        if (!File.Exists(path))
        {
            // TODO: File safety. 
            // If this group doesn't even exist in the DB, fail.
            // If the group does exist, but the property they are searching for does not in the record type, fail.
            // If both group, record property exists, but no index directory for this group exists, regenerate all indexes for this group.
            // If group, record property, index directory exists, but no indexer for this specific property, then regenerate indexes for just this property.
        }

        var index = await SmartIndexer.ReadIndex(path);
        var values = index.Select(keyValue => keyValue.Key).ToArray<object>();
        var found = new List<RecordStructure<T>>();
        var convertedValue = SmartIndexer.FormatObject(value);
        
        var position = Array.BinarySearch(values, convertedValue);
        while (position > 0)
        {
            var record = await GetRecord<T>(index[position].Value);
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
    /// Updates saved data and indexer information for a record without mutating it's master key / reference.
    /// </summary>
    /// <param name="record">Record structure of updated record</param>
    public async Task<bool> UpdateRecord<T>(RecordStructure<T> record) where T : notnull
    {
        var group = typeof(T).Name;
        var groupPath = Path.Join(configuration.DataDirectory, group);

        if (!Directory.Exists(groupPath))
        {
            return false;
        }
        
        await indexer.RemoveFromIndex(record);

        var serialisedRecord = await configuration.FileFormat.Serialise(record);
        await File.WriteAllTextAsync(Path.Join(configuration.DataDirectory, group, record.MasterKey), serialisedRecord);

        // Regenerate record indexes
        await indexer.AddToIndex(record);

        return true;
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

    /// <summary>
    /// Please only use this method for getting ALL records in a group, and returning only that. If you want to get all records
    /// in a group, and then filter them down to select only those with a specific property, such as with linq. PLEASE use
    /// "find records" instead, which will be able to make use of the smart indexer to run much faster.
    /// </summary>
    /// <typeparam name="T">Record group we are retrieving all for.</typeparam>
    /// <returns>All records contained within the specified group.</returns>
    public async Task<RecordStructure<T>[]> GetAllRecords<T>() where T : notnull
    {
        var group = typeof(T).Name;
        var path = Path.Join(configuration.DataDirectory, group);

        var found = new List<RecordStructure<T>>();
        
        if (!Directory.Exists(path))
        {
            return found.ToArray();
        }

        foreach (var recordPath in Directory.GetFiles(path))
        {
            await using var openStream = File.OpenRead(recordPath);
            found.Add(await configuration.FileFormat.Deserialise<RecordStructure<T>>(openStream));
        }

        return found.ToArray();
    }

    public async Task<RecordStructure<T>[]> FindRecordsAfter<T, U>(string byProperty, U value, bool descending = false) where T : notnull where U : notnull
    {
        var group = typeof(T).Name;
        var path = Path.Join(configuration.DataDirectory, group, configuration.IndexerDirectory, byProperty);

        if (!File.Exists(path))
        {
            // TODO: File safety
            // If this group doesn't even exist in the DB, fail.
            // If the group does exist, but the property they are searching for does not in the record type, fail.
            // If both group, record property exists, but no index directory for this group exists, regenerate all indexes for this group.
            // If group, record property, index directory exists, but no indexer for this specific property, then regenerate indexes for just this property.
        }

        var index = await SmartIndexer.ReadIndex(path);
        var values = index.Select(keyValue => keyValue.Key).ToArray<object>();
        var found = new List<RecordStructure<T>>();
        var position = 0;

        if (value is not IComparable comparableValue)
        {
            return found.ToArray();
        }
        
        while (comparableValue.CompareTo(values[position]) != -1)
        {
            found.Add((await GetRecord<T>(index.ElementAt(position).Value))!);
            position++;
        }

        if (descending)
        {
            found.Reverse();
        }

        return found.ToArray();
    }
    
    /// <summary>
    /// Gets all records with a value that preceeds that of the given input. For example, in a database of ages from 1-10,
    /// if searched value is "Age 5" this method will return the records with ages 1-5. If descending is true, it will be
    /// returned in order of 5-1.
    /// </summary>
    /// <param name="byProperty">Name of property in record that we are searching for.</param>
    /// <param name="value">Value of the property being searched for.</param>
    /// <typeparam name="U">The data type of the record we are searching for.</typeparam>
    public async Task<RecordStructure<T>[]> FindRecordsBefore<T, U>(string byProperty, U value, bool descending = false) where T : notnull where U : notnull
    {
        var group = typeof(T).Name;
        var path = Path.Join(configuration.DataDirectory, group, configuration.IndexerDirectory, byProperty);

        if (!File.Exists(path))
        {
            // TODO: File safety
            // If this group doesn't even exist in the DB, fail.
            // If the group does exist, but the property they are searching for does not in the record type, fail.
            // If both group, record property exists, but no index directory for this group exists, regenerate all indexes for this group.
            // If group, record property, index directory exists, but no indexer for this specific property, then regenerate indexes for just this property.
        }

        var index = await SmartIndexer.ReadIndex(path);
        var values = index.Select(keyValue => keyValue.Key).ToArray<object>();
        var found = new List<RecordStructure<T>>();
        var position = 0;

        if (value is not IComparable comparableValue)
        {
            return found.ToArray();
        }
        
        while (comparableValue.CompareTo(values[position]) != -1)
        {
            found.Add((await GetRecord<T>(index.ElementAt(position).Value))!);
            position++;
        }

        if (descending)
        {
            found.Reverse();
        }

        return found.ToArray();
    }
}
