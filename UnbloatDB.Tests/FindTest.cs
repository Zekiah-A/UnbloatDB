using UnbloatDB.Records;
using UnbloatDB.Tests.Types;

namespace UnbloatDB.Tests;

internal sealed class FindTest
{
    public Database Db;

    public FindTest(Database database)
    {
        Db = database;
    }

    public async Task<bool> RunTest()
    {
        var expected = new Artist(10, "Bradford", Gender.Male);

        var masterKey = await Db.CreateRecord(expected);

        var results = await Db.FindRecords<Artist>("Location", "Bradford");

        return results.Select(result => result.Data).Contains(expected);
    }
}