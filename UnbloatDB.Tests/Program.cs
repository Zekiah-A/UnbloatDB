using UnbloatDB;
using UnbloatDB.Serialisers;
using UnbloatDB.Tests;

// We test with each serialiser, in order to ensure each work equally.
var databases = new List<Database>
{
    new(new Configuration("json", new JsonSerialiser())),
    //new(new Configuration("yaml", new YamlSerialiser())),
    //new(new Configuration("xml", new XmlSerialiser()))
    //new(new Configuration("binary", new BinarySerialiser()))
};

foreach (var database in databases)
{
    Console.WriteLine("UnbloatDB v0.9a - ©Zekiah-A");
    
    // Test creating multiple account records, and song records with randomised values
    var createTest = new CreateTest(database, 10);
    Console.WriteLine("Result of Create test: " + await createTest.RunTest());

    var getTest = new GetTest(database);
    Console.WriteLine("Result of Get test: " + await getTest.RunTest());

    var findTest = new FindTest(database);
    Console.WriteLine("Result of Find test: " + await findTest.RunTest());

    var referenceTest = new ReferenceTest(database);
    Console.WriteLine("Result of Reference test: " + await referenceTest.RunTest());

    var deleteTest = new DeleteTest(database);
    Console.WriteLine("Result of Delete test: " + await deleteTest.RunTest());

    //var updateTest = new UpdateTest(database);
    //Console.WriteLine("Result of Update test: " + await updateTest.RunTest());
}

