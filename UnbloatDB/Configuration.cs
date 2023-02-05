using UnbloatDB.Serialisers;

namespace UnbloatDB;

public sealed record Configuration
(
    string DataDirectory,
    SerialiserBase FileFormat,
    bool RemoveDeletedReferences = false,
    string IndexerDirectory = "index"
);