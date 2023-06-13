using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;

namespace UnbloatDB;

public class IndexerFile : IDisposable
{
    public string Path;
    public FileStream Stream;
    public Index Index;
    public List<string> IndexKeys { get; }
    public List<object> IndexValues { get; }
    public Type ValueType;
    
    private bool disposed;
    private BinaryReader reader;
    private BinaryWriter writer;
    private BinaryWriter preWriter;
    private const int KeyLength = 36;
    
    // TODO: Remove valuetype once code generation is implemented and move to generics/some similar alternative
    public IndexerFile(string fromFile, Type valueType)
    {
        ValueType = valueType;
        Path = fromFile;
        Stream = new FileStream(fromFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        IndexKeys = new List<string>();
        IndexValues= new List<object>();
        Index = new Index(IndexKeys, IndexValues);
        reader = new BinaryReader(Stream, Encoding.Default, true);
        writer = new BinaryWriter(Stream, Encoding.Default, true);
        preWriter = new BinaryWriter(new MemoryStream());
            
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
            var value = ReadBinaryData(reader, length - KeyLength);
            
            Index.Add(new KeyValuePair<string, object>(key, value));
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
            var valueData = GetBinaryData(entry.Value);
            writer.Write((uint) (Encoding.UTF8.GetByteCount(entry.Key) + valueData.Length));
        }

        foreach (var entry in Index)
        {
            writer.Write(Encoding.UTF8.GetBytes(entry.Key));
            
            var valueData = GetBinaryData(entry.Value);
            valueData.Data.CopyTo(writer.BaseStream);
        }
    }

    public void Insert(int index, KeyValuePair<string, object> pair)
    {
        var valueData = GetBinaryData(pair.Value);

        {
            var pairLength = Encoding.UTF8.GetByteCount(pair.Key) + valueData.Length;
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
            valueData.Data.CopyTo(writer.BaseStream);
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
            writer.Write((uint) (Encoding.UTF8.GetByteCount(pair.Key) + valueData.Length));
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
            location += Encoding.UTF8.GetByteCount(IndexKeys[i]);
            // TODO: If we can determine that metadata is not added, we can skip having to calculate the size from GetBinaryData. 
            var valueData = GetBinaryData(IndexValues[i]);
            location += (int) valueData.Length;
        }

        return location;
    }

    /// <summary>
    /// Gets the bytes and written length of the object to be written by a binary writer pre-emptively
    /// </summary>
    /// <returns></returns>
    private (Stream Data, long Length) GetBinaryData(object data)
    {
        preWriter.Seek(0, SeekOrigin.Begin);
        preWriter.BaseStream.SetLength(0);
    TestType:
        switch (data)
        {
            case bool value:
                preWriter.Write(value);
                break;
            case byte value:
                preWriter.Write(value);
                break;
            case byte[] value:
                preWriter.Write(value);
                break;
            case char value:
                preWriter.Write(value);
                break;
            case char[] value:
                preWriter.Write(value);
                break;
            case decimal value:
                preWriter.Write(value);
                break;
            case double value:
                preWriter.Write(value);
                break;
            case Half value:
                preWriter.Write(value);
                break;
            case short value:
                preWriter.Write(value);
                break;
            case int value:
                preWriter.Write(value);
                break;
            case long value:
                preWriter.Write(value);
                break;
            case sbyte value:
                preWriter.Write(value);
                break;
            case float value:
                preWriter.Write(value);
                break;
            case string value:
                preWriter.Write(value);
                break;
            case ushort value:
                preWriter.Write(value);
                break;
            case uint value:
                preWriter.Write(value);
                break;
            case ulong value:
                preWriter.Write(value);
                break;
            default:
                data = ConvertToMarshalType(data);
                goto TestType;
        }

        return (preWriter.BaseStream, preWriter.BaseStream.Position);
    }

    private object ReadBinaryData(BinaryReader binaryReader, int count = 0)
    {
        var primitiveRead = ReadBinaryDataInternal(ValueType, binaryReader, count);
        if (primitiveRead is not null)
        {
            return primitiveRead;
        }

