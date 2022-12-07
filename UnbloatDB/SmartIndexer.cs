using System.Reflection;
using System.Text;
using UnbloatDB.Attributes;

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

            var index = await ReadIndex(path);
            var values = index.Select(keyValue => keyValue[0]).ToArray();
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
                var foundIndex = Array.BinarySearch(values, propertyValue.GetType().IsEnum ? ((int) propertyValue).ToString());

                if (foundIndex > 0)
                {
                    index.Insert(foundIndex, new[] { propertyValue.GetType().IsEnum ? ((int) propertyValue).ToString() : propertyValue.ToString(), record.MasterKey }!);
                    await File.WriteAllTextAsync(indexPath, BuildIndex(in index));
                    continue;
                }

                // If we could not binary search in the index for another key with the same value we can place this before,
                // iterate through values until we find a value that is greater than new, and then jump back by one to give a sorted list.
                for (var i = 0; i < values.Length; i++)
                {
                    IComparable? convertedValue;

                    if (propertyValue.GetType().IsEnum)
                    {
                        convertedValue = (int) Enum.Parse(propertyValue.GetType(), values[i]);
                        propertyValue = (int) propertyValue;
                    }
                    else
                    {
                        convertedValue = Convert.ChangeType(values[i], propertyValue.GetType()) as IComparable;
                    }
                    
                    if (convertedValue is null || convertedValue.CompareTo(propertyValue) == -1)
                    {
                        continue;
                    }
                    
                    index.Insert(i, new[] { propertyValue.GetType().IsEnum ? ((int) propertyValue).ToString() : propertyValue.ToString(), record.MasterKey }!);
                    await File.WriteAllTextAsync(indexPath, BuildIndex(in index));
                    break;
                }
                
                continue;
            }

            // If no previous approaches worked (index length is probably zero/empty), then just add value to end of index.
            index.Add(new[] { propertyValue.GetType().IsEnum ? ((int) propertyValue).ToString() : propertyValue.ToString(), record.MasterKey }!);
            await File.WriteAllTextAsync(indexPath, BuildIndex(in index));
        }
    }

    // TODO: Benchmark performance of BuildIndex over await File.WriteAllLinesAsync(indexPath, index.Select(pair => string.Join(" ", pair)));
    private static string BuildIndex(in List<string[]> index)
    {
        var builder = new StringBuilder();
        foreach (var pair in index)
        {
            builder.AppendJoin(" ", pair);
            builder.Append(Environment.NewLine);
        }

        return builder.ToString();
    }
    
    public async Task RemoveFromIndex<T>(string masterKey)
    {
        //To-do
    }

    public async Task RegenerateAllIndexes()
    {
        //To-do
    }

    internal static async Task<List<string>> ReadIndex(string path)
    {
        return await File.ReadAllLinesAsync(path)
            .Select(line =>
            {
                var last = line.LastIndexOf(" ", StringComparison.Ordinal);
                return last == -1 ? Array.Empty<string>() : new[] { line[..last], line[(last + 1)..] };
            })
            .ToList();
    }
}
