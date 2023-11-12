using UnbloatDB.Attributes;
using UnbloatDB.Shared;
using UnbloatDB.Tests.Types;

namespace UnbloatDB.Records;

[Group(GroupName = nameof(Artist))]
internal sealed partial record Artist(
    int Age,
    string Location,
    Gender Gender);