        // All other non-primative but supported types are marshalled from ValueType.
        var type = DetermineMarshalType();
        var marshalledRead = ReadBinaryDataInternal(type, binaryReader, count);
        if (marshalledRead is null)
        {
            throw new InvalidOperationException($"Index file can not process data of value type {ValueType.FullName}");
        }
        
        return ConvertFromMarshalType(marshalledRead);
    }

    private object? ReadBinaryDataInternal(Type type, BinaryReader binaryReader, int count = 0)
    {
        if (type == typeof(bool))
        {
            return binaryReader.ReadBoolean();
        }
        if (type == typeof(byte))
        {
            return binaryReader.ReadByte();
        }
        if (type == typeof(char))
        {
            return binaryReader.ReadBytes(count);
        }
        if (type == typeof(char[]))
        {
            return binaryReader.ReadChars(count);
        }
        if (type == typeof(decimal))
        {
            return binaryReader.ReadDecimal();
        }
        if (type == typeof(double))
        {
            return binaryReader.ReadDouble();
        }
        if (type == typeof(Half))
        {
            return binaryReader.ReadHalf();
        }
        if (type == typeof(short))
        {
            return binaryReader.ReadInt16();
        }
        if (type == typeof(int))
        {
            return binaryReader.ReadInt32();
        }
        if (type == typeof(long))
        {
            return binaryReader.ReadInt64();
        }
        if (type == typeof(sbyte))
        {
            return binaryReader.ReadSByte();
        }
        if (type == typeof(float))
        {
            return binaryReader.ReadSingle();
        }
        if (type == typeof(string))
        {
            return binaryReader.ReadString();
        }
        if (type == typeof(ushort))
        {
            return binaryReader.ReadUInt16();
        }
        if (type == typeof(uint))
        {
            return binaryReader.ReadUInt32();
        }
        if (type == typeof(ulong))
        {
            return binaryReader.ReadUInt64();
        }

        return null;
    }

    // TODO: Add marshal types such as long[], etc and other primitive array types
    private object ConvertToMarshalType(object data)
    {
        if (ValueType.IsEnum)
        {
            return Convert.ChangeType(data, ValueType.GetEnumUnderlyingType()); 
        }

        if (ValueType == typeof(DateTime))
        {
            return ((DateTime)data).ToBinary();
        }

        if (ValueType == typeof(DateTimeOffset))
        {
            var buffer = new byte[sizeof(long) + sizeof(long)];
            BitConverter.GetBytes(((DateTimeOffset)data).UtcDateTime.Ticks).CopyTo(buffer, 0);
            BitConverter.GetBytes(((DateTimeOffset)data).Offset.Ticks).CopyTo(buffer, sizeof(long));
            
            return buffer;
        }
        
        throw new InvalidOperationException($"Index file can not process data of value type {ValueType.FullName}");
    }
    
    private object ConvertFromMarshalType(object data)
    {
        if (ValueType.IsEnum)
        {
            return Enum.ToObject(ValueType, data);
        }

        if (ValueType == typeof(DateTime))
        {
            return DateTime.FromBinary((long)data);
        }

        if (ValueType == typeof(DateTimeKind))
        {
            var dataBytes = (byte[])data;
            var ticksOffsetTicks = Unsafe.As<byte[], long[]>(ref dataBytes);
            return new DateTimeOffset(new DateTime(ticksOffsetTicks[0], DateTimeKind.Utc), TimeSpan.FromTicks(ticksOffsetTicks[1]));
        }

        throw new InvalidOperationException($"Index file can not process data of value type {ValueType.FullName}");
    }
    
    private Type DetermineMarshalType()
    {
        if (ValueType.IsEnum)
        {
            return ValueType.GetEnumUnderlyingType();
        }

        if (ValueType == typeof(DateTime))
        {
            return typeof(long); // dateTime.ToBinary();
        }

        if (ValueType == typeof(DateTimeOffset))
        {
            return typeof(byte[]); // ticks (long), offsetTicks (long) are both converted to 16 byte byte[] buffer 
        }

        throw new InvalidOperationException($"Index file can not process data of value type {ValueType.FullName}");
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