using System.Text;

namespace UnbloatDB;

internal sealed class SmartIndexer
{
    private readonly Config configuration;
    private readonly Dictionary<string, List<string[]>> indexerCache;
    
    public SmartIndexer(Config config)
    {
        configuration = config;
        indexerCache = new Dictionary<string, List<string[]>>();
    }

    /// <summary>
    /// Creates the indexer directory, used for storing record indexes of a group
    /// </summary>
    /// <typeparam name="T">The type of record being stored within this group, so that the appropriate index files can be created.</typeparam>
    public void BuildGroupIndexDirectoryFor<T>()
    {
        var template = typeof(T);
        var path = Path.Join(configuration.DataDirectory, template.Name, "si");
        
        //Create index directory in template's group
        Directory.CreateDirectory(path);
        
        //Populate index directory with appropiate index files
        foreach (var property in template.GetProperties())
        {
            //TODO: Only index primitive types for now
            //if (!property.GetType().IsPrimitive) continue;
            File.Create(Path.Join(path, property.Name));
        }
    }

    /// <summary>
    /// Create index data for each of a record's properties, so that it can be searched for by property and located quickly.
    /// </summary>
    /// <param name="record">Record being indexed by smart indexer</param>
    public async Task AddToIndex<T>(RecordStructure<T> record) where T : notnull
    {
        var group = typeof(T).Name;
        var path = Path.Join(configuration.DataDirectory, group, "si");

        if (!Directory.Exists(path))
        {
            // If there is no indexer directory for this group, then regenerate all indexes for this group.
        }
        
        foreach (var property in typeof(T).GetProperties())
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
            var propertyValue = property.GetValue(record.Data);

            // Figure out where to put in index, so we do not need to sort later by first binary searching for
            // same value, and appending after, if not alr in the array, we analyse where it should go.
            var foundIndex = Array.BinarySearch(values, propertyValue);

            if (foundIndex != -1)
            {
                index.Insert(foundIndex - 1, new[] { propertyValue as string, record.MasterKey });
                await File.WriteAllLinesAsync(indexPath, index.Select(pair => string.Join(" ", pair)));
                continue;
            }

            // If we could not binary search in the index for another key with the same value we can place this before,
            // iterate through values until we find a value that is greater than new, and then jump back by one to give a sorted list.
            if (values.Length > 0)
            {
                var foundAny = false;

                for (var i = 0; i < values.Length; i++)
                {
                    if (values[i].CompareTo(propertyValue) == -1) continue;
                    
                    index.Insert(i - 1, new[] { propertyValue as string, record.MasterKey });
                    foundAny = true;
                }

                if (foundAny)
                {
                    await File.WriteAllLinesAsync(indexPath, index.Select(pair => string.Join(" ", pair)));
                    continue;
                }
            }

            
            // If no previous approaches worked (index length is probably zero/empty), then just add value to end of index.
            index.Add(new[] { propertyValue as string, record.MasterKey });
            await File.WriteAllLinesAsync(indexPath, index.Select(pair => string.Join(" ", pair)));
            // Cache this index file to make subsequent loads faster
            indexerCache.Add(indexPath, index);
        }
    }
    
    public async Task RemoveFromIndex<T>(string masterKey)
    {
        //To-do
    }

    public async Task RegenerateAllIndexes()
    {
        //To-do
    }
}