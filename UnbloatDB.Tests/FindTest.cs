using UnbloatDB.Records;
using UnbloatDB.Tests.Types;

namespace UnbloatDB.Tests;

internal sealed class FindTest
{
    private Database Db { get; }

    public FindTest(Database database)
    {
        Db = database;
    }

    public async Task<bool> RunTest()
    {
        var expected = new Artist(10, "Bradford", Gender.Male);

        await Db.CreateRecord(expected);
        
        var results = await Db.FindRecords<Artist, Gender>("Gender", Gender.Male);

        return results.Select(result => result.Data).Contains(expected);
    }
}