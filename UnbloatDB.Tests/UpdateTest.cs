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
        
        var artistKey = await Db.CreateRecord(artist);

        var artistOriginal = await Db.GetRecord<Artist>(artistKey);
        if (artistOriginal is null)
        {
            return false;
        }
        
        var updatedArtist = artist with { Age = 25 };
        await Db.UpdateRecord(artistOriginal with { Data = updatedArtist });
        
        var result = await Db.GetRecord<Artist>(artistKey);
        return !result.Equals(artistOriginal);
    }
}