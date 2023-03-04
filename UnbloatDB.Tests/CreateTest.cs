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
    
    private static string[] Location =
    {
        "Barrow-in-Furness",
        "Bedford",
        "Birkenhead",
        "Bishop Auckland",
        "Blackpool",
        "Bloxwich",
        "Blyth",
        "Bolton",
        "Boston",
        "Bournemouth",
        "Bridgwater",
        "Brighouse",
        "Burton upon Trent",
        "Camborne",
        "Carlisle",
        "Castleford",
        "Cheadle",
        "Clay Cross",
        "Cleator Moor",
        "Colchester",
        "Corby",
        "Crawley",
        "Crewe",
        "Darlington",
        "Darwen",
        "Dewsbury",
        "Doncaster",
        "Dudley",
        "Glastonbury",
        "Goldthorpe",
        "Goole",
        "Grays",
        "Great Yarmouth",
        "Grimsby",
        "Harlow",
        "Hartlepool",
        "Hastings",
        "Hereford",
        "Ipswich",
        "Keighley and Shipley",
        "Kidsgrove",
        "King’s Lynn",
        "Kirkby-in-Ashfield",
        "Leyland",
        "Lincoln",
        "Long Eaton",
        "Loughborough",
        "Lowestoft",
        "Mablethorpe",
        "Mansfield",
        "Margate",
        "Middlebrough",
        "Millom",
        "Milton Keynes",
        "Morley",
        "Nelson",
        "Newark-on-Trent",
        "Newcastle-under-Lyme",
        "Newhaven",
        "Northampton",
        "Norwich",
        "Nuneaton",
        "Oldham",
        "Penzance",
        "Peterborough",
        "Preston",
        "Redcar",
        "Redditch",
        "Rochdale",
        "Rotherham",
        "Rowley Regis",
        "Runcorn",
        "Scarborough",
        "Scunthorpe",
        "Skegness",
        "Smethwick",
        "Southport",
        "St Helens",
        "St Ives",
        "Stainforth",
        "Stapleford",
        "Stavele",
        "Stevenage",
        "Stocksbridge"
    };

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
                Location[Random.Next(0, Location.Length - 1)],
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