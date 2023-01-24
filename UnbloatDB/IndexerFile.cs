using System.Text;

namespace UnbloatDB;

public class IndexerFile
{
    public FileStream Stream;
    public string Path;
    public int HeaderLength
    {
        get
        {
            // We include the size (uint) of the header length at the file start too
            var count = 4;

            foreach (var pair in Index)
            {
                count += pair.Key.Length * 4;
                count += ((string) SmartIndexer.FormatObject(pair.Value)).Length * 4;
            }

            return count;
        }
    }
    
    public List<KeyValuePair<string, string>> Index;

    public IndexerFile(string fromFile)
    {
        Path = fromFile;
        Stream = new FileStream(fromFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

        if (Stream.Length == 0)
        {
            Create();
        }
        
        
        // Populate this class with the data from the indexer file stream
        using var reader = new BinaryReader(Stream);
        reader.BaseStream.Seek(0, SeekOrigin.Begin);

        var headerLength = reader.ReadUInt32();
        var lengths = new List<int>();
        var index = new List<KeyValuePair<string, string>>();

        for (var i = 0; i < headerLength - 4; i += 4)
        {
            lengths.Add((int) reader.ReadUInt32());
        }
        
        foreach (var length in lengths)
        {
            var key = reader.ReadString();
            var value = reader.ReadString();
            
            index.Add(new KeyValuePair<string, string>(key, value));
        }

        Index = index;
    }
        
    private void Create()
    {
        using var writer = new BinaryWriter(Stream);
        writer.Seek(0, SeekOrigin.Begin);
        writer.Write((uint) HeaderLength);
        
        // Uint = 4 bytes, write each key value pair length as a uint  
        foreach (var entry in Index)
        {
            writer.Write(entry.Key.Length * 4 + entry.Value.Length * 4);
        }

        foreach (var entry in Index)
        {
            writer.Write(Encoding.UTF8.GetBytes(entry.Key));
            writer.Write(Encoding.UTF8.GetBytes(entry.Value));
        }
    }

    public void Insert(int index, KeyValuePair<string, string> pair)
    {
        using var writer = new BinaryWriter(Stream);

        // First write the record key - value to the right location in the file
        writer.Seek(GetElementLocation(index), SeekOrigin.Begin);
        writer.Write(Encoding.UTF8.GetBytes(pair.Key));
        writer.Write(Encoding.UTF8.GetBytes(pair.Value));
        
        // Next, jump back up and append our new changes to the header of the file
        writer.Seek(GetHeaderLocation(index), SeekOrigin.Begin);
        writer.Write(pair.Key.Length * 4 + pair.Value.Length * 4);

        Index.Insert(index, pair);
    }

    public void Remove(int index)
    {
        //TODO: Use Buffer.BlockCopy to avoid constructing a new indexer
        
        // First write the record key - value to the right location in the file
        //GetElementLocation(index), SeekOrigin.Begin

        // Next, jump back up and append our new changes to the header of the file
        //GetHeaderLocation(index), SeekOrigin.Begin

        Index.RemoveAt(index);
        Create();
    }
    
    private int GetElementLocation(int elementIndex)
    {
        var location = 0;
        location += HeaderLength;
        
        for (var i = 0; i < elementIndex; i++)
        {
            location += Index[i].Key.Length * 4;
            location += Index[i].Value.Length * 4;
        }

        return location;
    }

    private int GetHeaderLocation(int headerIndex)
    {
        var location = 4;
        location += headerIndex * 4;
        return location;
    }
}