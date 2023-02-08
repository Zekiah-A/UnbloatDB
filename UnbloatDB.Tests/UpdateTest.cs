using UnbloatDB.Records;
using UnbloatDB.Tests.Types;

namespace UnbloatDB.Tests;

internal sealed class UpdateTest
{
    private Database Db { get; }

    public UpdateTest(Database database)
    {
        Db = database;
    }

    public async Task<bool> RunTest()
    {
        var artist = new Artist(25, "Bradford", Gender.Male);
        var artistKey = await Db.CreateRecord<Artist>(artist);

        var artistOriginal = await Db.GetRecord<Artist>(artistKey);
        await Db.UpdateRecord(artistOriginal with { Data.Age = 26 });
        
        var result = await Db.GetRecord<Artist>(artistKey);
        return !result.Equals(artistOriginal);
    }
}