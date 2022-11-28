using UnbloatDB;
using UnbloatDB.Tests;

// We test with each serialiser, in order to ensure each work equally.
var databases = new List<Database>()
{
    new Database(new Config("json", new JsonSerialiser())),
    new Database(new Config("yaml", new YamlSerialiser()))
};

foreach (var database in databases)
{
    // Test creating 5000 account records, and song records with randomised values
    var createTest = new CreateTest(database, 5000, true);
    Console.WriteLine("Result of Create test: " + await createTest.RunTest());
}

