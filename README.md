# NUnit-Helpers
A collection of code snippets to make testing with NUnit even easier.

## Integration Testing

### Attributes

#### MigrateDatabase

The MigrateDatabase attribute can be applied on a class to migrate a database to the latest version based on a definition of migration scripts. The MigrateDatabase attribute depends on [DbUp](https://dbup.github.io/) and the scripts will be found by providing the assembly name where migration scripts is placed. By convention the attribute try to use a connection string named `DefaultConnection` placed in the test projects app.config when connecting to the database to migrate. Another connection string can be used by providing the name of the connection string to use. By default the database will be emptied before the migration is performed.

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
