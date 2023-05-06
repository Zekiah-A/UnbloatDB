using UnbloatDB.Records;
using UnbloatDB.Tests.Types;

namespace UnbloatDB.Tests;

public class ReferenceTest
{
    private Database Db { get; }

    public ReferenceTest(Database database)
    {
        Db = database;
    }

    public async Task<bool> RunTest()
    {
        // Create artist and song records
        var expectedArtist = new Artist(24, "London", Gender.Female);
        var artistKey = await Db.CreateRecord(expectedArtist);
        
        var expectedSong = new Song("/home/tea/my_favourite_tune.mp3", Genre.Indie, "07/02/2023", artistKey);
        var songKey = await Db.CreateRecord(expectedSong);
        
        // Get the song record, and check if it's artist reference equals the one we created, then check the artist for
        // it it is aware it has been referenced by this song (using it's referencers property).
        var passes = 0;
        
        var song = await Db.GetRecord<Song>(songKey);
        /*if (song is not null && (await Db.GetRecord<Artist>(song.Data.Artist.RecordKey))?.Data == expectedArtist)
        {
            passes++;
        }

        var artist = await Db.GetRecord<Artist>(artistKey);
        if (artist is not null && artist.Referencers.Count(referencer =>
            referencer is PropertyInterKeyReference<Artist> artistReference && artistReference.RecordKey == songKey) == 1)
        {
            passes++;
        }*/
        
        return passes == 2;
    }
}