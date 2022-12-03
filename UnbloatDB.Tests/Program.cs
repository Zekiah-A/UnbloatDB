using UnbloatDB;
using UnbloatDB.Serialisers;
using UnbloatDB.Tests;

// We test with each serialiser, in order to ensure each work equally.
var databases = new List<Database>
{
    new(new Config("json", new JsonSerialiser())),
    //new(new Config("yaml", new YamlSerialiser()))
};

foreach (var database in databases)
{
    // Test creating 5000 account records, and song records with randomised values
    var createTest = new CreateTest(database, 50);
    Console.WriteLine("Result of Create test: " + await createTest.RunTest());
}

