namespace UnbloatDB;

public sealed class Database
{
    private readonly Configuration configuration;
    private readonly SmartIndexer indexer;
    private readonly ReferenceResolver ReferenceResolver;
    
    public Database(Configuration config)
    {
        configuration = config;
        indexer = new SmartIndexer(config);
        ReferenceResolver = new ReferenceResolver(this);
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
        
        indexer.AddToIndex(structuredRecord);

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
    public async IAsyncEnumerable<RecordStructure<TGroup>> FindRecords<TGroup, TValue>(string propertyName, TValue value) where TGroup : notnull where TValue : notnull
    {
        var group = typeof(TGroup).Name;
        var path = Path.Join(configuration.DataDirectory, group, configuration.IndexerDirectory, propertyName);
        
        var indexFile = indexer.Indexers.GetValueOrDefault(path) ?? indexer.OpenIndex(path, typeof(TValue));

        var values = indexFile.IndexValues.ToList(); // We have to copy it so we don't mutate the index (would be fatal)

        var position = values.BinarySearch(value);
        while (position > 0)
        {
            var recordStructure = await GetRecord<TGroup>(indexFile.IndexKeys[position]);
            if (recordStructure is not null)
            {
                yield return recordStructure;
                values.RemoveAt(position);
            }

            position = values.BinarySearch(value);
        }
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
        indexer.RemoveFromIndex(updatedRecord);
        
        var serialisedRecord = await configuration.FileFormat.Serialise(updatedRecord);
        await File.WriteAllTextAsync(Path.Join(groupPath, record.MasterKey), serialisedRecord);

        // Regenerate record indexes
        indexer.AddToIndex(updatedRecord);
        
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
        
        indexer.RemoveFromIndex(record);

        var serialisedRecord = await configuration.FileFormat.Serialise(record);
        await File.WriteAllTextAsync(Path.Join(groupPath, record.MasterKey), serialisedRecord);

        // Regenerate record indexes
        indexer.AddToIndex(record);

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
        var record = await GetRecord<T>(masterKey);
        
        if (File.Exists(recordPath) && record is not null)
        {
            indexer.RemoveFromIndex(record);
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
    public async IAsyncEnumerable<RecordStructure<T>> GetAllRecords<T>() where T : notnull
    {
        var group = typeof(T).Name;
        var path = Path.Join(configuration.DataDirectory, group);


        if (!Directory.Exists(path))
        {
            retur;
        }

        foreach (var recordPath in Directory.GetFiles(path))
        {
            await using var openStream = File.OpenRead(recordPath);
            found.Add(await configuration.FileFormat.Deserialise<RecordStructure<T>>(openStream));
        }

        return found.ToArray();
    }

    public IndexerFile GetGroupIndexer<TGroup>(string forProperty, Type propertyType)
    {
        var group = typeof(TGroup).Name;
        var path = Path.Join(configuration.DataDirectory, group, configuration.IndexerDirectory, forProperty);
        var index = indexer.Indexers.GetValueOrDefault(path) ?? indexer.OpenIndex(path, propertyType);
        
        return index;
    }

    public async Task<RecordStructure<TGroup>[]> FindRecordsAfter<TGroup, TValue>(string byProperty, TValue value, bool descending = false) where TGroup : notnull where TValue : notnull
    {
        var group = typeof(TGroup).Name;
        var path = Path.Join(configuration.DataDirectory, group, configuration.IndexerDirectory, byProperty);

        if (!File.Exists(path))
        {
            return Array.Empty<RecordStructure<TGroup>>();
        }

        var indexFile = indexer.Indexers.GetValueOrDefault(path) ?? indexer.OpenIndex(path, typeof(TValue));
        var values = indexFile.IndexValues.ToList();  // We have to copy it so we don't mutate the index (would be fatal)
        var found = new List<RecordStructure<TGroup>>();
        var position = 0;

        if (value is not IComparable comparableValue)
        {
            return Array.Empty<RecordStructure<TGroup>>();
        }

        while (comparableValue.CompareTo(values[position]) != -1)
        {
            var record = await GetRecord<TGroup>(indexFile.Index.ElementAt(position).Key);
            if (record is not null)
            {
                found.Add(record);
            }
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
    public async Task<RecordStructure<TGroup>[]> FindRecordsBefore<TGroup, TValue>(string byProperty, TValue value, bool descending = false) where TGroup : notnull where TValue : notnull
    {
        var group = typeof(TGroup).Name;
        var path = Path.Join(configuration.DataDirectory, group, configuration.IndexerDirectory, byProperty);

        if (!File.Exists(path))
        {
            return Array.Empty<RecordStructure<TGroup>>();
        }

        var indexFile = indexer.Indexers.GetValueOrDefault(path) ?? indexer.OpenIndex(path, typeof(TValue));
        var values = indexFile.IndexValues.ToList();  // We have to copy it so we don't mutate the index (would be fatal)
        var found = new List<RecordStructure<TGroup>>();
        var position = 0;

        if (value is not IComparable comparableValue)
        {
            return Array.Empty<RecordStructure<TGroup>>();
        }
        
        while (comparableValue.CompareTo(values[position]) != -1)
        {
            var record = await GetRecord<TGroup>(indexFile.Index.ElementAt(position).Key);
            if (record is not null)
            {
                found.Add(record);
            }
            position++;
        }

        if (descending)
        {
            found.Reverse();
        }

        return found.ToArray();
    }
}
