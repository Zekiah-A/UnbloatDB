using System.Collections;
using OneOf;
using UnbloatDB.Attributes;

namespace UnbloatDB;

internal sealed class SmartIndexer
{
    private readonly Configuration configuration;
    private readonly Database database;
    public Dictionary<string, IndexerFile> Indexers { get; set; }
    
    private static readonly HashSet<Type> NumberTypes = new()
    {
        typeof(int), typeof(double), typeof(decimal),
        typeof(long), typeof(short), typeof(sbyte),
        typeof(byte), typeof(ulong), typeof(ushort),  
        typeof(uint), typeof(float)
    };

    public SmartIndexer(Configuration config, Database db)
    {
        Indexers = new Dictionary<string, IndexerFile>();
        
        configuration = config;
        database = db;
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
            throw new Exception("Could not find indexer directory for record group " + record.GetType().Name + " in " + path);
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
            throw new Exception("Could not find indexer directory for record group " + record.GetType().Name + " in " + path);
        }
        
        foreach (var property in typeof(T).GetProperties())
        {
            // Do not index collection types, do not follow database normalisation rules
            if (Attribute.IsDefined(property, typeof(DoNotIndexAttribute)) || IsEnumerable(property.GetType()))
            {
                continue;
            }

            var indexPath = Path.Join(path, property.Name);
            var indexFile = Indexers.GetValueOrDefault(indexPath) ?? OpenIndex(indexPath);
            var propertyValue = property.GetValue(record.Data);

            // Do not index null values for now, way to handle such cases must be found later
            if (propertyValue is null)
            {
                continue;
            }

            if (indexFile.Index.Count > 0)
            {
                var values = indexFile.Index.Select(keyValue => keyValue.Value).ToArray<object>();
                
                // Figure out where to put in index, so we do not need to sort later by first binary searching for
                // same value, and appending after, if not already in the array, we analyse where it should go.
                // If value is not found, will give bitwise compliment negative number of the next value bigger than what we want,
                // so we can just place the record before that.
                //TODO: For now we keep all format objects as string, soon we will compare numbers properly by ensuring comparison is same type as PropertyValue if possible
                var foundIndex = Array.BinarySearch(values, FormatObject(propertyValue));
                indexFile.Insert(foundIndex >= 0 ? foundIndex : ~foundIndex,
                    new KeyValuePair<string, string>(record.MasterKey, FormatObject(propertyValue).ToString()));
            }
            else
            {
                // If no previous approaches worked (index length is probably zero/empty), then just add value to end of index.
                indexFile.Insert(indexFile.Index.Count, new KeyValuePair<string, string>(record.MasterKey, FormatObject(propertyValue).ToString()));
            }
        }
    }

    private static bool IsEnumerable(Type type)
    {
        return type.Name != nameof(String) 
            && type.GetInterface(nameof(IEnumerable)) != null;
    }
    
    internal static object FormatObject(object value)
    {
        if (value.GetType() is { IsEnum: true })
        {
            return (int) Convert.ChangeType(value, typeof(int));
        }
        
        return NumberTypes.Contains(value.GetType()) ? value : value.ToString()!;
    }
    
    public IndexerFile OpenIndex(string path)
    {
        var indexer = new IndexerFile(path);
        Indexers.Add(path, indexer);
        return indexer;
    }
}
