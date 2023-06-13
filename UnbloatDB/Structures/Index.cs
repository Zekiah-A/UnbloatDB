using System.Collections;

namespace UnbloatDB;

public class Index : IEnumerable<KeyValuePair<string, object>>
{
    private readonly List<string> indexerKeys;
    private readonly List<object> indexerValues;
    
    public int Count => indexerKeys.Count;

    public Index(List<string> keys, List<object> values)
    {
        indexerKeys = keys;
        indexerValues = values;
    }
    
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        return indexerKeys.Select((key, index)
            => new KeyValuePair<string, object>(key, indexerValues[index])).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    public void Add(string key, object value)
    {
        indexerKeys.Add(key);
        indexerValues.Add(value);
    }

    public void Add(KeyValuePair<string, object> pair)
    {
        indexerKeys.Add(pair.Key);
        indexerValues.Add(pair.Value);
    }

    public void RemoveAt(int index)
    {
        indexerKeys.RemoveAt(index);
        indexerValues.RemoveAt(index);
    }

    public void Insert(int index, KeyValuePair<string, object> pair)
    {
        indexerKeys.Insert(index, pair.Key);
        indexerValues.Insert(index, pair.Value);
    }
}