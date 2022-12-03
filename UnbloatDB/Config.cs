using UnbloatDB.Serialisers;

namespace UnbloatDB;

public sealed record Config(string DataDirectory, SerialiserBase FileFormat, string IndexerDirectory = "index");