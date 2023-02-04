using System.Collections;
using UnbloatDB.Keys;

namespace UnbloatDB;

public sealed record RecordStructure<T>(string MasterKey, T Data) where T : notnull
{
    public List<IKeyReferenceBase> KeyReferencers = new();
}