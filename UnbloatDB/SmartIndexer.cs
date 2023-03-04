using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using OneOf;
using UnbloatDB.Attributes;
using UnbloatDB.Keys;
using BindingFlags = System.Reflection.BindingFlags;

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
            throw new Exception("Could not find indexer directory for record group " + nameof(record.GetType) + " in " + path);
        }
        
        // Remove/edit referencers to notify them that record has been deleted, a reverse mirror of what happens in the add method
        foreach (var referencer in record.Referencers)
        {
            var referencerType = referencer.GetType().GetGenericTypeDefinition();
            
            // RecordStructure<>
            var referenceRecord = await typeof(Database)
                .GetMethod(nameof(Database.GetRecord))!
                .MakeGenericMethod(referencerType)
                .InvokeAsync(database, (referencer as KeyReferenceBase<object>)!.RecordKey);

            // TODO: Delete this from referencers
            
            await typeof(Database)
                .GetMethod(
                    nameof(Database.UpdateRecord),
                    1,
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(RecordStructure<>) }, //new[] { Type.MakeGenericSignatureType(typeof(RecordStructure<>)), Type.MakeGenericMethodParameter(0) },
                    null
                )!
                .MakeGenericMethod(referencerType)
                .InvokeAsync(database, referenceRecord);
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
            var propertyValue = property.GetValue(record.Data);

            // Do not index null values for now, way to handle such cases must be found later
            if (propertyValue is null)
            {
                continue;
            }

            if (indexFile.Index.Count > 0)
            {
                var values = indexFile.Index.Select(keyValue => keyValue.Key).ToArray<object>();
                
                // Figure out where to put in index, so we do not need to sort later by first binary searching for
                // same value, and appending after, if not already in the array, we analyse where it should go.
                // If value is not found, will give bitwise compliment negative number of the next value bigger than what we want,
                // so we can just place the record before that.
                var foundIndex = Array.BinarySearch(values, FormatObject(propertyValue));
                indexFile.Insert(foundIndex >= 0 ? foundIndex : ~foundIndex,
                    new KeyValuePair<string, string>(record.MasterKey, FormatObject(propertyValue).ToString()!));
            }
            else
            {
                // If no previous approaches worked (index length is probably zero/empty), then just add value to end of index.
                indexFile.Insert(indexFile.Index.Count, new KeyValuePair<string, string>(record.MasterKey, FormatObject(propertyValue).ToString()!));
            }
            
            // If it's a key reference, we update the "references" field of that record
            // TODO: Weird bug, checking equality always returns false, so for now we just do a comparison on the string names.
            // TODO: https://stackoverflow.com/questions/59213561/why-does-type-equals-always-returns-false-with-generic-types
            if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition().BaseType!.Name == typeof(KeyReferenceBase<>).Name)
            {
                // Now we know the group of the record we are targeting, and key, we can update the target records'
                // references to include a reference back to this.
                var targetType = property.PropertyType.GetGenericArguments()[0];

                // Get the record key of the target so that we can get their record from the DB.
                // propertyValue: IntraKeyReference, targetKey: string. 
                var targetKey = propertyValue.GetType().GetProperty("RecordKey")!.GetValue(propertyValue)!;

                // We magically create a generic method at runtime for handling this target type and retrieve the
                // referenced database record.
                var targetRecord = (await typeof(Database).GetMethod(nameof(Database.GetRecord))!
                    .MakeGenericMethod(targetType).InvokeAsync(database, targetKey))!;

                // If we are in the same group (the target reference and record have the same generic type), then we use
                // an IntraKey, otherwise we can utilise an Interkey.
                var referencersProperty = targetRecord.GetType().GetProperty("Referencers")!;
                if (targetType == typeof(T))
                {
                    // This reflection abomination will attempt to call the list add method on the record to add this referencer.
                    var selfReference = new PropertyIntraKeyReference<T>(property.Name, record.MasterKey);
                    referencersProperty.PropertyType.GetMethod("Add")!
                        .Invoke(referencersProperty.GetValue(targetRecord), new object[] { selfReference });
                }
                else
                {
                    var selfReference = new PropertyInterKeyReference<T>(property.Name, record.MasterKey, typeof(T).Name);
                    referencersProperty.PropertyType.GetMethod("Add")!
                        .Invoke(referencersProperty.GetValue(targetRecord), new object[] { selfReference });
                }
                
                // Update the target record with the reference to this.
                await typeof(Database)
                    .GetMethod(
                        nameof(Database.UpdateRecord),
                        1,
                        BindingFlags.Public | BindingFlags.Instance,
                        null,
                        new[] { typeof(RecordStructure<>) }, //new[] { Type.MakeGenericSignatureType(typeof(RecordStructure<>)), Type.MakeGenericMethodParameter(0) },
                        null
                    )!
                    .MakeGenericMethod(targetType)
                    .InvokeAsync(database, targetRecord);
            }
        }
    }

    private static bool IsEnumerable(Type type)
    {
        return type.Name != nameof(String) 
            && type.GetInterface(nameof(IEnumerable)) != null;
    }
    
    internal static OneOf<string, int, T> FormatObject<T>(T value) where T : notnull
    {
        if (typeof(T).BaseType is { IsEnum: true })
        {
            return (int) Convert.ChangeType(value, typeof(int));
        }
        
        return NumberTypes.Contains(typeof(T)) ? value : value.ToString()!;
    }
    
    public IndexerFile OpenIndex(string path)
    {
        var indexer = new IndexerFile(path);
        Indexers.Add(path, indexer);
        return indexer;
    }
}
