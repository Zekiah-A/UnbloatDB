using UnbloatDB.Attributes;
using UnbloatDB.Keys;
using UnbloatDB.Tests.Types;

namespace UnbloatDB.Records;

//TODO: Add inter/intrakey data
internal sealed record Song([property: DoNotIndex] string File, Genre Genre, string Date, IntraKeyReference<Artist> Artist);