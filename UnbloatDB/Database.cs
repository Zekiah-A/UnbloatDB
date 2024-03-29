namespace UnbloatDB;

public sealed class Database
{
    private readonly Configuration configuration;
    private readonly SmartIndexer indexer;

    public Database(Configuration config)
    {
        configuration = config;
        indexer = new SmartIndexer(config, this);
    }

    /// <summary>
    /// Creates a new record in the database from supplied data
    /// </summary>
    /// <param name="data">The data of the record we are creating (assigned a master key, converted into record structure by DB).</param>
    /// <typeparam name="T">The data type of the record we are creating.</typeparam>
    /// <returns> String master key of the newly created record </returns>
    public async Task<string> CreateRecord<T>(T data) where T : notnull
    {
        var group = typeof(T).Name;
        var masterKey = Guid.NewGuid().ToString();
        var structuredRecord = new RecordStructure<T>(masterKey, data);
        var groupPath = Path.Join(configuration.DataDirectory, group);

        if (!Directory.Exists(groupPath))
        {
            Directory.CreateDirectory(groupPath);
            indexer.BuildGroupIndexDirectoryFor<T>();
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
    /// <param name="propertyName">Name of property in record that we are searching for.</param>
    /// <param name="value">Value of the property being searched for.</param>
    /// <typeparam name="TValue">The data type of the record we are searching for.</typeparam>
    public async Task<RecordStructure<TKey>[]> FindRecords<TKey, TValue>(string propertyName, TValue value) where TKey : notnull where TValue : notnull
    {
        var group = typeof(TKey).Name;
        var path = Path.Join(configuration.DataDirectory, group, configuration.IndexerDirectory, propertyName);
        
        var indexFile = indexer.Indexers.GetValueOrDefault(path) ?? indexer.OpenIndex(path);

        var values = indexFile.Index.Select(keyValue => keyValue.Key).ToArray();
        var found = new List<RecordStructure<TKey>>();
        var convertedValue = SmartIndexer.FormatObject(value);
        
        var position = Array.BinarySearch(values, convertedValue);
        while (position > 0)
        {
            var record = await GetRecord<TKey>(indexFile.Index[position].Value);
            if (record is not null)
            {
                found.Add(record);
                values = values.RemoveAt(position);
            }

            position = Array.BinarySearch(values, convertedValue);
        }

        return found.ToArray(); 
    }

    public async Task<bool> UpdateRecord<T>(string masterKey, T data) where T: notnull
    {
        var group = typeof(T).Name;
        var groupPath = Path.Join(configuration.DataDirectory, group);
        
        if (!Directory.Exists(groupPath))
        {
            return false;
        }
        
        var record = await GetRecord<T>(masterKey);
        if (record is null)
        {
            return false;
        }
        
        var updatedRecord = record with { Data = data };
        await indexer.RemoveFromIndex(updatedRecord);
        
        var serialisedRecord = await configuration.FileFormat.Serialise(updatedRecord);
        await File.WriteAllTextAsync(Path.Join(groupPath, record.MasterKey), serialisedRecord);

        // Regenerate record indexes
        await indexer.AddToIndex(updatedRecord);
        
        return true;
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
        await File.WriteAllTextAsync(Path.Join(groupPath, record.MasterKey), serialisedRecord);

        // Regenerate record indexes
        await indexer.AddToIndex(record);

        return true;
    } 

    /// <summary>
    /// Deletes a specified record (via master key) from the database.
    /// </summary>
    /// <param name="masterKey">The master key of the record that is being deleted.</param>
    /// <typeparam name="T">The data type of the record that is being deleted.</typeparam>
    public async Task DeleteRecord<T>(string masterKey) where T : notnull
    {
        var group = typeof(T).Name;
        var recordPath = Path.Join(configuration.DataDirectory, group, masterKey);
        var record = await GetRecord<string>(masterKey);
        
        if (File.Exists(group) && record is not null)
        {
            await indexer.RemoveFromIndex(record);
            File.Delete(recordPath);
        }
    }

    /// <summary>
    /// Please only use this method for getting ALL records in a group, and returning only that. If you want to get all records
    /// in a group, and then filter them down to select only those with a specific property, such as via linq. PLEASE use
    /// the "FindRecords\<T, U>" method instead, which will be able to make use of the smart indexer to run much faster.
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
    
    public async Task<RecordStructure<T>[]> FindRecordsAfter<T, TValue>(string byProperty, TValue value, bool descending = false) where T : notnull where TValue : notnull
    {
        var group = typeof(T).Name;
        var path = Path.Join(configuration.DataDirectory, group, configuration.IndexerDirectory, byProperty);
        var found = new List<RecordStructure<T>>();

        if (!File.Exists(path))
        {
            return found.ToArray();
        }

        var indexFile = indexer.Indexers.GetValueOrDefault(path) ?? indexer.OpenIndex(path);
        var values = indexFile.Index.Select(keyValue => keyValue.Key).ToArray<object>();
        var position = 0;

        if (value is not IComparable comparableValue)
        {
            return found.ToArray();
        }
        
        while (comparableValue.CompareTo(values[position]) != -1)
        {
            found.Add((await GetRecord<T>(indexFile.Index.ElementAt(position).Value))!);
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
    /// <typeparam name="TValue">The data type of the record we are searching for.</typeparam>
    public async Task<RecordStructure<T>[]> FindRecordsBefore<T, TValue>(string byProperty, TValue value, bool descending = false) where T : notnull where TValue : notnull
    {
        var group = typeof(T).Name;
        var path = Path.Join(configuration.DataDirectory, group, configuration.IndexerDirectory, byProperty);
        var found = new List<RecordStructure<T>>();

        if (!File.Exists(path))
        {
            return found.ToArray();
        }

        var indexFile = indexer.Indexers.GetValueOrDefault(path) ?? indexer.OpenIndex(path);
        var values = indexFile.Index.Select(keyValue => keyValue.Key).ToArray<object>();
        var position = 0;

        if (value is not IComparable comparableValue)
        {
            return found.ToArray();
        }
        
        while (comparableValue.CompareTo(values[position]) != -1)
        {
            found.Add((await GetRecord<T>(indexFile.Index.ElementAt(position).Value))!);
            position++;
        }

        if (descending)
        {
            found.Reverse();
        }

        return found.ToArray();
    }
}
