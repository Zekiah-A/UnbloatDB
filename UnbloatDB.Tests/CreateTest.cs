using System.Globalization;
using UnbloatDB;
using UnbloatDB.Records;
using UnbloatDB.Tests.Types;

namespace UnbloatDB.Tests;

internal sealed class CreateTest
{
    public int RecordCount { get; }
    public Database Db;
    public Random Random { get; }

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

            var song = new Song
            (
                Random.Next(0, 1000).ToString(),
                (Genre) Random.Next(4),
                new DateTime().AddDays(Random.Next(0, 10000)).ToString(CultureInfo.InvariantCulture)
            );

            await Db.CreateRecord(artist);
            await Db.CreateRecord(song);
        }
        
        return true;
    }
}