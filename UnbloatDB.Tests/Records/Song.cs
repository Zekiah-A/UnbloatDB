using UnbloatDB.Attributes;
using UnbloatDB.Shared;
using UnbloatDB.Tests.Types;

namespace UnbloatDB.Records;

[Group(GroupName = nameof(Song))]
internal sealed partial record Song(
    [field: DoNotIndex] string File,
    Genre Genre,
    string Date,
    [field: KeyReference(ReferenceTargetType = typeof(Artist))] string Author);