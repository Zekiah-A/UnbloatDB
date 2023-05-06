using System.Runtime.CompilerServices;
using System.Text;

namespace UnbloatDB;

public class IndexerFile: IDisposable
{
    public string Path;
    public FileStream Stream;
    public List<KeyValuePair<string, string>> Index { get; }
    
    private bool disposed;
    private BinaryReader reader;
    private BinaryWriter writer;
    private const int KeyLength = 36;
    
    public IndexerFile(string fromFile)
    {
        Path = fromFile;
        Stream = new FileStream(fromFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        Index = new List<KeyValuePair<string, string>>();
        reader = new BinaryReader(Stream, Encoding.Default, true);
        writer = new BinaryWriter(Stream, Encoding.Default, true);

        if (Stream.Length == 0)
        {
            Create();
        }
        
        // Populate this class with the data from the indexer file stream
        reader.BaseStream.Seek(0, SeekOrigin.Begin);

        var headerLength = reader.ReadUInt32();
        var lengths = new List<int>();

        // GetHeaderLength() includes length of total header length (first byte)
        for (var i = 4; i < headerLength; i += 4)
        {
            lengths.Add((int) reader.ReadUInt32());
        }
        
        // Now that we have read past header, we should be in the main record body
        foreach (var length in lengths)
        {
            var key = Encoding.UTF8.GetString(reader.ReadBytes(KeyLength));
            var value = Encoding.UTF8.GetString(reader.ReadBytes(length - KeyLength));
            
            Index.Add(new KeyValuePair<string, string>(key, value));
        }
    }
        
    private void Create()
    {
        Stream.SetLength(0);
        writer.Seek(0, SeekOrigin.Begin);
        writer.Write(BitConverter.GetBytes((uint) GetHeaderLength()), 0, sizeof(uint));
        
        // Uint = 4 bytes, write each key value pair length as a uint  
        foreach (var entry in Index)
        {
            writer.Write((uint) (Encoding.UTF8.GetByteCount(entry.Key) + Encoding.UTF8.GetByteCount(entry.Value)));
        }

        foreach (var entry in Index)
        {
            writer.Write(Encoding.UTF8.GetBytes(entry.Key));
            writer.Write(Encoding.UTF8.GetBytes(entry.Value));
        }
    }

    public void Insert(int index, KeyValuePair<string, string> pair)
    {
        {
            var pairLength = Encoding.UTF8.GetByteCount(pair.Key) + Encoding.UTF8.GetByteCount(pair.Value);
            // We need to make space to insert this new record, so shift over everything after this by the size of the
            // record we are to add
            Stream.Seek(GetElementLocation(index), SeekOrigin.Begin);
            var proceeding = new MemoryStream(reader.ReadBytes((int) (Stream.Length - Stream.Position)));
            Stream.Seek(GetElementLocation(index) + pairLength, SeekOrigin.Begin);
            proceeding.CopyTo(Stream);
            proceeding.Flush();

            // First write the record key - value to the right location in the file
            writer.Seek(GetElementLocation(index), SeekOrigin.Begin);
            writer.Write(Encoding.UTF8.GetBytes(pair.Key));
            writer.Write(Encoding.UTF8.GetBytes(pair.Value));
            writer.Flush();
        }
        {
            // Next, jump back up and shift everything over by 4 bytes to make space for new header length entry.
            Stream.Seek(GetHeaderLocation(index), SeekOrigin.Begin);
            var proceeding = new MemoryStream(reader.ReadBytes((int) (Stream.Length - Stream.Position)));
            Stream.Seek(GetHeaderLocation(index) + sizeof(uint), SeekOrigin.Begin);
            proceeding.CopyTo(Stream);
            proceeding.Flush();
            
            Stream.SetLength(Stream.Position);

            // Append this record's length to our newly made space in the header of in the file
            writer.Seek(GetHeaderLocation(index), SeekOrigin.Begin);
            writer.Write((uint) (Encoding.UTF8.GetByteCount(pair.Key) + Encoding.UTF8.GetByteCount(pair.Value)));
            writer.Flush();
            
            // Update header length (+4 because we just added another uint32 record length to header)
            writer.Seek(0, SeekOrigin.Begin); 
            writer.Write(BitConverter.GetBytes((uint) GetHeaderLength() + sizeof(uint)), 0, sizeof(uint));
            writer.Flush();
        }
        Index.Insert(index, pair);
    }

    public void Remove(int index)
    {
        //First get length of this record so we know how much to cut out
        reader.BaseStream.Seek(GetHeaderLocation(index), SeekOrigin.Begin);
        var recordLength = reader.ReadUInt32();

        // Next we copy everything following record location
        reader.BaseStream.Seek(GetElementLocation(index) + recordLength, SeekOrigin.Begin);
        var proceeding = new MemoryStream(reader.ReadBytes((int) (reader.BaseStream.Length - reader.BaseStream.Position)));
        
        // Then jump back to just before the record to insert preceding before it, cutting the duplicated data at the end
        reader.BaseStream.Seek(GetElementLocation(index), SeekOrigin.Begin);
        proceeding.CopyTo(Stream);
        proceeding.Flush();
        reader.BaseStream.SetLength(reader.BaseStream.Position);
    
        // Next, jump back up and shift over this record length from the header of the indexer
        reader.BaseStream.Seek(GetHeaderLocation(index) + 4, SeekOrigin.Begin);
        proceeding = new MemoryStream(reader.ReadBytes((int) (reader.BaseStream.Length - reader.BaseStream.Position)));
        reader.BaseStream.SetLength(reader.BaseStream.Position);

        reader.BaseStream.Seek(GetHeaderLocation(index), SeekOrigin.Begin);
        proceeding.CopyTo(Stream);
        proceeding.Flush();
        
        // Update header length, (-4) because we just removed a 4 byte record length from the header 
        writer.Seek(0, SeekOrigin.Begin);
        writer.Write(BitConverter.GetBytes((uint) GetHeaderLength() - sizeof(uint)), 0, sizeof(uint));
        writer.Flush();
        
        Index.RemoveAt(index);
    }
    
    private int GetElementLocation(int elementIndex)
    {
        var location = 0;
        location += GetHeaderLength();
        
        for (var i = 0; i < elementIndex; i++)
        {
            location += Encoding.UTF8.GetByteCount(Index[i].Key);
            location += Encoding.UTF8.GetByteCount(Index[i].Value);
        }

        return location;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetHeaderLocation(int headerIndex)
    {
        return headerIndex * sizeof(uint) + sizeof(uint);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetHeaderLength()
    {
        return sizeof(uint) + Index.Count * sizeof(uint);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            Stream.Flush();
            Stream.Dispose();
        }

        disposed = true;
    }
}