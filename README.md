# NUnit-Helpers
A collection of code snippets to make testing with NUnit even easier.

## Integration Testing

### Attributes

#### AutoRollback

The AutoRollback attribute can be applied to a class or a method. This makes all changes to automatically be rolled back after the test is performed leaving the database in the same state as before the test.

Can be used in conjunction with the Seed attribute to also rollback all changes performed by the seed script.

**Example:**

```csharp
/*
  Automatically rollback changes made during test and performed by the executed seed script.
*/

[Test]
[AutoRollback]
[Seed]
public void CanGetPersons()
{
  ...
}
```

#### MigrateDatabase

The MigrateDatabase attribute can be applied on a class to migrate a database to the latest version based on a definition of migration scripts. The MigrateDatabase attribute depends on [DbUp](https://dbup.github.io/) and the scripts will be found by providing the assembly name where migration scripts is placed. 

By convention the attribute try to use a connection string named `DefaultConnection` placed in the test projects app.config when connecting to the database to migrate. Another connection string can be used by providing the name of the connection string to use. By default the database will be emptied before the migration is performed.

**Example:**

```csharp
/*
  Migrate a database to the latest version using migration scripts found in assembly named Migrations.
*/
[TestFixture]
[MigrateDatabase("Migrations")]
public class PersonRepositoryTests
{
  ...
}
```

#### Seed

The Seed attribute is applied to a test method to seed a database with data required for a integration test. The SQL code used to seed the database is placed in a .sql file with the same name as the test method with `Embedded Resource` set as Build Action. By default the sql file needs to be placed in a namespace called `Seeds`. 

By convention the attribute try to use a connection string named `DefaultConnection` placed in the test projects app.config when connecting to the database to seed. Another connection string can be used by providing the name of the connection string to use. 

The namespace where seed script is located can be changed by setting the property `NameSpace`.

**Example:**

```csharp
/*
  Run SQL queried placed in seed script located in Seeds\CanGetPersons.sql.  
*/

[Test]
[Seed]
public void CanGetPersons()
{
  ...
}
```
