using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnbloatDB.Attributes;
using UnbloatDB.Keys;

namespace UnbloatDB;

internal sealed class SmartIndexer
{
    private readonly Config configuration;
    private readonly Database database;
    public Dictionary<string, IndexerFile> Indexers { get; set; }

    public SmartIndexer(Config config, Database db)
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
            // Do not index collection types, do not follow database normalisation rules
            if (Attribute.IsDefined(property, typeof(DoNotIndexAttribute)) || IsEnumerable(property.GetType()))
            {
                continue;
            }

            var indexPath = Path.Join(path, property.Name);
            var indexFile = Indexers.GetValueOrDefault(indexPath) ?? OpenIndex(indexPath);
            var values = indexFile.Index.Select(keyValue => keyValue.Key).ToArray<object>();
            var propertyValue = property.GetValue(record.Data);

            // Do not index null values for now, way to handle such cases must be found later
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

            // If it's a key reference, we update the "references" field of that record
            if (property.GetType().GetGenericTypeDefinition() == typeof(KeyReferenceBase<>))
            {
                // Now we know the group of the record we are targeting, and key, we can update the target records'
                // references to include a reference back to this.
                var targetType = property.GetType().GetGenericArguments()[0];
                
                // TODO: Try to avoid dynamic, is always typeof RecordStructure<targetType>
                var targetRecord = (dynamic) typeof(Database).GetMethod(nameof(Database.GetRecord))!
                    .Invoke(database, new object?[] {propertyValue.ToString()})!;

                // If we are in the same group (the target reference and record have the same generic type), then we use
                // an IntraKey, otherwise we can utilise an Interkey.
                if (targetType == targetRecord.GetType().GetGenericArguments()[0])
                {
                    var selfReference = new PropertyIntraKeyReference<T>(property.Name, record.MasterKey);
                    targetRecord.KeyReferencors.Add(selfReference);
                }
                else
                {
                    var selfReference = new PropertyInterKeyReference<T>(property.Name, record.MasterKey, nameof(T));
                    targetRecord.KeyReferencors.Add(selfReference);
                }
                
                // Update the target record with the reference to this.
                typeof(Database).GetMethod(nameof(Database.UpdateRecord))!.Invoke(database, targetRecord);
            }
        }
    }

    private static bool IsEnumerable(Type type)
    {
        return type.Name != nameof(String) 
                && type.GetInterface(nameof(IEnumerable)) != null;
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
