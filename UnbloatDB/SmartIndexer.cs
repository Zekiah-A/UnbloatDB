using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnbloatDB.Attributes;

namespace UnbloatDB;

internal sealed class SmartIndexer
{
    private readonly Config configuration;
    public Dictionary<string, IndexerFile> Indexers { get; set; }

    public SmartIndexer(Config config)
    {
        configuration = config;
        Indexers = new Dictionary<string, IndexerFile>();
        
        /*AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            foreach (var indexer in Indexers)
            {
                indexer.Value.Dispose();
            }
        };*/
    }

    /// <summary>
    /// Creates the indexer directory, used for storing record indexes of a group
    /// </summary>
    /// <typeparam name="T">The type of record being stored within this group, so that the appropriate index files can be created.</typeparam>
    public void BuildGroupIndexDirectoryFor<T>()
    {
        var path = Path.Join(configuration.DataDirectory, typeof(T).Name, configuration.IndexerDirectory);
        
        //Create index directory in template's group
        Directory.CreateDirectory(path);
        
        //Populate index directory with appropriate index files
        foreach (var property in typeof(T).GetProperties())
        {
            if (Attribute.IsDefined(property, typeof(DoNotIndexAttribute)))
            {
                continue;
            }
            
            OpenIndex(Path.Join(path, property.Name));
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

            var indexFile = Indexers.GetValueOrDefault(indexPath) ?? OpenIndex(indexPath);
            var keys = indexFile.Index.Select(keyValue => keyValue.Value).ToArray();
            var found = Array.IndexOf(keys, record.MasterKey);

            if (found == -1)
            {
                continue;
            }
            
            indexFile.Remove(found);
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

            /*if (!File.Exists(indexPath))
            {
                throw new Exception("Could not find indexer file for property " + property.Name + " in " + path);
            }*/

            var indexFile = Indexers.GetValueOrDefault(indexPath) ?? OpenIndex(indexPath);
            var values = indexFile.Index.Select(keyValue => keyValue.Key).ToArray<object>();
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
                indexFile.Insert(foundIndex >= 0 ? foundIndex : ~foundIndex, new KeyValuePair<string, string>(FormatObject(propertyValue).ToString()!, record.MasterKey));
                continue;
            }
            
            // If no previous approaches worked (index length is probably zero/empty), then just add value to end of index.
            indexFile.Insert(indexFile.Index.Count, new KeyValuePair<string, string>(FormatObject(propertyValue).ToString()!, record.MasterKey));
        }
    }
    
    internal static object FormatObject<T>(T value) where T : notnull
    {
        return (value.GetType().IsEnum ?
            Convert.ChangeType(value, typeof(int)).ToString() :
            int.TryParse(value.ToString(), out _) ? value.ToString() : value)!;
    }

    public IndexerFile OpenIndex(string path)
    {
        var indexer = new IndexerFile(path);
        Indexers.Add(path, indexer);
        return indexer;
    }
}
