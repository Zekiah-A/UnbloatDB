using UnbloatDB.Serialisers;

namespace UnbloatDB;

public record Config(string DataDirectory, SerialiserBase FileFormat);