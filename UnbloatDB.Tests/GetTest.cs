using System.Diagnostics;
using UnbloatDB.Records;
using UnbloatDB.Tests.Types;

namespace UnbloatDB.Tests;

internal sealed class GetTest
{
    public Database Db;

    public GetTest(Database database)
    {
        Db = database;
    }

    public async Task<bool> RunTest()
    {
        var expected = new Artist(10, "Bradford", Gender.Male);

        var masterKey = await Db.CreateRecord(expected);

        var result = await Db.GetRecord<Artist>(masterKey);
        
        return result is not null && result.Data.Equals(expected);
    }
}