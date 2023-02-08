# UnbloatDB - Architecture Specifications

### Data storage in UnbloatDB
Data in UnbloatDB has been designed to make use of the filesystem, with the individual files containing the data being file format agnostic and configurable. This allows you to choose, for example, to save your data in whatever format suits your application, such as json, yaml, or even pure bytearray. UnbloatDB will make use of the directory structure within the filesystem, with each “group” (imagine a SQL table) getting their own directory, with an eponymous file name, and each record being a file within that directory, with its name being its master key to allow for blazingly fast direct file access. Each group directory would also contain various indexer subdirectories, each the name of a property within the group’s record type. These indexer directories contain the indexer files for its property. Indexer files are a key part of UnbloatDB, as they allow for being able to search, categorise, relate and find similar records from merely one, two or even a few properties that they may possess. Indexers are used to quickly find the associated master key, from a value, making searches easy, without having to iterate through every record to check for matching properties, etc.

![image1](https://user-images.githubusercontent.com/73035340/203779698-965d8de1-fdcb-4db0-b032-719bf7fc6c10.png)
 
On a high level, all of the groups are on 2D plane, with cross referencing between records in different groups and the same group being possible with the use of Key references.

### Optimisations in UnbloatDB
UnbloatDB will attempt to cache all indexers, so they can quickly be read to and written, provided it has the memory beforehand, as they are frequently accessed, indexers will be modified in memory due to large size, and saved to disk periodically, and when the database is shut down.

UnbloatDB will also attempt to sort indexers when appending a new value, allowing for faster binary search to be used with integer values, and faster string search.

### Abstractions in UnbloatDB
In UnbloatDB, data storage is abstracted in order to make it easier to manage the database. Each object containing data is called a “Record”, with records of the same type (such as accounts) being called a “Group”. Records are identified and indexed by their master key, which is a code that allows you to access the handle of that record. Keys that reference another record in the same group are called IntraKeys, with keys that reference another record in a different group being called InterKeys. For example, if a song has the property "artist", the value should be an IntraKey to the artist's record. On the other hand, if an song has the property remixes”, then the value should be an IntraKey to another song record.

### Footnotes:
While small, lightweight and fast, UnbloatDB will not be the best, or even a good solution in all cases, while being ideal for HTTP-Based CDN/Account centred sites, where users are uploading content, with a need for vast saved static storage and quick content query speeds, UnbloatDB does not claim to be a "Do it all" database like many others that exist. UnbloatDB will not be great for cases such as unstructured data storage with records of unknown or completely unrealted types, real time messaging and chatting applications, and more, such use cases simply just have better alternatives than UnbloatDB.

UnbloatDB is also designed to not be an full separate program, or hassle to set up alongside your program, it is intended to be a quick and easy C# library that you can chuck into your project and use right away. While this does limit the scope of the database, and how many actions it can perform on its own, it helps keep things short and simple. Due to the unobfuscated, and simple data storage techniques, no tools or software should be needed to modify, read or share data that the database has saved from outside of the program, this means that linking an UnbloatDB to any other project, hosting files straight out of the database via a webserver, or just making quick changes can be done without a sweat.
