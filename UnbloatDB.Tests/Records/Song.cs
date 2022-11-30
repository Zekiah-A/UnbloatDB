using UnbloatDB.Attributes;
using UnbloatDB.Tests.Types;

namespace UnbloatDB.Records;

//TODO: Add inter/intrakey data
internal sealed record Song([DoNotIndex] string File, Genre Genre, string Date);