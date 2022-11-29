using UnbloatDB.Tests.Types;

namespace UnbloatDB.Records;

//TODO: Add inter/intrakey data
internal sealed record Song(string File, Genre Genre, string Date);