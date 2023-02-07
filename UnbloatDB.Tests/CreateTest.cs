using System.Globalization;
using UnbloatDB;
using UnbloatDB.Keys;
using UnbloatDB.Records;
using UnbloatDB.Tests.Types;

namespace UnbloatDB.Tests;

internal sealed class CreateTest
{
    private int RecordCount { get; }
    private Database Db { get; }
    private Random Random { get; }

    public CreateTest(Database database, int recordCount)
    {
        RecordCount = recordCount;
        Db = database;
        Random = new Random();
    }

    public async Task<bool> RunTest()
    {
        for (var i = 0; i < RecordCount; i++)
        {
            var artist = new Artist
            (
                Random.Next(0, 82),
                Variants.Location[Random.Next(0, Variants.Location.Length - 1)],
                (Gender) Random.Next(3)
            );

            var artistKey = await Db.CreateRecord(artist);

            var song = new Song
            (
                Random.Next(0, 1000).ToString(),
                (Genre) Random.Next(4),
                new DateTime().AddDays(Random.Next(0, 10000)).ToString(CultureInfo.InvariantCulture),
                new InterKeyReference<Artist>(artistKey)
            );
            
            await Db.CreateRecord(song);
        }
        
        return true;
    }
}