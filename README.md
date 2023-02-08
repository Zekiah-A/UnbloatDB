# UnbloatDB
A database that uses common file formats, such as YAML, JSON and Plaintext to store data, supporting simple record querying and smart indexing.

This database is not supposed to be the biggest, most feature packed, or suitable for every use. It is a simple, minimal way to store data, using human readable file formats. UnbloatDB aims more to be an abstraction over the file system, allowing for blazingly fast search, easy record management and indexing, than a full database.
## Getting started:
This is an example setup of an UnbloatDB instance.
```cs
// Create the datatypes we went to save to the database
public enum Gender
{
    Male,
    Female,
    Other,
    Unknown
}

internal sealed record Artist(int Age, string Location, Gender Gender);
...

// Create an instance of UnbloatDB, using the folder "my_database_folder" to store the data.
var database = new Database(new Configuration("my_database_folder", new JsonSerialiser()));

// Save data to the database
var myArtist = new Artist(25, "Ipswich", Gender.Male);
// CreateRecord returns the string master key of the record, which can be used later within subsequent queries.
var mykey = await database.CreateRecord<Artist>(myArtist);

// Find all artists that are male
var allMaleArtists = await database.FindRecords<Artist, Gender>("Gender", Gender.Male);

// Find all artists with age greater than 25
var allOlderArtists = await database.FindRecordsAfter<Artist, int>("Age", 25);

// Delete a record from the database, will return a bool to indicate if deletion was sucessful
var deleted = await database.DeleteRecord<Artist>(myKey);
```

Look within the `UnbloatDB.Tests` directory for more examples of actions that can be carried out with UnbloatDB.

## How it works:
Technical speficications can be found at [the specification sheet](https://github.com/Zekiah-A/UnbloatDB/blob/main/TECHNICAL_SPECIFICATIONS.md)


