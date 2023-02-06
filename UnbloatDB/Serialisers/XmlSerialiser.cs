using System.Xml.Serialization;

namespace UnbloatDB.Serialisers;

public class XmlSerialiser : SerialiserBase
{
    public XmlSerialiser()
    {
        
    }

    public override ValueTask<string> Serialise<T>(T instance)
    {
        var serializer = new XmlSerializer(typeof(T));
        using var serialiseStream = new MemoryStream();
        serializer.Serialize(serialiseStream, instance);
        serialiseStream.Flush();

        using var reader = new StreamReader(serialiseStream);
        return ValueTask.FromResult(reader.ReadToEnd());
    }

    public override ValueTask<T> Deserialise<T>(Stream data)
    {
        var serializer = new XmlSerializer(typeof(T));
        return ValueTask.FromResult((T) serializer.Deserialize(data)!);
    }

    
}