# UnbloatDB
A database that uses common file formats, such as YAML, JSON and Plaintext to store data, supporting EF-Core like ORM mapping and smart indexing.

This database is not supposed to be the biggest, most feature packed, or suitable for every use. It is a simple, minimal way to store data, using human readable file formats. UnbloatDB aims more to be an abstraction over the file system, allowing for blazingly fast search, easy record management and indexing, than a full database.

Technical speficications can be found at [the specification sheet](https://github.com/Zekiah-A/UnbloatDB/blob/main/TECHNICAL_SPECIFICATIONS.md)

### TODO:
- Migrate to source generation model for vast amounts of database reflection logic.
- Attempt to keep filestreams open for as long as possible, to allow for more sporadic random writes.