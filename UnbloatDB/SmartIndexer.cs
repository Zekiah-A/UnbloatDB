namespace UnbloatDB;

public class SmartIndexer
{
    private readonly Config configuration;
    
    public SmartIndexer(Config config)
    {
        configuration = config;
    }

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

    public async Task AddToIndex(RecordStructure record)
    {
        var path = Path.Join(configuration.DataDirectory, record.GetType().Name, "si");
        
        foreach (var property in record.Data.GetType().GetProperties())
        {
            var indexFile = await File.ReadAllLinesAsync(Path.Join(path, property.Name));
            var index = indexFile
                .Select(line => line.Split(" "))
                .Where(keyVal => keyVal.Length == 2)
                .ToList();

            var stringsEnumerable = index.ToList();
            var values = stringsEnumerable.Select(keyValue => keyValue[0]) as string[];

            // Figure out where to put in index, so we do not need to sort later by first binary searching for same value,
            // and appending after, if not alr in the array, we analyse where it should go.
            var foundIndex = Array.BinarySearch(values, property.GetValue(record));
            
            if (foundIndex != -1)
            {
                stringsEnumerable.Insert(foundIndex, property.GetValue(record) as string[]);
            }
            else
            {
                
            }            
            
        }
    }

    public async Task FindFromGroup<T>(string Value, string ByProperty)
    {
        
    }

    public async Task RemoveFromIndex<T>()
    {
        
    }

    public async Task RegenerateAllIndexes<T>()
    {
        //To-do
    }
}