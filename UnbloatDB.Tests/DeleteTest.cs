using UnbloatDB.Records;
using UnbloatDB.Tests.Types;

namespace UnbloatDB.Tests;

internal sealed class DeleteTest
{
    private Database Db { get; }

    public DeleteTest(Database database)
    {
        Db = database;
    }

    public async Task<bool> RunTest()
    {
        var toDelete = new Artist(25, "Bradford", Gender.Male);
        var toDeleteKey = await Db.CreateRecord(toDelete);

        await Db.DeleteRecord<Artist>(toDeleteKey);

        var result = await Db.GetRecord<Artist>(toDeleteKey);
        
        return result is null;
    }
}