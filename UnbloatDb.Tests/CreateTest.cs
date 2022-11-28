using UnbloatDB;
using UnbloatDB.Types;

namespace UnbloatDB.Tests;

internal sealed class CreateTest
{
    public int RecordCount { get; }
    public int RandomiseValues { get; }
    public Database Db;
    public Random Random { get; }

    public CreateTest(Database database, int recordCount, bool randomiseValues)
    {
        RecordCount = recordCount;
        RandomiseValues = randomiseValues;
        Db = database;
        random = new Random();
    }

    public async Task<bool> RunTest()
    {
        for (var i = 0; i < RecordCount; i++)
        {
            var guid = Guid.NewGuid();

            var artist = new Artist
            (
                Random.Next(0, 82),
                Variants.Location[Random.Next(0, Variants.Location.Length - 1)],
                (Gender) Random.Next(0, Gender.Length - 1)
            );

            var song = new Song
            (
                guid.ToString(),
                (Genre) Random.Next(0, Genre.Length - 1),
                new DateTime().AddDays(Random.Next(0, 10000))
            );

            Db.CreateRecord(artist);
            Db.CreateRecord(song);
        }
        return true;
    }
}