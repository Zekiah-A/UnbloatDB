using System.Collections;
using UnbloatDB.Attributes;

namespace UnbloatDB;

internal sealed class SmartIndexer
{
    private readonly Configuration configuration;
    public Dictionary<string, IndexerFile> Indexers { get; set; }
    
    public SmartIndexer(Configuration config)
    {
        Indexers = new Dictionary<string, IndexerFile>();
        
        configuration = config;
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
            
            OpenIndex(Path.Join(path, property.Name), property.PropertyType);
        }
    }
    
    /// <summary>
    /// Removes each property of a record structure from the record indexer
    /// </summary>
    /// <param name="record">Record being removed from indexer</param>
    public void RemoveFromIndex<T>(RecordStructure<T> record) where T : notnull
    {
        var group = typeof(T).Name;
        var path = Path.Join(configuration.DataDirectory, group, configuration.IndexerDirectory);

        // If there is no indexer directory for this group, then regenerate all indexes for this group.
        if (!Directory.Exists(path))
        {
            throw new Exception("Could not find indexer directory for record group " + group + " in " + path);
        }

        foreach (var property in typeof(T).GetProperties())
        {
            // Do not delete collection types, do not follow database normalisation rules
            if (Attribute.IsDefined(property, typeof(DoNotIndexAttribute)) || IsEnumerable(property.PropertyType))
            {
                continue;
            }
            
            var indexPath = Path.Join(path, property.Name);
            var indexFile = Indexers.GetValueOrDefault(indexPath) ?? OpenIndex(indexPath, property.PropertyType);

            var values = indexFile.IndexValues.ToList(); // We have to copy it so we don't mutate the index (would be fatal)
            var index = values.BinarySearch(property.GetValue(record.Data)!);
            if (index > 0)
            {
                indexFile.Remove(index);
            }
        }
    }

    /// <summary>
    /// Create index data for each of a record's properties, so that it can be searched for by property and located quickly.
    /// </summary>
    /// <param name="record">Record being indexed by smart indexer</param>
    public void AddToIndex<T>(RecordStructure<T> record) where T : notnull
    {
        var group = typeof(T).Name;
        var path = Path.Join(configuration.DataDirectory, group, configuration.IndexerDirectory);

        // If there is no indexer directory for this group, then regenerate all indexes for this group.
        if (!Directory.Exists(path))
        {
            throw new Exception("Could not find indexer directory for record group " + record.GetType().Name + " in " + path);
        }
        
        foreach (var property in typeof(T).GetProperties())
        {
            // Do not index collection types, do not follow database normalisation rules
            if (Attribute.IsDefined(property, typeof(DoNotIndexAttribute)) || IsEnumerable(property.PropertyType))
            {
                continue;
            }

            var indexPath = Path.Join(path, property.Name);
            var indexFile = Indexers.GetValueOrDefault(indexPath) ?? OpenIndex(indexPath, property.PropertyType);
            var propertyValue = property.GetValue(record.Data);

            // Do not index null values for now, way to handle such cases must be found later
            if (propertyValue is null)
            {
                continue;
            }

            if (indexFile.Index.Count > 0)
            {
                var values = indexFile.IndexValues.ToList(); // We have to copy it so we don't mutate the index (would be fatal)
                
                // Figure out where to put in index, so we do not need to sort later by first binary searching for
                // same value, and appending after, if not already in the array, we analyse where it should go.
                // If value is not found, will give bitwise compliment negative number of the next value bigger than what we want,
                // so we can just place the record before that.
                var foundIndex = values.BinarySearch(propertyValue);
                indexFile.Insert(foundIndex >= 0 ? foundIndex : ~foundIndex,
                    new KeyValuePair<string, object>(record.MasterKey, propertyValue));
            }
            else
            {
                // If no previous approaches worked (index length is probably zero/empty), then just add value to end of index.
                indexFile.Insert(indexFile.Index.Count, new KeyValuePair<string, object>(record.MasterKey, propertyValue));
            }
        }
    }

    private static bool IsEnumerable(Type type)
    {
        return type.Name != nameof(String) 
            && type.GetInterface(nameof(IEnumerable)) != null;
    }

    public IndexerFile OpenIndex(string path, Type valueType)
    {
        var indexer = new IndexerFile(path, valueType);
        Indexers.Add(path, indexer);
        return indexer;
    }
}
