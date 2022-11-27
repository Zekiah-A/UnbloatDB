namespace UnbloatDB;

internal sealed class SmartIndexer
{
    private readonly Config configuration;
    
    public SmartIndexer(Config config)
    {
        configuration = config;
    }

    /// <summary>
    /// Creates the indexer directory, used for storing record indexes of a group
    /// </summary>
    /// <typeparam name="T">The type of record being stored within this group, so that the appropriate index files can be created.</typeparam>
    public async Task BuildGroupIndexDirectoryFor<T>()
    {
        var template = typeof(T);
        var path = Path.Join(configuration.DataDirectory, template.Name, "si");
        
        //Create index directory in template's group
        Directory.CreateDirectory(path);
        
        //Populate index directory with appropiate index files
        foreach (var property in template.GetProperties())
        {
            //TODO: Only index primitive types for now
            if (!property.GetType().IsPrimitive) continue;
            await File.WriteAllTextAsync(Path.Join(path, property.Name), "");
        }
    }

    /// <summary>
    /// Create index data for each of a record's properties, so that it can be searched for by property and located quickly.
    /// </summary>
    /// <param name="record">Record being indexed by smart indexer</param>
    public async Task AddToIndex(RecordStructure record)
    {
        var group = nameof(record.Data);
        var path = Path.Join(configuration.DataDirectory, group, "si");

        if (!Directory.Exists(path))
        {
            // If there is no indexer directory for this group, then regenerate all indexes for this group.
        }
        
        foreach (var property in record.Data.GetType().GetProperties())
        {
            var indexPath = Path.Join(path, property.Name);

            if (!File.Exists(indexPath))
            {
                // If there is no indexer for this specific property, then regenerate indexes for just this property.
            }
              
            var indexFile = await File.ReadAllLinesAsync(indexPath);
            var index = indexFile
                .Select(line => line.Split(" "))
                .Where(keyVal => keyVal.Length == 2)
                .ToList();

            var values = index.Select(keyValue => keyValue[0]) as string[];

            // Figure out where to put in index, so we do not need to sort later by first binary searching for same value,
            // and appending after, if not alr in the array, we analyse where it should go.
            var foundIndex = Array.BinarySearch(values, property.GetValue(record));

            if (foundIndex != -1)
            {
                index.Insert(foundIndex, property.GetValue(record) as string[]);
            }
            else
            {
                
            }
            
        }
    }
    
    public async Task RemoveFromIndex<T>()
    {
        //To-do
    }

    public async Task RegenerateAllIndexes()
    {
        //To-do
    }
}