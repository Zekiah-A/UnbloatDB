using System.Reflection;
using System.Text;
using UnbloatDB.Attributes;

namespace UnbloatDB;

internal sealed class SmartIndexer
{
    private readonly Config configuration;
    private Dictionary<string, IndexerFile> indexers;

    public SmartIndexer(Config config)
    {
        configuration = config;
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            foreach (var indexer in indexers)
            {
                indexer.Dispose();
            }
        };
    }

    /// <summary>
    /// Creates the indexer directory, used for storing record indexes of a group
    /// </summary>
    /// <typeparam name="T">The type of record being stored within this group, so that the appropriate index files can be created.</typeparam>
    public async Task BuildGroupIndexDirectoryFor<T>()
    {
        var path = Path.Join(configuration.DataDirectory, typeof(T).Name, configuration.IndexerDirectory);
        
        //Create index directory in template's group
        Directory.CreateDirectory(path);
        
        //Populate index directory with appropiate index files
        foreach (var property in typeof(T).GetProperties())
        {
            //TODO: Make this only index primitive types for now
            if (Attribute.IsDefined(property, typeof(DoNotIndexAttribute)))
            {
                continue;
            }

            await File.WriteAllTextAsync(Path.Join(path, property.Name), "");
        }
    }
    
    public async Task RegenerateAllIndexes()
    {
        //To-do
    }

    /// <summary>
    /// Removes each property of a record structure from the record indexer
    /// </summary>
    /// <param name="record">Record being removed from indexer</param>
    public async Task RemoveFromIndex<T>(RecordStructure<T> record) where T : notnull
    {
        var group = typeof(T).Name;
        var path = Path.Join(configuration.DataDirectory, group, configuration.IndexerDirectory);

        // If there is no indexer directory for this group, then regenerate all indexes for this group.
        if (!Directory.Exists(path))
        {
            throw new Exception("Could not find indexer directory for record group " + nameof(record.GetType) + " in " + path);
        }
        
        foreach (var property in typeof(T).GetProperties())
        {
            if (Attribute.IsDefined(property, typeof(DoNotIndexAttribute)))
            {
                continue;
            }
            
            var indexPath = Path.Join(path, property.Name);

            if (!File.Exists(indexPath))
            {
                throw new Exception("Could not find indexer file for property " + property.Name + " in " + path);
            }

            var index = await ReadIndex(indexPath);
            var keys = index.Select(keyValue => keyValue.Value).ToArray();
            var found = Array.IndexOf(keys, record.MasterKey);

            if (found == -1)
            {
                continue;
            }
            
            index.RemoveAt(found);
            await File.WriteAllTextAsync(indexPath, BuildIndex(in index));
        }
    }

    /// <summary>
    /// Create index data for each of a record's properties, so that it can be searched for by property and located quickly.
    /// </summary>
    /// <param name="record">Record being indexed by smart indexer</param>
    public async Task AddToIndex<T>(RecordStructure<T> record) where T : notnull
    {
        var group = typeof(T).Name;
        var path = Path.Join(configuration.DataDirectory, group, configuration.IndexerDirectory);

        // If there is no indexer directory for this group, then regenerate all indexes for this group.
        if (!Directory.Exists(path))
        {
            throw new Exception("Could not find indexer directory for record group " + nameof(record.GetType) + " in " + path);
        }
        
        foreach (var property in typeof(T).GetProperties())
        {
            if (Attribute.IsDefined(property, typeof(DoNotIndexAttribute)))
            {
                continue;
            }
            
            var indexPath = Path.Join(path, property.Name);

            if (!File.Exists(indexPath))
            {
                throw new Exception("Could not find indexer file for property " + property.Name + " in " + path);
            }

            var index = await ReadIndex(indexPath);
            var values = index.Select(keyValue => keyValue.Key).ToArray<object>();
            var propertyValue = property.GetValue(record.Data);

            //TODO: Do not index null values for now, way to handle such cases must be found later
            if (propertyValue is null)
            {
                continue;
            }

            if (values is { Length: > 0 })
            {
                // Figure out where to put in index, so we do not need to sort later by first binary searching for
                // same value, and appending after, if not already in the array, we analyse where it should go.
                // If value is not found, will give bitwise compliment negative number of the next value bigger than what we want,
                // so we can just place the record before that.
                var foundIndex = Array.BinarySearch(values, FormatObject(propertyValue));
                index.Insert(foundIndex >= 0 ? foundIndex : ~foundIndex, new KeyValuePair<string, string>(FormatObject(propertyValue).ToString()!, record.MasterKey));
                await File.WriteAllTextAsync(indexPath, BuildIndex(in index));
                continue;
            }
            
            // If no previous approaches worked (index length is probably zero/empty), then just add value to end of index.
            index.Add(new KeyValuePair<string, string>(FormatObject(propertyValue).ToString()!, record.MasterKey));
            await File.WriteAllTextAsync(indexPath, BuildIndex(in index));
        }
    }
    
    private static string BuildIndex(in List<KeyValuePair<string, string>> index)
    {
        var builder = new StringBuilder();
        foreach (var pair in index)
        {
            builder.Append(pair.Key);
            builder.Append(' ');
            builder.Append(pair.Value);
            builder.Append(Environment.NewLine);
        }

        return builder.ToString();
    }

    internal static object FormatObject<T>(T value) where T : notnull
    {
        return (value.GetType().IsEnum ?
            Convert.ChangeType(value, typeof(int)).ToString() :
            int.TryParse(value.ToString(), out _) ? value.ToString() : value)!;
    }
    
    internal static async Task<List<KeyValuePair<string, string>>> ReadIndex(string path)
    {
        var text = await File.ReadAllLinesAsync(path);
        
        var index = new List<KeyValuePair<string, string>>();

        foreach (var line in text)
        {
            var separator = line.IndexOf(' ', StringComparison.Ordinal);
            index.Add(new KeyValuePair<string, string>(line[..separator], line[(separator + 1)..]));
        }

        return index;
    }
}
