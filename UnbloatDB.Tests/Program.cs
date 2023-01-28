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
    Console.WriteLine("-----------------------------------");
    
    // Test creating multiple account records, and song records with randomised values
    var createTest = new CreateTest(database, 10);
    Console.WriteLine("Result of Create test: " + await createTest.RunTest());

    //var getTest = new GetTest(database);
    //Console.WriteLine("Result of Get test: " + await getTest.RunTest());

    //var findTest = new FindTest(database);
    //Console.WriteLine("Result of Find test: " + await findTest.RunTest());
}

